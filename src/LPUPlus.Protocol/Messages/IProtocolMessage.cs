namespace LPUPlus.Protocol.Messages;

/// <summary>
/// Base interface for all protocol messages.
/// Every message has a Type discriminator for polymorphic deserialization.
/// </summary>
public interface IProtocolMessage
{
    /// <summary>
    /// Message type discriminator string (e.g., "mouse_move", "cursor_update").
    /// Used for routing and deserialization.
    /// </summary>
    string Type { get; }
}
