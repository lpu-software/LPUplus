namespace LPUPlus.Protocol.Models;

/// <summary>
/// Streaming quality presets.
/// </summary>
public enum QualityMode
{
    /// <summary>Lower bitrate, higher FPS. Prioritizes responsiveness.</summary>
    Performance,

    /// <summary>Medium bitrate and FPS. Good balance.</summary>
    Balanced,

    /// <summary>Higher bitrate, maximum visual quality.</summary>
    Quality,

    /// <summary>Dynamically adjusts based on network conditions. Recommended default.</summary>
    Adaptive,
}

/// <summary>
/// Connection type as detected via ICE candidate inspection.
/// </summary>
public enum ConnectionType
{
    /// <summary>Direct LAN connection (host candidate).</summary>
    LanDirect,

    /// <summary>Direct connection via STUN (srflx candidate).</summary>
    Direct,

    /// <summary>Relayed via TURN server (relay candidate).</summary>
    Relay,

    /// <summary>Connection type not yet determined.</summary>
    Unknown,
}
