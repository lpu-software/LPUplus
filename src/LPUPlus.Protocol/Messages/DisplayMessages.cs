using LPUPlus.Protocol.Models;

namespace LPUPlus.Protocol.Messages;

// ─────────────────────────────────────────────────────────────
//  DISPLAY MESSAGES (DataChannel: "control", reliable)
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Sent by the host with the complete monitor list.
/// </summary>
public sealed record MonitorListMessage : IProtocolMessage
{
    public string Type => MessageTypes.MonitorList;
    public required DisplayInfo[] Monitors { get; init; }
    public required string ActiveMonitorId { get; init; }
}

/// <summary>
/// Sent by the receiver to switch which monitor is being viewed/captured.
/// </summary>
public sealed record MonitorSwitchMessage : IProtocolMessage
{
    public string Type => MessageTypes.MonitorSwitch;
    public required string MonitorId { get; init; }
}

/// <summary>
/// Sent by the host when a display configuration change is detected.
/// </summary>
public sealed record DisplayChangedMessage : IProtocolMessage
{
    public string Type => MessageTypes.DisplayChanged;
    public required DisplayInfo[] Monitors { get; init; }
    public required string ActiveMonitorId { get; init; }
    public required string ChangeReason { get; init; }
}
