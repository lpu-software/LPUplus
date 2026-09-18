using LPUPlus.Protocol.Models;

namespace LPUPlus.Protocol.Messages;

// ─────────────────────────────────────────────────────────────
//  SESSION MESSAGES
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Sent by the receiver to request a remote session.
/// </summary>
public sealed record SessionRequestMessage : IProtocolMessage
{
    public string Type => MessageTypes.SessionRequest;
    public required string SessionToken { get; init; }
    public required Permission RequestedPermissions { get; init; }
}

/// <summary>
/// Sent by the host to approve a session with granted permissions.
/// </summary>
public sealed record SessionApprovedMessage : IProtocolMessage
{
    public string Type => MessageTypes.SessionApproved;
    public required string SessionId { get; init; }
    public required Permission GrantedPermissions { get; init; }
    public required DisplayInfo[] Displays { get; init; }
    public required string ActiveDisplayId { get; init; }
}

/// <summary>
/// Sent by the host to reject a session request.
/// </summary>
public sealed record SessionRejectedMessage : IProtocolMessage
{
    public string Type => MessageTypes.SessionRejected;
    public string? Reason { get; init; }
}

/// <summary>
/// Sent by either side to end a session.
/// </summary>
public sealed record SessionEndedMessage : IProtocolMessage
{
    public string Type => MessageTypes.SessionEnded;
    public required string Reason { get; init; }
    public required string InitiatedBy { get; init; }
}

/// <summary>
/// Sent by the host to update permissions mid-session.
/// </summary>
public sealed record PermissionUpdateMessage : IProtocolMessage
{
    public string Type => MessageTypes.PermissionUpdate;
    public required Permission Permissions { get; init; }
}
