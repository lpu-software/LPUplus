using LPUPlus.Protocol.Models;

namespace LPUPlus.Protocol.Messages;

// ─────────────────────────────────────────────────────────────
//  AUTHENTICATION MESSAGES
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Sent by the host to register with the signaling server.
/// </summary>
public sealed record HostRegisterMessage : IProtocolMessage
{
    public string Type => MessageTypes.HostRegister;
    public required string DeviceId { get; init; }
    public required string DeviceName { get; init; }
    public required string Platform { get; init; }
    public required string PairingCodeHash { get; init; }
}

/// <summary>
/// Sent by the receiver to authenticate using a pairing code.
/// </summary>
public sealed record PairRequestMessage : IProtocolMessage
{
    public string Type => MessageTypes.PairRequest;
    public required string PairingCode { get; init; }
}

/// <summary>
/// Server response to a pairing attempt.
/// </summary>
public sealed record PairResponseMessage : IProtocolMessage
{
    public string Type => MessageTypes.PairResponse;
    public required bool Success { get; init; }
    public string? SessionToken { get; init; }
    public string? DeviceName { get; init; }
    public string? DeviceId { get; init; }
    public string? Error { get; init; }
}
