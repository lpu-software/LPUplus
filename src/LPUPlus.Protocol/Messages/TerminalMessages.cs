namespace LPUPlus.Protocol.Messages;

// ─────────────────────────────────────────────────────────────
//  TERMINAL MESSAGES (DataChannel: "terminal", reliable)
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Request to start a terminal session.
/// </summary>
public sealed record TerminalStartMessage : IProtocolMessage
{
    public string Type => MessageTypes.TerminalStart;
    public int Cols { get; init; } = 120;
    public int Rows { get; init; } = 40;
    /// <summary>"auto" = platform default (PowerShell on Windows, zsh on macOS).</summary>
    public string Shell { get; init; } = "auto";
}

/// <summary>
/// Terminal started confirmation.
/// </summary>
public sealed record TerminalStartedMessage : IProtocolMessage
{
    public string Type => MessageTypes.TerminalStarted;
    public required string TerminalId { get; init; }
    public required string Shell { get; init; }
}

/// <summary>
/// User input to the terminal (stdin).
/// </summary>
public sealed record TerminalInputMessage : IProtocolMessage
{
    public string Type => MessageTypes.TerminalInput;
    public required string Data { get; init; }
}

/// <summary>
/// Terminal output (stdout/stderr) streamed to the browser.
/// </summary>
public sealed record TerminalOutputMessage : IProtocolMessage
{
    public string Type => MessageTypes.TerminalOutput;
    public required string Data { get; init; }
}

/// <summary>
/// Terminal resize request from the browser.
/// </summary>
public sealed record TerminalResizeMessage : IProtocolMessage
{
    public string Type => MessageTypes.TerminalResize;
    public required int Cols { get; init; }
    public required int Rows { get; init; }
}

/// <summary>
/// Terminal session has ended.
/// </summary>
public sealed record TerminalExitMessage : IProtocolMessage
{
    public string Type => MessageTypes.TerminalExit;
    public required int ExitCode { get; init; }
}

/// <summary>
/// Terminal error.
/// </summary>
public sealed record TerminalErrorMessage : IProtocolMessage
{
    public string Type => MessageTypes.TerminalError;
    public required string Error { get; init; }
}
