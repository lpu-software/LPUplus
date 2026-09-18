namespace LPUPlus.Protocol.Models;

/// <summary>
/// Granular permission flags that the host can grant or deny per session.
/// </summary>
[Flags]
public enum Permission
{
    None = 0,
    ScreenView = 1 << 0,
    MouseControl = 1 << 1,
    KeyboardControl = 1 << 2,
    Clipboard = 1 << 3,
    FileRead = 1 << 4,
    FileWrite = 1 << 5,
    FileDelete = 1 << 6,
    Terminal = 1 << 7,
}

/// <summary>
/// Convenience groupings for common permission sets.
/// </summary>
public static class PermissionSets
{
    public const Permission ViewOnly = Permission.ScreenView;

    public const Permission RemoteControl =
        Permission.ScreenView |
        Permission.MouseControl |
        Permission.KeyboardControl;

    public const Permission FullAccess =
        Permission.ScreenView |
        Permission.MouseControl |
        Permission.KeyboardControl |
        Permission.Clipboard |
        Permission.FileRead |
        Permission.FileWrite |
        Permission.FileDelete |
        Permission.Terminal;
}
