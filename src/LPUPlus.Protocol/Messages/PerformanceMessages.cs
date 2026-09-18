using LPUPlus.Protocol.Models;

namespace LPUPlus.Protocol.Messages;

// ─────────────────────────────────────────────────────────────
//  PERFORMANCE MESSAGES (DataChannel: "metrics", unreliable)
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Periodic performance telemetry. Sent every 1–2 seconds.
/// All values are measured at runtime — never fabricated.
/// </summary>
public sealed record PerformanceUpdateMessage : IProtocolMessage
{
    public string Type => MessageTypes.PerformanceUpdate;

    // ── Connection ──
    public required ConnectionType ConnectionType { get; init; }
    public required double RttMs { get; init; }
    public required double JitterMs { get; init; }
    public required double PacketLossPercent { get; init; }

    // ── Video ──
    public required double Fps { get; init; }
    public required int ResolutionWidth { get; init; }
    public required int ResolutionHeight { get; init; }
    public required double BitrateMbps { get; init; }
    public required string Codec { get; init; }

    // ── Latency breakdown ──
    public double CaptureMs { get; init; }
    public double EncodeMs { get; init; }
    public double NetworkMs { get; init; }
    public double DecodeMs { get; init; }
    public double RenderMs { get; init; }

    // ── Host resources (optional) ──
    public double? CpuPercent { get; init; }
    public double? GpuPercent { get; init; }
    public double? MemoryPercent { get; init; }

    public required long TimestampMs { get; init; }
}
