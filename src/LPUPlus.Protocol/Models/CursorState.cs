namespace LPUPlus.Protocol.Models;

/// <summary>
/// Represents the host cursor state at a point in time.
/// Sent independently of the video stream at high frequency (~120Hz).
/// </summary>
public sealed record CursorState
{
    /// <summary>Cursor X position in host physical pixels.</summary>
    public required double X { get; init; }

    /// <summary>Cursor Y position in host physical pixels.</summary>
    public required double Y { get; init; }

    /// <summary>Which monitor the cursor is on.</summary>
    public required string MonitorId { get; init; }

    /// <summary>Whether the cursor is visible.</summary>
    public required bool Visible { get; init; }

    /// <summary>Cursor type/shape.</summary>
    public CursorType CursorType { get; init; } = CursorType.Arrow;

    /// <summary>Hotspot X offset within the cursor image.</summary>
    public int HotspotX { get; init; }

    /// <summary>Hotspot Y offset within the cursor image.</summary>
    public int HotspotY { get; init; }

    /// <summary>Timestamp in microseconds (for interpolation/prediction).</summary>
    public required long TimestampUs { get; init; }
}

/// <summary>
/// Standard cursor types that map to CSS cursor values on the receiver.
/// </summary>
public enum CursorType
{
    Arrow,
    Hand,
    IBeam,
    Crosshair,
    SizeNS,
    SizeWE,
    SizeNWSE,
    SizeNESW,
    Move,
    Wait,
    Progress,
    Help,
    NotAllowed,
    Custom,
}
