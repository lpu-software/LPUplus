using LPUPlus.Protocol.Models;

namespace LPUPlus.Agent.Security;

/// <summary>
/// Manages granular permissions for the active remote session.
/// The host must explicitly approve each permission category.
/// </summary>
public sealed class PermissionManager
{
    private Permission _granted = Permission.None;
    private readonly object _lock = new();

    /// <summary>
    /// Currently granted permissions.
    /// </summary>
    public Permission GrantedPermissions
    {
        get { lock (_lock) return _granted; }
    }

    /// <summary>
    /// Grant a specific permission.
    /// </summary>
    public void Grant(Permission permission)
    {
        lock (_lock) _granted |= permission;
    }

    /// <summary>
    /// Revoke a specific permission.
    /// </summary>
    public void Revoke(Permission permission)
    {
        lock (_lock) _granted &= ~permission;
    }

    /// <summary>
    /// Set permissions to an exact value (replace all).
    /// </summary>
    public void SetPermissions(Permission permissions)
    {
        lock (_lock) _granted = permissions;
    }

    /// <summary>
    /// Check if a specific permission is currently granted.
    /// Used for enforcement before processing incoming messages.
    /// </summary>
    public bool HasPermission(Permission permission)
    {
        lock (_lock) return (_granted & permission) == permission;
    }

    /// <summary>
    /// Revoke all permissions (session end or security event).
    /// </summary>
    public void RevokeAll()
    {
        lock (_lock) _granted = Permission.None;
    }

    /// <summary>
    /// Get a display-friendly summary of current permissions.
    /// </summary>
    public IReadOnlyList<(string Name, bool Granted)> GetPermissionSummary()
    {
        lock (_lock)
        {
            return
            [
                ("Screen View", _granted.HasFlag(Permission.ScreenView)),
                ("Mouse Control", _granted.HasFlag(Permission.MouseControl)),
                ("Keyboard Control", _granted.HasFlag(Permission.KeyboardControl)),
                ("Clipboard", _granted.HasFlag(Permission.Clipboard)),
                ("File Read", _granted.HasFlag(Permission.FileRead)),
                ("File Write", _granted.HasFlag(Permission.FileWrite)),
                ("File Delete", _granted.HasFlag(Permission.FileDelete)),
                ("Terminal", _granted.HasFlag(Permission.Terminal)),
            ];
        }
    }
}
