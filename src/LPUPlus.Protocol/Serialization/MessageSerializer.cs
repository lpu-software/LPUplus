using System.Text.Json;
using System.Text.Json.Serialization;
using LPUPlus.Protocol.Messages;

namespace LPUPlus.Protocol.Serialization;

/// <summary>
/// Versioned message envelope. Every message sent over WebSocket or DataChannel
/// is wrapped in this envelope for forward compatibility.
/// </summary>
public sealed record MessageEnvelope
{
    /// <summary>Protocol version for forward compatibility.</summary>
    [JsonPropertyName("v")]
    public int Version { get; init; } = MessageTypes.ProtocolVersion;

    /// <summary>Message type discriminator.</summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>The actual message payload as a raw JSON element.</summary>
    [JsonPropertyName("payload")]
    public required JsonElement Payload { get; init; }

    /// <summary>Timestamp in milliseconds (Unix epoch).</summary>
    [JsonPropertyName("ts")]
    public long TimestampMs { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

/// <summary>
/// High-performance message serializer using System.Text.Json.
/// Handles envelope wrapping/unwrapping and polymorphic deserialization.
/// </summary>
public static class MessageSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>
    /// Maps message type strings to their concrete C# types for deserialization.
    /// </summary>
    private static readonly Dictionary<string, Type> TypeMap = new()
    {
        // Auth
        [MessageTypes.HostRegister] = typeof(HostRegisterMessage),
        [MessageTypes.PairRequest] = typeof(PairRequestMessage),
        [MessageTypes.PairResponse] = typeof(PairResponseMessage),

        // Session
        [MessageTypes.SessionRequest] = typeof(SessionRequestMessage),
        [MessageTypes.SessionApproved] = typeof(SessionApprovedMessage),
        [MessageTypes.SessionRejected] = typeof(SessionRejectedMessage),
        [MessageTypes.SessionEnded] = typeof(SessionEndedMessage),
        [MessageTypes.PermissionUpdate] = typeof(PermissionUpdateMessage),

        // Signaling
        [MessageTypes.SdpOffer] = typeof(SdpOfferMessage),
        [MessageTypes.SdpAnswer] = typeof(SdpAnswerMessage),
        [MessageTypes.IceCandidate] = typeof(IceCandidateMessage),

        // Cursor
        [MessageTypes.CursorUpdate] = typeof(CursorUpdateMessage),

        // Input
        [MessageTypes.MouseMove] = typeof(MouseMoveMessage),
        [MessageTypes.MouseDown] = typeof(MouseDownMessage),
        [MessageTypes.MouseUp] = typeof(MouseUpMessage),
        [MessageTypes.MouseWheel] = typeof(MouseWheelMessage),
        [MessageTypes.KeyDown] = typeof(KeyDownMessage),
        [MessageTypes.KeyUp] = typeof(KeyUpMessage),

        // Display
        [MessageTypes.MonitorList] = typeof(MonitorListMessage),
        [MessageTypes.MonitorSwitch] = typeof(MonitorSwitchMessage),
        [MessageTypes.DisplayChanged] = typeof(DisplayChangedMessage),

        // Files
        [MessageTypes.FileListRequest] = typeof(FileListRequestMessage),
        [MessageTypes.FileListResponse] = typeof(FileListResponseMessage),
        [MessageTypes.FileDownloadRequest] = typeof(FileDownloadRequestMessage),
        [MessageTypes.FileDownloadStart] = typeof(FileDownloadStartMessage),
        [MessageTypes.FileUploadStart] = typeof(FileUploadStartMessage),
        [MessageTypes.FileUploadAck] = typeof(FileUploadAckMessage),
        [MessageTypes.FileChunk] = typeof(FileChunkMessage),
        [MessageTypes.FileChunkAck] = typeof(FileChunkAckMessage),
        [MessageTypes.FileTransferComplete] = typeof(FileTransferCompleteMessage),
        [MessageTypes.FileDelete] = typeof(FileDeleteMessage),
        [MessageTypes.FileRename] = typeof(FileRenameMessage),
        [MessageTypes.FileCreateFolder] = typeof(FileCreateFolderMessage),
        [MessageTypes.FileOperationResult] = typeof(FileOperationResultMessage),

        // Terminal
        [MessageTypes.TerminalStart] = typeof(TerminalStartMessage),
        [MessageTypes.TerminalStarted] = typeof(TerminalStartedMessage),
        [MessageTypes.TerminalInput] = typeof(TerminalInputMessage),
        [MessageTypes.TerminalOutput] = typeof(TerminalOutputMessage),
        [MessageTypes.TerminalResize] = typeof(TerminalResizeMessage),
        [MessageTypes.TerminalExit] = typeof(TerminalExitMessage),
        [MessageTypes.TerminalError] = typeof(TerminalErrorMessage),

        // Performance
        [MessageTypes.PerformanceUpdate] = typeof(PerformanceUpdateMessage),

        // AI
        [MessageTypes.AIRequest] = typeof(AIRequestMessage),
        [MessageTypes.AIResponse] = typeof(AIResponseMessage),
        [MessageTypes.ResearchRequest] = typeof(ResearchRequestMessage),
        [MessageTypes.ResearchResponse] = typeof(ResearchResponseMessage),

        // Error
        [MessageTypes.Error] = typeof(ErrorMessage),
    };

    /// <summary>
    /// Serialize a protocol message into a JSON envelope string.
    /// </summary>
    public static string Serialize(IProtocolMessage message)
    {
        var payloadJson = JsonSerializer.SerializeToElement(message, message.GetType(), Options);

        var envelope = new MessageEnvelope
        {
            Type = message.Type,
            Payload = payloadJson,
        };

        return JsonSerializer.Serialize(envelope, Options);
    }

    /// <summary>
    /// Serialize a protocol message into UTF-8 bytes (for DataChannel).
    /// </summary>
    public static byte[] SerializeToBytes(IProtocolMessage message)
    {
        var payloadJson = JsonSerializer.SerializeToElement(message, message.GetType(), Options);

        var envelope = new MessageEnvelope
        {
            Type = message.Type,
            Payload = payloadJson,
        };

        return JsonSerializer.SerializeToUtf8Bytes(envelope, Options);
    }

    /// <summary>
    /// Deserialize a JSON envelope string into a typed protocol message.
    /// Returns null if the message type is unrecognized.
    /// </summary>
    public static IProtocolMessage? Deserialize(string json)
    {
        var envelope = JsonSerializer.Deserialize<MessageEnvelope>(json, Options);
        if (envelope is null) return null;

        return DeserializeFromEnvelope(envelope);
    }

    /// <summary>
    /// Deserialize UTF-8 bytes into a typed protocol message.
    /// </summary>
    public static IProtocolMessage? Deserialize(ReadOnlySpan<byte> utf8Json)
    {
        var envelope = JsonSerializer.Deserialize<MessageEnvelope>(utf8Json, Options);
        if (envelope is null) return null;

        return DeserializeFromEnvelope(envelope);
    }

    /// <summary>
    /// Extract just the message type from a JSON string without full deserialization.
    /// Useful for routing decisions.
    /// </summary>
    public static string? PeekType(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("type", out var typeProp))
                return typeProp.GetString();
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Check if a message type string is known/registered.
    /// </summary>
    public static bool IsKnownType(string type) => TypeMap.ContainsKey(type);

    private static IProtocolMessage? DeserializeFromEnvelope(MessageEnvelope envelope)
    {
        if (!TypeMap.TryGetValue(envelope.Type, out var targetType))
            return null;

        var rawJson = envelope.Payload.GetRawText();
        return (IProtocolMessage?)JsonSerializer.Deserialize(rawJson, targetType, Options);
    }
}
