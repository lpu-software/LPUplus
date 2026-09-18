namespace LPUPlus.Protocol.Messages;

// ─────────────────────────────────────────────────────────────
//  WEBRTC SIGNALING MESSAGES (WebSocket, reliable)
// ─────────────────────────────────────────────────────────────

/// <summary>
/// SDP offer sent by the host to initiate WebRTC.
/// </summary>
public sealed record SdpOfferMessage : IProtocolMessage
{
    public string Type => MessageTypes.SdpOffer;
    public required string Sdp { get; init; }
}

/// <summary>
/// SDP answer sent by the receiver to complete negotiation.
/// </summary>
public sealed record SdpAnswerMessage : IProtocolMessage
{
    public string Type => MessageTypes.SdpAnswer;
    public required string Sdp { get; init; }
}

/// <summary>
/// ICE candidate exchanged during connectivity checks.
/// </summary>
public sealed record IceCandidateMessage : IProtocolMessage
{
    public string Type => MessageTypes.IceCandidate;
    public required string Candidate { get; init; }
    public string? SdpMid { get; init; }
    public int? SdpMLineIndex { get; init; }
}

// ─────────────────────────────────────────────────────────────
//  ERROR MESSAGE
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Generic error message for any subsystem.
/// </summary>
public sealed record ErrorMessage : IProtocolMessage
{
    public string Type => MessageTypes.Error;
    public required string Code { get; init; }
    public required string Message { get; init; }
    public string? Details { get; init; }
}
