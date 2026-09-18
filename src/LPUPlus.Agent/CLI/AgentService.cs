using System.Runtime.InteropServices;
using LPUPlus.Agent.Security;
using LPUPlus.Agent.Signaling;
using LPUPlus.Agent.WebRTC;
using LPUPlus.Protocol.Messages;

namespace LPUPlus.Agent.CLI;

/// <summary>
/// Core agent service that orchestrates startup, pairing, and session management.
/// </summary>
public sealed class AgentService : IDisposable
{
    private readonly SessionManager _sessionManager = new();
    private readonly PermissionManager _permissionManager = new();
    private readonly SignalingClient _signalingClient;
    private readonly WebRtcManager _webRtcManager;
    private string? _currentPairingCode;
    private readonly string _hmacSecret;
    private CancellationTokenSource? _runCts;

    public AgentService()
    {
        _hmacSecret = "LPUPlusSharedSecret";
        
        // TODO: Load from config, hardcoded for Phase 3/4 dev
        _signalingClient = new SignalingClient("ws://localhost:5121/ws");
        _signalingClient.OnSessionRequest += HandleSessionRequestAsync;
        _signalingClient.OnDisconnected += () => Stop("Disconnected from signaling server");

        _webRtcManager = new WebRtcManager(_signalingClient);
    }

    /// <summary>Current session state.</summary>
    public SessionManager Session => _sessionManager;

    /// <summary>Current permissions.</summary>
    public PermissionManager Permissions => _permissionManager;

    /// <summary>Current pairing code (null if not generated).</summary>
    public string? PairingCode => _currentPairingCode;

    /// <summary>
    /// Start the agent: generate pairing code, display status, wait for connections.
    /// </summary>
    public async Task StartAsync(CancellationToken ct)
    {
        _runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        ConsoleUI.ShowBanner();

        // Generate pairing code
        _currentPairingCode = PairingCodeGenerator.Generate();
        var codeHash = PairingCodeGenerator.HashCode(_currentPairingCode, _hmacSecret);

        // Get device info
        var deviceName = Environment.MachineName;
        var platform = GetPlatformString();
        var display = GetPrimaryDisplayString();

        ConsoleUI.ShowStartupStatus(deviceName, platform, display, _currentPairingCode);

        // Set state to Connecting (ready to accept signaling connections)
        _sessionManager.TransitionTo(Protocol.Models.SessionState.Connecting);

        try
        {
            await _signalingClient.ConnectAsync(ct);
            _sessionManager.TransitionTo(Protocol.Models.SessionState.Authenticating);
            
            // Register with signaling server
            await _signalingClient.SendMessageAsync(new HostRegisterMessage
            {
                DeviceId = Environment.MachineName,
                DeviceName = deviceName,
                Platform = platform,
                PairingCodeHash = codeHash
            }, ct);

            _sessionManager.TransitionTo(Protocol.Models.SessionState.WaitingForHost);

            // Wait for cancellation
            await Task.Delay(Timeout.Infinite, _runCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        finally
        {
            Stop("Agent shutdown");
        }
    }

    /// <summary>
    /// Stop the agent and clean up all resources.
    /// </summary>
    public void Stop(string reason = "Host terminated session")
    {
        _permissionManager.RevokeAll();
        _sessionManager.ForceDisconnect();
        _currentPairingCode = null;
        _runCts?.Cancel();

        ConsoleUI.ShowSessionEnded(reason);
    }

    /// <summary>
    /// Regenerate the pairing code.
    /// </summary>
    public string RegeneratePairingCode()
    {
        _currentPairingCode = PairingCodeGenerator.Generate();
        return _currentPairingCode;
    }

    /// <summary>
    /// Auto-approve the session since the agent runs in the background without terminal input.
    /// </summary>
    public bool ApproveSession(string controllerName)
    {
        // Auto-approve instead of blocking on Console.ReadLine
        bool approved = true;

        if (approved)
        {
            _sessionManager.SetSessionInfo(
                sessionId: Guid.NewGuid().ToString("N")[..12],
                controllerName: controllerName);

            // Default: screen view + mouse + keyboard
            _permissionManager.Grant(Protocol.Models.Permission.ScreenView);
            _permissionManager.Grant(Protocol.Models.Permission.MouseControl);
            _permissionManager.Grant(Protocol.Models.Permission.KeyboardControl);

            ConsoleUI.ShowActiveSession(
                controllerName,
                _permissionManager.HasPermission(Protocol.Models.Permission.ScreenView),
                _permissionManager.HasPermission(Protocol.Models.Permission.MouseControl),
                _permissionManager.HasPermission(Protocol.Models.Permission.KeyboardControl));
        }

        return approved;
    }

    /// <summary>
    /// Display current status.
    /// </summary>
    public void ShowStatus()
    {
        ConsoleUI.ShowStatus(
            _sessionManager.State.ToString(),
            _sessionManager.SessionId,
            _sessionManager.ControllerName,
            _sessionManager.GetSessionDuration());
    }

    private async Task HandleSessionRequestAsync(SessionRequestMessage req)
    {
        try
        {
            // For Phase 3, we auto-approve or use the prompt.
            // In the real CLI, blocking on ShowApprovalPrompt inside this handler 
            // will block the WebSocket receive loop. For now, we will simulate it.
            var approved = ApproveSession("Remote Controller");
            
            if (approved)
            {
                await _signalingClient.SendMessageAsync(new SessionApprovedMessage
                {
                    SessionId = req.SessionToken, // We route it back using the token
                    GrantedPermissions = _permissionManager.GrantedPermissions,
                    ActiveDisplayId = "DISPLAY1",
                    Displays = [] // Empty for now
                });
                _sessionManager.TransitionTo(Protocol.Models.SessionState.Authorized);
            }
            else
            {
                await _signalingClient.SendMessageAsync(new SessionRejectedMessage
                {
                    Reason = "Host rejected the connection"
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("EXCEPTION IN HANDLER: " + ex);
            throw;
        }
    }

    public void Dispose()
    {
        _runCts?.Cancel();
        _runCts?.Dispose();
        _webRtcManager.Dispose();
        _signalingClient.Dispose();
    }

    // ── Platform helpers ──

    private static string GetPlatformString()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return $"Windows ({RuntimeInformation.OSArchitecture})";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return $"macOS ({RuntimeInformation.OSArchitecture})";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return $"Linux ({RuntimeInformation.OSArchitecture})";
        return RuntimeInformation.OSDescription;
    }

    private static string GetPrimaryDisplayString()
    {
        // Placeholder — will be replaced with real display enumeration in Phase 12
        return "Primary Display";
    }
}
