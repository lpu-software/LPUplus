using LPUPlus.Agent.Security;
using LPUPlus.Protocol.Models;

namespace LPUPlus.Agent.Tests;

public class SecurityTests
{
    [Fact]
    public void PairingCodeGenerator_GeneratesValidFormat()
    {
        var code = PairingCodeGenerator.Generate();
        Assert.NotNull(code);
        Assert.Equal(9, code.Length); // 4 chars + 1 dash + 4 chars
        Assert.True(PairingCodeGenerator.IsValidFormat(code));
    }

    [Fact]
    public void PairingCodeGenerator_ExcludesAmbiguousChars()
    {
        for (int i = 0; i < 100; i++)
        {
            var code = PairingCodeGenerator.Generate();
            Assert.DoesNotContain("0", code);
            Assert.DoesNotContain("O", code);
            Assert.DoesNotContain("1", code);
            Assert.DoesNotContain("I", code);
            Assert.DoesNotContain("L", code);
        }
    }

    [Fact]
    public void PermissionManager_GrantsAndRevokesPermissions()
    {
        var manager = new PermissionManager();
        Assert.Equal(Permission.None, manager.GrantedPermissions);

        manager.Grant(Permission.ScreenView);
        Assert.True(manager.HasPermission(Permission.ScreenView));
        Assert.False(manager.HasPermission(Permission.MouseControl));

        manager.Grant(Permission.MouseControl);
        Assert.True(manager.HasPermission(Permission.MouseControl));

        manager.Revoke(Permission.ScreenView);
        Assert.False(manager.HasPermission(Permission.ScreenView));
        Assert.True(manager.HasPermission(Permission.MouseControl));

        manager.RevokeAll();
        Assert.Equal(Permission.None, manager.GrantedPermissions);
    }
}
