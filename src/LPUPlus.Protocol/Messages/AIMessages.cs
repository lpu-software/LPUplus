namespace LPUPlus.Protocol.Messages;

// ─────────────────────────────────────────────────────────────
//  AI MESSAGES (HTTP API, not DataChannel)
// ─────────────────────────────────────────────────────────────

/// <summary>
/// AI analysis request. Routed through the server (keys never exposed to browser).
/// </summary>
public sealed record AIRequestMessage : IProtocolMessage
{
    public string Type => MessageTypes.AIRequest;
    public required string RequestId { get; init; }
    public required AIRequestType RequestType { get; init; }
    public required string Prompt { get; init; }

    /// <summary>Base64-encoded screenshot JPEG (for screen/region analysis). Null for text-only.</summary>
    public string? ImageBase64 { get; init; }

    /// <summary>Terminal output context (for terminal analysis).</summary>
    public string? TerminalContext { get; init; }

    /// <summary>Performance context (for combined analysis).</summary>
    public string? PerformanceContext { get; init; }
}

/// <summary>
/// AI response from the provider router.
/// </summary>
public sealed record AIResponseMessage : IProtocolMessage
{
    public string Type => MessageTypes.AIResponse;
    public required string RequestId { get; init; }
    public required bool Success { get; init; }
    public string? Response { get; init; }
    public string? Provider { get; init; }
    public string? Model { get; init; }
    public string? Error { get; init; }
    public long LatencyMs { get; init; }
}

/// <summary>
/// Web research request.
/// </summary>
public sealed record ResearchRequestMessage : IProtocolMessage
{
    public string Type => MessageTypes.ResearchRequest;
    public required string RequestId { get; init; }
    public required string Query { get; init; }
}

/// <summary>
/// Web research response with sources and AI summary.
/// </summary>
public sealed record ResearchResponseMessage : IProtocolMessage
{
    public string Type => MessageTypes.ResearchResponse;
    public required string RequestId { get; init; }
    public required bool Success { get; init; }
    public string? Summary { get; init; }
    public ResearchSource[]? Sources { get; init; }
    public string? Error { get; init; }
}

// ── Supporting types ────────────────────────────────────────

public enum AIRequestType
{
    /// <summary>General chat / question.</summary>
    Chat,
    /// <summary>Analyze a full screen screenshot.</summary>
    ScreenAnalysis,
    /// <summary>Analyze a selected screen region.</summary>
    RegionAnalysis,
    /// <summary>Analyze terminal output.</summary>
    TerminalAnalysis,
    /// <summary>Combined context (screen + terminal + metrics).</summary>
    CombinedAnalysis,
}

public sealed record ResearchSource
{
    public required string Title { get; init; }
    public required string Url { get; init; }
    public string? Snippet { get; init; }
}
