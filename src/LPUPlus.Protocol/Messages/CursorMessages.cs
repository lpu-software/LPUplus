using LPUPlus.Protocol.Models;

namespace LPUPlus.Protocol.Messages;

// ─────────────────────────────────────────────────────────────
//  CURSOR MESSAGES (DataChannel: "cursor", unreliable)
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Sent by the host at ~120Hz with current cursor state.
/// Uses unreliable DataChannel for lowest latency — dropped updates are fine
/// because the next update will correct the position.
/// </summary>
public sealed record CursorUpdateMessage : IProtocolMessage
{
    public string Type => MessageTypes.CursorUpdate;
    public required double X { get; init; }
    public required double Y { get; init; }
    public required string MonitorId { get; init; }
    public required bool Visible { get; init; }
    public CursorType CursorType { get; init; } = CursorType.Arrow;
    public int HotspotX { get; init; }
    public int HotspotY { get; init; }
    public required long TimestampUs { get; init; }
}
