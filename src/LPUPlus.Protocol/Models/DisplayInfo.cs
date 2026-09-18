namespace LPUPlus.Protocol.Models;

/// <summary>
/// Information about a single display/monitor on the host.
/// Used for multi-monitor enumeration and coordinate mapping.
/// </summary>
public sealed record DisplayInfo
{
    /// <summary>Unique identifier for this display.</summary>
    public required string Id { get; init; }

    /// <summary>Human-readable display name.</summary>
    public required string Name { get; init; }

    /// <summary>Physical width in pixels (actual device pixels).</summary>
    public required int Width { get; init; }

    /// <summary>Physical height in pixels (actual device pixels).</summary>
    public required int Height { get; init; }

    /// <summary>Logical width in pixels (DPI-adjusted).</summary>
    public required int LogicalWidth { get; init; }

    /// <summary>Logical height in pixels (DPI-adjusted).</summary>
    public required int LogicalHeight { get; init; }

    /// <summary>X position in the virtual desktop coordinate space.</summary>
    public required int X { get; init; }

    /// <summary>Y position in the virtual desktop coordinate space.</summary>
    public required int Y { get; init; }

    /// <summary>Scale factor (1.0, 1.25, 1.5, 2.0, etc.).</summary>
    public required double ScaleFactor { get; init; }

    /// <summary>Whether this is the primary display.</summary>
    public required bool IsPrimary { get; init; }

    /// <summary>Display orientation.</summary>
    public DisplayOrientation Orientation { get; init; } = DisplayOrientation.Landscape;
}

public enum DisplayOrientation
{
    Landscape,
    Portrait,
    LandscapeFlipped,
    PortraitFlipped,
}
