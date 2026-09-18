using System.Net.WebSockets;
using System.Text;
using LPUPlus.Protocol.Messages;
using LPUPlus.Protocol.Serialization;
using LPUPlus.Server.Auth;

namespace LPUPlus.Server.Signaling;

/// <summary>
/// Handles the WebSocket lifecycle and message processing for a single connection.
/// </summary>
public sealed class SignalingHandler
{
    private readonly ConnectionManager _connections;
    private readonly PairingStore _pairingStore;
    private readonly JwtTokenService _jwtService;
    private readonly LPUPlus.Server.AI.AIService _aiService;
    private readonly ILogger<SignalingHandler> _logger;

    public SignalingHandler(
        ConnectionManager connections,
        PairingStore pairingStore,
        JwtTokenService jwtService,
        LPUPlus.Server.AI.AIService aiService,
        ILogger<SignalingHandler> logger)
    {
        _connections = connections;
        _pairingStore = pairingStore;
        _jwtService = jwtService;
        _aiService = aiService;
        _logger = logger;
    }

    /// <summary>
    /// Main loop for a WebSocket connection.
    /// </summary>
    public async Task HandleConnectionAsync(WebSocket socket, string connectionId)
    {
        _connections.AddSocket(connectionId, socket);
        _logger.LogInformation("WebSocket connected: {ConnectionId}", connectionId);

        var buffer = new byte[1024 * 16];

        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (receiveResult.MessageType == WebSocketMessageType.Text)
                {
                    // Read full message (might span multiple receives if > 16KB)
                    using var ms = new MemoryStream();
                    ms.Write(buffer, 0, receiveResult.Count);
                    while (!receiveResult.EndOfMessage)
                    {
                        receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        ms.Write(buffer, 0, receiveResult.Count);
                    }
                    var payloadSpan = ms.ToArray().AsSpan();
                    
                    var message = MessageSerializer.Deserialize(payloadSpan);
                    if (message != null)
                    {
                        await ProcessMessageAsync(connectionId, message);
                    }
                }
            }
        }
        catch (WebSocketException)
        {
            // Normal disconnect
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing WebSocket connection {ConnectionId}", connectionId);
        }
        finally
        {
            await _connections.RemoveSocketAsync(connectionId);
            _logger.LogInformation("WebSocket disconnected: {ConnectionId}", connectionId);
        }
    }

    private async Task ProcessMessageAsync(string connectionId, IProtocolMessage message)
    {
        switch (message)
        {
            case HostRegisterMessage req:
                _logger.LogInformation("Host register: {DeviceId}", req.DeviceId);
                _pairingStore.RegisterHost(req.DeviceId, req.DeviceName, req.Platform, req.PairingCodeHash, connectionId);
                break;

            case PairRequestMessage req:
                _logger.LogInformation("Pair request received");
                var host = _pairingStore.TryPair(req.PairingCode);
                
                if (host == null)
                {
                    await _connections.SendMessageAsync(connectionId, new PairResponseMessage 
                    { 
                        Success = false, 
                        Error = "Invalid pairing code or host not available" 
                    });
                    break;
                }

                // If code is valid, we notify the host that someone is requesting a session.
                // The receiver stays connected and waits for the host to approve.
                // We'll use a SessionRequestMessage routed to the host.
                await _connections.SendMessageAsync(host.ConnectionId, new SessionRequestMessage
                {
                    // The receiver hasn't actually specified requested permissions yet in the pairing code flow,
                    // but we can default to RemoteControl for the prompt.
                    SessionToken = connectionId, // Use connectionId as temporary token for routing the approval
                    RequestedPermissions = LPUPlus.Protocol.Models.PermissionSets.RemoteControl
                });
                break;

            case SessionApprovedMessage req:
                // Host approved the session. The `SessionId` in this message is generated by the host,
                // but wait, let's have the Server generate the official session token.
                // The Host sent this approval in response to SessionRequest.
                
                var receiverConnectionId = req.SessionId; // We passed receiver's connectionId in SessionRequest.SessionToken

                // Generate JWT
                // First need to find the host that sent this.
                var pendingHost = _pairingStore.GetPendingHosts().FirstOrDefault(h => h.ConnectionId == connectionId);
                if (pendingHost == null) return;

                var jwt = _jwtService.GenerateSessionToken(
                    Guid.NewGuid().ToString("N"),
                    pendingHost.DeviceId,
                    req.GrantedPermissions.ToString().Split(',').Select(p => p.Trim()).ToArray()
                );

                var session = _pairingStore.CreateSession(pendingHost.DeviceId, connectionId, receiverConnectionId, jwt);

                // Notify receiver of success and give them the JWT
                await _connections.SendMessageAsync(receiverConnectionId, new PairResponseMessage
                {
                    Success = true,
                    SessionToken = jwt,
                    DeviceName = pendingHost.DeviceName,
                    DeviceId = pendingHost.DeviceId
                });

                // We don't need to send anything back to the host here, the host just waits for SDP Offer.
                break;

            case SessionRejectedMessage req:
                // Host rejected. We need to tell the receiver.
                // Unfortunately we don't have the receiver's connection ID in this message as modeled.
                // Let's assume we can add it or just log it for now.
                // ACTUALLY: Let's find the receiver by looking for pending requests (or just log).
                _logger.LogInformation("Session rejected by host");
                break;

            // SDP and ICE routing
            case SdpOfferMessage req:
            case SdpAnswerMessage ans:
            case IceCandidateMessage ice:
                // Route between host and receiver.
                // Find active session for this connection
                var activeSession = _pairingStore.GetSessionByConnectionId(connectionId);
                if (activeSession != null)
                {
                    var targetConnectionId = activeSession.HostConnectionId == connectionId 
                        ? activeSession.ReceiverConnectionId 
                        : activeSession.HostConnectionId;

                    await _connections.SendMessageAsync(targetConnectionId, message);
                }
                break;

            case AIRequestMessage req:
                _logger.LogInformation("Processing AIRequest for {RequestId}", req.RequestId);
                var aiResponse = await _aiService.ProcessRequestAsync(req);
                await _connections.SendMessageAsync(connectionId, aiResponse);
                break;

            default:
                _logger.LogWarning("Unhandled message type: {Type}", message.Type);
                break;
        }
    }
}
