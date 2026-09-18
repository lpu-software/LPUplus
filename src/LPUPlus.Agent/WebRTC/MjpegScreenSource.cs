using System.Diagnostics;

namespace LPUPlus.Agent.WebRTC;

public class MjpegScreenSource : IDisposable
{
    private Process? _ffmpegProcess;
    private CancellationTokenSource _cts = new();
    private int _frameCount = 0;

    public event Action<byte[]>? OnJpegFrame;

    public void Start()
    {
        _cts = new CancellationTokenSource();
        
        // High quality 1080p MJPEG capture at 30fps
        // -q:v 5 = high quality JPEG (range 2-31, lower = better)
        // -r 30 = 30 frames per second for smooth video
        // -nostdin = don't read from stdin (prevents hangs)
        // -loglevel error = suppress noisy output
        // Note: RedirectStandardError must be false to prevent pipe buffer deadlock
        _ffmpegProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-nostdin -f avfoundation -pix_fmt uyvy422 -i \"1\" -vf scale=1920:-1 -q:v 5 -r 30 -loglevel error -f image2pipe -vcodec mjpeg -",
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        _ffmpegProcess.Start();
        Console.WriteLine($"[MJPEG] ffmpeg started (PID: {_ffmpegProcess.Id})");

        Task.Run(() => ReadMjpegStream(_ffmpegProcess.StandardOutput.BaseStream, _cts.Token));
    }

    private async Task ReadMjpegStream(Stream stream, CancellationToken token)
    {
        try
        {
            var buffer = new byte[65536]; // 64KB read buffer for large HD frames
            var memoryStream = new MemoryStream(256 * 1024); // Pre-allocate 256KB
            
            while (!token.IsCancellationRequested)
            {
                int read = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                if (read == 0)
                {
                    Console.WriteLine("[MJPEG] ffmpeg stdout stream ended (read 0 bytes).");
                    // Check if ffmpeg process has exited
                    if (_ffmpegProcess != null && _ffmpegProcess.HasExited)
                    {
                        Console.WriteLine($"[MJPEG] ffmpeg process exited with code: {_ffmpegProcess.ExitCode}");
                    }
                    break;
                }

                memoryStream.Write(buffer, 0, read);
                
                // Extract ALL complete JPEG frames from the buffer
                ExtractFrames(memoryStream);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[MJPEG] Stream reading cancelled.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MJPEG] Stream error: {ex.GetType().Name}: {ex.Message}");
        }
        
        Console.WriteLine($"[MJPEG] Total frames emitted: {_frameCount}");
    }

    private void ExtractFrames(MemoryStream memoryStream)
    {
        while (true)
        {
            var bytes = memoryStream.ToArray();
            if (bytes.Length < 2) return;
            
            // Find JPEG start marker (FF D8)
            int startIdx = -1;
            for (int i = 0; i < bytes.Length - 1; i++)
            {
                if (bytes[i] == 0xFF && bytes[i + 1] == 0xD8)
                {
                    startIdx = i;
                    break;
                }
            }
            
            if (startIdx < 0) return; // No start marker found yet
            
            // Find JPEG end marker (FF D9) AFTER the start
            int endIdx = -1;
            for (int i = startIdx + 2; i < bytes.Length - 1; i++)
            {
                if (bytes[i] == 0xFF && bytes[i + 1] == 0xD9)
                {
                    endIdx = i + 1; // Include the D9 byte
                    break;
                }
            }
            
            if (endIdx < 0) return; // No complete frame yet, wait for more data
            
            // Extract the complete JPEG frame
            int frameLength = endIdx + 1 - startIdx;
            var jpeg = new byte[frameLength];
            Array.Copy(bytes, startIdx, jpeg, 0, frameLength);
            
            // Remove the extracted frame from the buffer
            int remaining = bytes.Length - (endIdx + 1);
            memoryStream.SetLength(0);
            if (remaining > 0)
            {
                memoryStream.Write(bytes, endIdx + 1, remaining);
            }
            
            // Emit the frame
            _frameCount++;
            try
            {
                OnJpegFrame?.Invoke(jpeg);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MJPEG] Frame emit error: {ex.Message}");
            }
        }
    }

    public void Stop()
    {
        _cts.Cancel();
        if (_ffmpegProcess != null && !_ffmpegProcess.HasExited)
        {
            try { _ffmpegProcess.Kill(); } catch { }
            Console.WriteLine("[MJPEG] ffmpeg process killed.");
        }
        _ffmpegProcess?.Dispose();
    }

    public void Dispose()
    {
        Stop();
    }
}
