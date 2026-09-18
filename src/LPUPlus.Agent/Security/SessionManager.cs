using LPUPlus.Protocol.Models;

namespace LPUPlus.Agent.Security;

/// <summary>
/// Manages the session lifecycle using the formal state machine.
/// Validates all state transitions and emits change events.
/// </summary>
public sealed class SessionManager
{
    private SessionState _state = SessionState.Disconnected;
    private readonly object _lock = new();

    /// <summary>Session ID (set when session is approved).</summary>
    public string? SessionId { get; private set; }

    /// <summary>Connected controller's display name.</summary>
    public string? ControllerName { get; private set; }

    /// <summary>When the current session started.</summary>
    public DateTimeOffset? SessionStartTime { get; private set; }

    /// <summary>Current session state.</summary>
    public SessionState State
    {
        get { lock (_lock) return _state; }
    }

    /// <summary>Whether a session is currently active.</summary>
    public bool IsActive => State == SessionState.Active;

    /// <summary>Whether we are connected (Active or Connected).</summary>
    public bool IsConnected => State is SessionState.Active or SessionState.Connected;

    /// <summary>
    /// Fired when session state changes. Args: (oldState, newState).
    /// </summary>
    public event Action<SessionState, SessionState>? StateChanged;

    /// <summary>
    /// Transition to a new state. Throws if the transition is invalid.
    /// </summary>
    public void TransitionTo(SessionState newState)
    {
        lock (_lock)
        {
            var oldState = _state;
            SessionStateMachine.ValidateTransition(oldState, newState);
            _state = newState;

            // Track session timing
            if (newState == SessionState.Active)
                SessionStartTime = DateTimeOffset.UtcNow;
            else if (newState == SessionState.Disconnected)
            {
                SessionId = null;
                ControllerName = null;
                SessionStartTime = null;
            }

            StateChanged?.Invoke(oldState, newState);
        }
    }

    /// <summary>
    /// Set session info when approved.
    /// </summary>
    public void SetSessionInfo(string sessionId, string controllerName)
    {
        SessionId = sessionId;
        ControllerName = controllerName;
    }

    /// <summary>
    /// Force disconnect (e.g., on error or host termination).
    /// Bypasses state machine validation for emergency cleanup.
    /// </summary>
    public void ForceDisconnect()
    {
        lock (_lock)
        {
            var oldState = _state;
            _state = SessionState.Disconnected;
            SessionId = null;
            ControllerName = null;
            SessionStartTime = null;
            StateChanged?.Invoke(oldState, SessionState.Disconnected);
        }
    }

    /// <summary>
    /// Get session duration (if active).
    /// </summary>
    public TimeSpan? GetSessionDuration()
    {
        return SessionStartTime.HasValue
            ? DateTimeOffset.UtcNow - SessionStartTime.Value
            : null;
    }
}
