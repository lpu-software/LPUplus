using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace LPUPlus.Server.Auth;

/// <summary>
/// Manages pairing codes and device registration on the server side.
/// Handles the pairing flow: host registers → receiver enters code → server validates.
/// </summary>
public sealed class PairingStore
{
    /// <summary>
    /// Registered host waiting for a receiver to pair.
    /// </summary>
    public sealed record PendingHost(
        string DeviceId,
        string DeviceName,
        string Platform,
        string PairingCodeHash,
        string ConnectionId,
        DateTimeOffset RegisteredAt);

    /// <summary>
    /// Active session after successful pairing.
    /// </summary>
    public sealed record ActiveSession(
        string SessionId,
        string HostDeviceId,
        string HostConnectionId,
        string ReceiverConnectionId,
        string SessionToken,
        DateTimeOffset CreatedAt);

    // Pending hosts indexed by device ID
    private readonly ConcurrentDictionary<string, PendingHost> _pendingHosts = new();

    // Active sessions indexed by session ID
    private readonly ConcurrentDictionary<string, ActiveSession> _activeSessions = new();

    // HMAC secret for pairing code verification
    private readonly string _hmacSecret;

    public PairingStore()
    {
        _hmacSecret = "LPUPlusSharedSecret";
    }

    /// <summary>
    /// Register a host that is waiting for a receiver to pair.
    /// </summary>
    public void RegisterHost(string deviceId, string deviceName, string platform,
                              string pairingCodeHash, string connectionId)
    {
        var host = new PendingHost(deviceId, deviceName, platform, pairingCodeHash,
                                    connectionId, DateTimeOffset.UtcNow);
        _pendingHosts[deviceId] = host;
    }

    /// <summary>
    /// Attempt to pair with a host using a pairing code.
    /// Returns the matched host if the code is valid, null otherwise.
    /// </summary>
    public PendingHost? TryPair(string pairingCode)
    {
        var normalizedCode = pairingCode.ToUpperInvariant().Replace("-", "").Trim();
        var codeHash = HashCode(normalizedCode);

        foreach (var host in _pendingHosts.Values)
        {
            if (host.PairingCodeHash == codeHash)
            {
                return host;
            }
        }

        return null;
    }

    /// <summary>
    /// Create an active session after the host approves.
    /// </summary>
    public ActiveSession CreateSession(string hostDeviceId, string hostConnectionId,
                                        string receiverConnectionId, string sessionToken)
    {
        var session = new ActiveSession(
            SessionId: Guid.NewGuid().ToString("N")[..12],
            HostDeviceId: hostDeviceId,
            HostConnectionId: hostConnectionId,
            ReceiverConnectionId: receiverConnectionId,
            SessionToken: sessionToken,
            CreatedAt: DateTimeOffset.UtcNow);

        _activeSessions[session.SessionId] = session;

        // Remove from pending hosts
        _pendingHosts.TryRemove(hostDeviceId, out _);

        return session;
    }

    /// <summary>
    /// Get an active session by ID.
    /// </summary>
    public ActiveSession? GetSession(string sessionId)
    {
        _activeSessions.TryGetValue(sessionId, out var session);
        return session;
    }

    /// <summary>
    /// Get an active session by connection ID (host or receiver).
    /// </summary>
    public ActiveSession? GetSessionByConnectionId(string connectionId)
    {
        return _activeSessions.Values.FirstOrDefault(s => 
            s.HostConnectionId == connectionId || s.ReceiverConnectionId == connectionId);
    }

    /// <summary>
    /// End an active session.
    /// </summary>
    public bool EndSession(string sessionId)
    {
        return _activeSessions.TryRemove(sessionId, out _);
    }

    /// <summary>
    /// Remove a host registration (e.g., on disconnect).
    /// </summary>
    public bool RemoveHost(string deviceId)
    {
        return _pendingHosts.TryRemove(deviceId, out _);
    }

    /// <summary>
    /// Get all pending host devices (for debugging/admin).
    /// </summary>
    public IReadOnlyList<PendingHost> GetPendingHosts()
    {
        return _pendingHosts.Values.ToList();
    }

    /// <summary>
    /// Clean up expired registrations (older than 10 minutes).
    /// </summary>
    public int CleanupExpired(TimeSpan? maxAge = null)
    {
        maxAge ??= TimeSpan.FromMinutes(10);
        var cutoff = DateTimeOffset.UtcNow - maxAge.Value;
        var removed = 0;

        foreach (var kvp in _pendingHosts)
        {
            if (kvp.Value.RegisteredAt < cutoff)
            {
                if (_pendingHosts.TryRemove(kvp.Key, out _))
                    removed++;
            }
        }

        return removed;
    }

    /// <summary>
    /// Hash a pairing code for comparison using the server's HMAC secret.
    /// </summary>
    public string HashCode(string normalizedCode)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_hmacSecret);
        var codeBytes = Encoding.UTF8.GetBytes(normalizedCode);
        var hash = HMACSHA256.HashData(keyBytes, codeBytes);
        return Convert.ToBase64String(hash);
    }
}
