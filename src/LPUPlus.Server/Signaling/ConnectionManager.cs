using System.Collections.Concurrent;
using System.Net.WebSockets;
using LPUPlus.Protocol.Messages;
using LPUPlus.Protocol.Serialization;
using LPUPlus.Server.Auth;

namespace LPUPlus.Server.Signaling;

/// <summary>
/// Manages active WebSocket connections for hosts and receivers,
/// and routes protocol messages between them.
/// </summary>
public sealed class ConnectionManager
{
    private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();
    private readonly PairingStore _pairingStore;

    public ConnectionManager(PairingStore pairingStore)
    {
        _pairingStore = pairingStore;
    }

    /// <summary>
    /// Register a new WebSocket connection.
    /// </summary>
    public void AddSocket(string connectionId, WebSocket socket)
    {
        _sockets[connectionId] = socket;
    }

    /// <summary>
    /// Remove a WebSocket connection and clean up any associated state.
    /// </summary>
    public async Task RemoveSocketAsync(string connectionId)
    {
        if (_sockets.TryRemove(connectionId, out var socket))
        {
            try
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", CancellationToken.None);
                }
            }
            catch { /* Ignore socket exceptions on close */ }
        }

        // Clean up pairing store if this was a host
        // We'll iterate pending hosts to find the connectionId
        var pending = _pairingStore.GetPendingHosts().FirstOrDefault(h => h.ConnectionId == connectionId);
        if (pending != null)
        {
            _pairingStore.RemoveHost(pending.DeviceId);
        }
    }

    /// <summary>
    /// Send a protocol message to a specific connection.
    /// </summary>
    public async Task SendMessageAsync(string connectionId, IProtocolMessage message)
    {
        if (_sockets.TryGetValue(connectionId, out var socket) && socket.State == WebSocketState.Open)
        {
            var bytes = MessageSerializer.SerializeToBytes(message);
            try
            {
                await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch
            {
                // If send fails, assume connection is dead and remove it
                await RemoveSocketAsync(connectionId);
            }
        }
    }

    /// <summary>
    /// Send an error message to a specific connection.
    /// </summary>
    public Task SendErrorAsync(string connectionId, string code, string message)
    {
        return SendMessageAsync(connectionId, new ErrorMessage
        {
            Code = code,
            Message = message
        });
    }
}
