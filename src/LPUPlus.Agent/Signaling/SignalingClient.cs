using System.Net.WebSockets;
using LPUPlus.Protocol.Messages;
using LPUPlus.Protocol.Serialization;

namespace LPUPlus.Agent.Signaling;

/// <summary>
/// Connects to the ASP.NET Core signaling server and handles message routing.
/// </summary>
public sealed class SignalingClient : IDisposable
{
    private readonly ClientWebSocket _ws = new();
    private readonly Uri _serverUri;
    private readonly CancellationTokenSource _cts = new();

    // Event hooks for AgentService
    public event Func<SessionRequestMessage, Task>? OnSessionRequest;
    public event Func<SdpOfferMessage, Task>? OnSdpOffer;
    public event Func<SdpAnswerMessage, Task>? OnSdpAnswer;
    public event Func<IceCandidateMessage, Task>? OnIceCandidate;
    public event Action? OnDisconnected;

    public SignalingClient(string serverUrl)
    {
        _serverUri = new Uri(serverUrl);
    }

    /// <summary>
    /// Connect to the signaling server and start receiving messages.
    /// </summary>
    public async Task ConnectAsync(CancellationToken ct = default)
    {
        await _ws.ConnectAsync(_serverUri, ct);
        _ = ReceiveLoopAsync(_cts.Token);
    }

    /// <summary>
    /// Send a message to the signaling server.
    /// </summary>
    public async Task SendMessageAsync(IProtocolMessage message, CancellationToken ct = default)
    {
        if (_ws.State != WebSocketState.Open) return;

        var bytes = MessageSerializer.SerializeToBytes(message);
        await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[1024 * 16];
        try
        {
            while (_ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                
                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var payloadSpan = buffer.AsSpan(0, result.Count);
                    var message = MessageSerializer.Deserialize(payloadSpan);
                    if (message != null)
                    {
                        await ProcessMessageAsync(message);
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            // Network error
        }
        finally
        {
            OnDisconnected?.Invoke();
        }
    }

    private async Task ProcessMessageAsync(IProtocolMessage message)
    {
        switch (message)
        {
            case SessionRequestMessage req when OnSessionRequest != null:
                await OnSessionRequest(req);
                break;
            case SdpOfferMessage offer when OnSdpOffer != null:
                await OnSdpOffer(offer);
                break;
            case SdpAnswerMessage answer when OnSdpAnswer != null:
                await OnSdpAnswer(answer);
                break;
            case IceCandidateMessage ice when OnIceCandidate != null:
                await OnIceCandidate(ice);
                break;
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _ws.Dispose();
    }
}
