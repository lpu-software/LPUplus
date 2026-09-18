using LPUPlus.Server.Auth;

namespace LPUPlus.Server.Tests;

public class AuthTests
{
    [Fact]
    public void JwtTokenService_GeneratesAndValidatesToken()
    {
        var service = new JwtTokenService("super_secret_key_that_is_long_enough_for_hmac_sha256!");
        var token = service.GenerateSessionToken("session-123", "device-456", ["ScreenView", "MouseControl"]);

        Assert.NotNull(token);
        Assert.NotEmpty(token);

        var principal = service.ValidateToken(token);
        Assert.NotNull(principal);

        var sub = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        Assert.Equal("session-123", sub);

        var deviceId = principal.FindFirst("device_id")?.Value;
        Assert.Equal("device-456", deviceId);

        var perms = principal.FindAll("perm").Select(c => c.Value).ToList();
        Assert.Contains("ScreenView", perms);
        Assert.Contains("MouseControl", perms);
    }

    [Fact]
    public void PairingStore_HandlesPairingLifecycle()
    {
        var store = new PairingStore();

        // Register host
        var rawCode = "ABCD-EFGH";
        var normalizedCode = "ABCDEFGH";
        var hash = store.HashCode(normalizedCode);
        store.RegisterHost("dev-1", "My PC", "Windows", hash, "conn-1");

        var pending = store.GetPendingHosts();
        Assert.Single(pending);
        Assert.Equal("dev-1", pending[0].DeviceId);

        // Fail to pair with wrong code
        var failedPair = store.TryPair("WRONG-CODE");
        Assert.Null(failedPair);

        // Pair with correct code
        var successPair = store.TryPair(rawCode);
        Assert.NotNull(successPair);
        Assert.Equal("dev-1", successPair.DeviceId);

        // Create session
        var session = store.CreateSession("dev-1", "conn-1", "conn-2", "jwt-token");
        Assert.NotNull(session);
        Assert.Equal("dev-1", session.HostDeviceId);

        // Host should be removed from pending
        Assert.Empty(store.GetPendingHosts());

        // Get session
        var active = store.GetSession(session.SessionId);
        Assert.NotNull(active);
        Assert.Equal("jwt-token", active.SessionToken);

        // End session
        var removed = store.EndSession(session.SessionId);
        Assert.True(removed);
        Assert.Null(store.GetSession(session.SessionId));
    }
}
