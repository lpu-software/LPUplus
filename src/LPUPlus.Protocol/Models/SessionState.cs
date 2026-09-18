namespace LPUPlus.Protocol.Models;

/// <summary>
/// Formal session state machine as defined in the architecture.
/// Invalid transitions should be rejected.
/// </summary>
public enum SessionState
{
    Disconnected,
    Connecting,
    Authenticating,
    WaitingForHost,
    Authorized,
    NegotiatingWebRtc,
    Connected,
    Active,
    Disconnecting,
}

/// <summary>
/// Session state machine — validates allowed transitions.
/// </summary>
public static class SessionStateMachine
{
    private static readonly Dictionary<SessionState, SessionState[]> AllowedTransitions = new()
    {
        [SessionState.Disconnected] = [SessionState.Connecting],
        [SessionState.Connecting] = [SessionState.Authenticating, SessionState.Disconnected],
        [SessionState.Authenticating] = [SessionState.WaitingForHost, SessionState.Disconnected],
        [SessionState.WaitingForHost] = [SessionState.Authorized, SessionState.Disconnected],
        [SessionState.Authorized] = [SessionState.NegotiatingWebRtc, SessionState.Disconnected],
        [SessionState.NegotiatingWebRtc] = [SessionState.Connected, SessionState.Disconnected],
        [SessionState.Connected] = [SessionState.Active, SessionState.Disconnecting, SessionState.Disconnected],
        [SessionState.Active] = [SessionState.Disconnecting, SessionState.Disconnected],
        [SessionState.Disconnecting] = [SessionState.Disconnected],
    };

    /// <summary>
    /// Returns true if transitioning from <paramref name="from"/> to <paramref name="to"/> is valid.
    /// </summary>
    public static bool CanTransition(SessionState from, SessionState to)
    {
        return AllowedTransitions.TryGetValue(from, out var targets) &&
               Array.IndexOf(targets, to) >= 0;
    }

    /// <summary>
    /// Validates a transition and throws if invalid.
    /// </summary>
    public static void ValidateTransition(SessionState from, SessionState to)
    {
        if (!CanTransition(from, to))
            throw new InvalidOperationException(
                $"Invalid session state transition: {from} → {to}");
    }
}
