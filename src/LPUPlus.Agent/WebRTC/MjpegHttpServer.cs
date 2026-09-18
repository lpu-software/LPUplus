using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LPUPlus.Agent.WebRTC;

/// <summary>
/// A lightweight HTTP server that serves an MJPEG stream.
/// Browsers natively render multipart/x-mixed-replace JPEG streams in &lt;img&gt; tags.
/// This is the same protocol IP cameras have used for decades - battle-tested and reliable.
/// </summary>
public sealed class MjpegHttpServer : IDisposable
{
    private readonly int _port;
    private TcpListener? _listener;
    private CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<int, ClientState> _clients = new();
    private int _clientIdCounter = 0;
    private byte[]? _latestFrame;
    private readonly object _frameLock = new();

    public int Port => _port;

    public MjpegHttpServer(int port = 8765)
    {
        _port = port;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Loopback, _port);
        _listener.Start();
        Console.WriteLine($"[MJPEG-HTTP] Server listening on http://localhost:{_port}/stream");
        Task.Run(() => AcceptClientsAsync(_cts.Token));
    }

    private async Task AcceptClientsAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var tcpClient = await _listener!.AcceptTcpClientAsync(token);
                var clientId = Interlocked.Increment(ref _clientIdCounter);
                Console.WriteLine($"[MJPEG-HTTP] Client {clientId} connected");
                _ = Task.Run(() => HandleClientAsync(tcpClient, clientId, token));
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MJPEG-HTTP] Accept error: {ex.Message}");
            }
        }
    }

    private async Task HandleClientAsync(TcpClient tcpClient, int clientId, CancellationToken token)
    {
        try
        {
            using (tcpClient)
            {
                var stream = tcpClient.GetStream();

                // Read the HTTP request (we don't need to parse it, just consume it)
                var requestBuffer = new byte[4096];
                await stream.ReadAsync(requestBuffer, 0, requestBuffer.Length, token);

                // Send HTTP response headers for MJPEG stream
                var boundary = "mjpegboundary";
                var headers = $"HTTP/1.1 200 OK\r\n" +
                              $"Content-Type: multipart/x-mixed-replace; boundary={boundary}\r\n" +
                              $"Cache-Control: no-cache, no-store, must-revalidate\r\n" +
                              $"Pragma: no-cache\r\n" +
                              $"Access-Control-Allow-Origin: *\r\n" +
                              $"Connection: keep-alive\r\n" +
                              $"\r\n";

                var headerBytes = Encoding.ASCII.GetBytes(headers);
                await stream.WriteAsync(headerBytes, 0, headerBytes.Length, token);
                await stream.FlushAsync(token);

                // Register the client for frame pushing
                var clientState = new ClientState(stream, boundary);
                _clients[clientId] = clientState;

                // Keep connection alive until cancelled or disconnected
                try
                {
                    await Task.Delay(Timeout.Infinite, token);
                }
                catch (OperationCanceledException) { }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MJPEG-HTTP] Client {clientId} error: {ex.Message}");
        }
        finally
        {
            _clients.TryRemove(clientId, out _);
            Console.WriteLine($"[MJPEG-HTTP] Client {clientId} disconnected");
        }
    }

    /// <summary>
    /// Push a JPEG frame to all connected HTTP clients.
    /// </summary>
    public void PushFrame(byte[] jpegData)
    {
        lock (_frameLock)
        {
            _latestFrame = jpegData;
        }

        var deadClients = new List<int>();

        foreach (var kvp in _clients)
        {
            try
            {
                var client = kvp.Value;
                var frameHeader = Encoding.ASCII.GetBytes(
                    $"--{client.Boundary}\r\n" +
                    $"Content-Type: image/jpeg\r\n" +
                    $"Content-Length: {jpegData.Length}\r\n" +
                    $"\r\n");
                var frameFooter = Encoding.ASCII.GetBytes("\r\n");

                var outputStream = client.Stream;
                outputStream.Write(frameHeader, 0, frameHeader.Length);
                outputStream.Write(jpegData, 0, jpegData.Length);
                outputStream.Write(frameFooter, 0, frameFooter.Length);
                outputStream.Flush();
            }
            catch
            {
                deadClients.Add(kvp.Key);
            }
        }

        foreach (var id in deadClients)
        {
            _clients.TryRemove(id, out _);
        }
    }

    public void Stop()
    {
        _cts.Cancel();
        _listener?.Stop();
        _clients.Clear();
    }

    public void Dispose()
    {
        Stop();
    }

    private record ClientState(NetworkStream Stream, string Boundary);
}
