namespace LPUPlus.Protocol.Messages;

// ─────────────────────────────────────────────────────────────
//  FILE MESSAGES (DataChannel: "files", reliable)
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Request to list directory contents.
/// </summary>
public sealed record FileListRequestMessage : IProtocolMessage
{
    public string Type => MessageTypes.FileListRequest;
    public required string Path { get; init; }
}

/// <summary>
/// Directory listing response.
/// </summary>
public sealed record FileListResponseMessage : IProtocolMessage
{
    public string Type => MessageTypes.FileListResponse;
    public required string Path { get; init; }
    public required FileEntry[] Entries { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Request to download a file from host.
/// </summary>
public sealed record FileDownloadRequestMessage : IProtocolMessage
{
    public string Type => MessageTypes.FileDownloadRequest;
    public required string Path { get; init; }
}

/// <summary>
/// Start of a file download stream.
/// </summary>
public sealed record FileDownloadStartMessage : IProtocolMessage
{
    public string Type => MessageTypes.FileDownloadStart;
    public required string TransferId { get; init; }
    public required string Name { get; init; }
    public required long Size { get; init; }
    public required string Sha256 { get; init; }
}

/// <summary>
/// Start of a file upload from receiver to host.
/// </summary>
public sealed record FileUploadStartMessage : IProtocolMessage
{
    public string Type => MessageTypes.FileUploadStart;
    public required string Name { get; init; }
    public required long Size { get; init; }
    public required string DestinationPath { get; init; }
}

/// <summary>
/// Acknowledgment that upload can proceed.
/// </summary>
public sealed record FileUploadAckMessage : IProtocolMessage
{
    public string Type => MessageTypes.FileUploadAck;
    public required string TransferId { get; init; }
}

/// <summary>
/// A chunk of file data (used in both upload and download).
/// </summary>
public sealed record FileChunkMessage : IProtocolMessage
{
    public string Type => MessageTypes.FileChunk;
    public required string TransferId { get; init; }
    public required long Offset { get; init; }
    public required byte[] Data { get; init; }
}

/// <summary>
/// Acknowledge receipt of a chunk.
/// </summary>
public sealed record FileChunkAckMessage : IProtocolMessage
{
    public string Type => MessageTypes.FileChunkAck;
    public required string TransferId { get; init; }
    public required long Offset { get; init; }
}

/// <summary>
/// Transfer completed with integrity hash.
/// </summary>
public sealed record FileTransferCompleteMessage : IProtocolMessage
{
    public string Type => MessageTypes.FileTransferComplete;
    public required string TransferId { get; init; }
    public required string Sha256 { get; init; }
    public required bool Success { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Delete a file on the host.
/// </summary>
public sealed record FileDeleteMessage : IProtocolMessage
{
    public string Type => MessageTypes.FileDelete;
    public required string Path { get; init; }
}

/// <summary>
/// Rename/move a file on the host.
/// </summary>
public sealed record FileRenameMessage : IProtocolMessage
{
    public string Type => MessageTypes.FileRename;
    public required string OldPath { get; init; }
    public required string NewPath { get; init; }
}

/// <summary>
/// Create a folder on the host.
/// </summary>
public sealed record FileCreateFolderMessage : IProtocolMessage
{
    public string Type => MessageTypes.FileCreateFolder;
    public required string Path { get; init; }
}

/// <summary>
/// Generic file operation result.
/// </summary>
public sealed record FileOperationResultMessage : IProtocolMessage
{
    public string Type => MessageTypes.FileOperationResult;
    public required bool Success { get; init; }
    public string? Error { get; init; }
}

// ── Supporting types ────────────────────────────────────────

public sealed record FileEntry
{
    public required string Name { get; init; }
    public required bool IsDirectory { get; init; }
    public required long Size { get; init; }
    public required DateTimeOffset Modified { get; init; }
}
