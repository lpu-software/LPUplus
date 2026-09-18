using System.Text.Json;
using LPUPlus.Protocol.Messages;
using LPUPlus.Protocol.Models;
using LPUPlus.Protocol.Serialization;

namespace LPUPlus.Protocol.Tests;

public class MessageSerializerTests
{
    // ─────────────────────────────────────────────────────────
    //  ENVELOPE ROUND-TRIP TESTS
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void Serialize_MouseMove_RoundTrips()
    {
        var original = new MouseMoveMessage
        {
            NormX = 0.5,
            NormY = 0.75,
            MonitorId = "DISPLAY1",
            TimestampMs = 1234567890,
        };

        var json = MessageSerializer.Serialize(original);
        var deserialized = MessageSerializer.Deserialize(json);

        Assert.NotNull(deserialized);
        var result = Assert.IsType<MouseMoveMessage>(deserialized);
        Assert.Equal(original.NormX, result.NormX);
        Assert.Equal(original.NormY, result.NormY);
        Assert.Equal(original.MonitorId, result.MonitorId);
        Assert.Equal(original.TimestampMs, result.TimestampMs);
    }

    [Fact]
    public void Serialize_CursorUpdate_RoundTrips()
    {
        var original = new CursorUpdateMessage
        {
            X = 960.5,
            Y = 540.25,
            MonitorId = "DISPLAY1",
            Visible = true,
            CursorType = CursorType.IBeam,
            HotspotX = 4,
            HotspotY = 8,
            TimestampUs = 999888777,
        };

        var json = MessageSerializer.Serialize(original);
        var deserialized = MessageSerializer.Deserialize(json);

        Assert.NotNull(deserialized);
        var result = Assert.IsType<CursorUpdateMessage>(deserialized);
        Assert.Equal(original.X, result.X);
        Assert.Equal(original.Y, result.Y);
        Assert.Equal(CursorType.IBeam, result.CursorType);
        Assert.True(result.Visible);
    }

    [Fact]
    public void Serialize_SessionApproved_WithDisplays_RoundTrips()
    {
        var original = new SessionApprovedMessage
        {
            SessionId = "session-123",
            GrantedPermissions = Permission.ScreenView | Permission.MouseControl | Permission.KeyboardControl,
            ActiveDisplayId = "DISPLAY1",
            Displays =
            [
                new DisplayInfo
                {
                    Id = "DISPLAY1",
                    Name = "Primary Monitor",
                    Width = 1920,
                    Height = 1080,
                    LogicalWidth = 1920,
                    LogicalHeight = 1080,
                    X = 0,
                    Y = 0,
                    ScaleFactor = 1.0,
                    IsPrimary = true,
                },
                new DisplayInfo
                {
                    Id = "DISPLAY2",
                    Name = "Secondary Monitor",
                    Width = 2560,
                    Height = 1440,
                    LogicalWidth = 1706,
                    LogicalHeight = 960,
                    X = 1920,
                    Y = 0,
                    ScaleFactor = 1.5,
                    IsPrimary = false,
                },
            ],
        };

        var json = MessageSerializer.Serialize(original);
        var deserialized = MessageSerializer.Deserialize(json);

        Assert.NotNull(deserialized);
        var result = Assert.IsType<SessionApprovedMessage>(deserialized);
        Assert.Equal("session-123", result.SessionId);
        Assert.Equal(2, result.Displays.Length);
        Assert.Equal(1.5, result.Displays[1].ScaleFactor);
        Assert.True(result.GrantedPermissions.HasFlag(Permission.MouseControl));
        Assert.False(result.GrantedPermissions.HasFlag(Permission.Terminal));
    }

    [Fact]
    public void Serialize_KeyDown_WithModifiers_RoundTrips()
    {
        var original = new KeyDownMessage
        {
            Code = "KeyA",
            Key = "a",
            Modifiers = new KeyModifiers { Ctrl = true, Shift = true },
            TimestampMs = 555,
        };

        var json = MessageSerializer.Serialize(original);
        var deserialized = MessageSerializer.Deserialize(json);

        Assert.NotNull(deserialized);
        var result = Assert.IsType<KeyDownMessage>(deserialized);
        Assert.Equal("KeyA", result.Code);
        Assert.True(result.Modifiers.Ctrl);
        Assert.True(result.Modifiers.Shift);
        Assert.False(result.Modifiers.Alt);
        Assert.False(result.Modifiers.Meta);
    }

    [Fact]
    public void Serialize_PerformanceUpdate_RoundTrips()
    {
        var original = new PerformanceUpdateMessage
        {
            ConnectionType = ConnectionType.Direct,
            RttMs = 28.5,
            JitterMs = 3.2,
            PacketLossPercent = 0.1,
            Fps = 59.7,
            ResolutionWidth = 1920,
            ResolutionHeight = 1080,
            BitrateMbps = 8.1,
            Codec = "H.264",
            CaptureMs = 4.0,
            EncodeMs = 6.0,
            NetworkMs = 28.0,
            DecodeMs = 4.0,
            RenderMs = 3.0,
            CpuPercent = 42.5,
            GpuPercent = 65.0,
            TimestampMs = 1234567890,
        };

        var json = MessageSerializer.Serialize(original);
        var deserialized = MessageSerializer.Deserialize(json);

        Assert.NotNull(deserialized);
        var result = Assert.IsType<PerformanceUpdateMessage>(deserialized);
        Assert.Equal(ConnectionType.Direct, result.ConnectionType);
        Assert.Equal(59.7, result.Fps);
        Assert.Equal("H.264", result.Codec);
        Assert.Equal(42.5, result.CpuPercent);
    }

    // ─────────────────────────────────────────────────────────
    //  BYTE SERIALIZATION TESTS
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void SerializeToBytes_Deserializes_Correctly()
    {
        var original = new MouseWheelMessage
        {
            NormX = 0.3,
            NormY = 0.6,
            DeltaX = 0,
            DeltaY = -120,
            MonitorId = "DISPLAY1",
            TimestampMs = 999,
        };

        var bytes = MessageSerializer.SerializeToBytes(original);
        var deserialized = MessageSerializer.Deserialize(bytes);

        Assert.NotNull(deserialized);
        var result = Assert.IsType<MouseWheelMessage>(deserialized);
        Assert.Equal(-120, result.DeltaY);
    }

    // ─────────────────────────────────────────────────────────
    //  ENVELOPE STRUCTURE TESTS
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void Envelope_Contains_Version_Type_Payload_Timestamp()
    {
        var msg = new PairRequestMessage { PairingCode = "7K4P-92MX" };
        var json = MessageSerializer.Serialize(msg);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("v", out var version));
        Assert.Equal(MessageTypes.ProtocolVersion, version.GetInt32());

        Assert.True(root.TryGetProperty("type", out var type));
        Assert.Equal("pair_request", type.GetString());

        Assert.True(root.TryGetProperty("payload", out _));
        Assert.True(root.TryGetProperty("ts", out _));
    }

    [Fact]
    public void PeekType_Returns_CorrectType()
    {
        var msg = new TerminalInputMessage { Data = "ls -la\r" };
        var json = MessageSerializer.Serialize(msg);

        var type = MessageSerializer.PeekType(json);
        Assert.Equal(MessageTypes.TerminalInput, type);
    }

    [Fact]
    public void PeekType_Returns_Null_ForInvalidJson()
    {
        var type = MessageSerializer.PeekType("not json at all");
        Assert.Null(type);
    }

    // ─────────────────────────────────────────────────────────
    //  UNKNOWN TYPE HANDLING
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void Deserialize_UnknownType_ReturnsNull()
    {
        var json = """{"v":1,"type":"future_message","payload":{},"ts":0}""";
        var result = MessageSerializer.Deserialize(json);
        Assert.Null(result);
    }

    [Fact]
    public void IsKnownType_Returns_Correct()
    {
        Assert.True(MessageSerializer.IsKnownType(MessageTypes.MouseMove));
        Assert.True(MessageSerializer.IsKnownType(MessageTypes.CursorUpdate));
        Assert.False(MessageSerializer.IsKnownType("nonexistent_type"));
    }

    // ─────────────────────────────────────────────────────────
    //  SESSION STATE MACHINE TESTS
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void SessionStateMachine_AllowsValidTransitions()
    {
        Assert.True(SessionStateMachine.CanTransition(SessionState.Disconnected, SessionState.Connecting));
        Assert.True(SessionStateMachine.CanTransition(SessionState.Connecting, SessionState.Authenticating));
        Assert.True(SessionStateMachine.CanTransition(SessionState.Authenticating, SessionState.WaitingForHost));
        Assert.True(SessionStateMachine.CanTransition(SessionState.WaitingForHost, SessionState.Authorized));
        Assert.True(SessionStateMachine.CanTransition(SessionState.Authorized, SessionState.NegotiatingWebRtc));
        Assert.True(SessionStateMachine.CanTransition(SessionState.NegotiatingWebRtc, SessionState.Connected));
        Assert.True(SessionStateMachine.CanTransition(SessionState.Connected, SessionState.Active));
        Assert.True(SessionStateMachine.CanTransition(SessionState.Active, SessionState.Disconnecting));
        Assert.True(SessionStateMachine.CanTransition(SessionState.Disconnecting, SessionState.Disconnected));
    }

    [Fact]
    public void SessionStateMachine_RejectsInvalidTransitions()
    {
        Assert.False(SessionStateMachine.CanTransition(SessionState.Disconnected, SessionState.Active));
        Assert.False(SessionStateMachine.CanTransition(SessionState.Connecting, SessionState.Active));
        Assert.False(SessionStateMachine.CanTransition(SessionState.Active, SessionState.Connecting));
        Assert.False(SessionStateMachine.CanTransition(SessionState.Disconnecting, SessionState.Active));
    }

    [Fact]
    public void SessionStateMachine_ValidateTransition_ThrowsOnInvalid()
    {
        Assert.Throws<InvalidOperationException>(() =>
            SessionStateMachine.ValidateTransition(SessionState.Disconnected, SessionState.Active));
    }

    [Fact]
    public void SessionStateMachine_AllowsDisconnect_FromAnyActiveState()
    {
        // Every state (except Disconnected itself) should allow transition to Disconnected
        var activeStates = new[]
        {
            SessionState.Connecting,
            SessionState.Authenticating,
            SessionState.WaitingForHost,
            SessionState.Authorized,
            SessionState.NegotiatingWebRtc,
            SessionState.Connected,
            SessionState.Active,
            SessionState.Disconnecting,
        };

        foreach (var state in activeStates)
        {
            Assert.True(
                SessionStateMachine.CanTransition(state, SessionState.Disconnected),
                $"Expected {state} → Disconnected to be valid");
        }
    }

    // ─────────────────────────────────────────────────────────
    //  PERMISSION TESTS
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void Permission_Flags_Work_Correctly()
    {
        var perms = Permission.ScreenView | Permission.MouseControl;

        Assert.True(perms.HasFlag(Permission.ScreenView));
        Assert.True(perms.HasFlag(Permission.MouseControl));
        Assert.False(perms.HasFlag(Permission.Terminal));
        Assert.False(perms.HasFlag(Permission.FileWrite));
    }

    [Fact]
    public void PermissionSets_Contain_ExpectedFlags()
    {
        Assert.True(PermissionSets.ViewOnly.HasFlag(Permission.ScreenView));
        Assert.False(PermissionSets.ViewOnly.HasFlag(Permission.MouseControl));

        Assert.True(PermissionSets.RemoteControl.HasFlag(Permission.ScreenView));
        Assert.True(PermissionSets.RemoteControl.HasFlag(Permission.MouseControl));
        Assert.True(PermissionSets.RemoteControl.HasFlag(Permission.KeyboardControl));
        Assert.False(PermissionSets.RemoteControl.HasFlag(Permission.Terminal));

        Assert.True(PermissionSets.FullAccess.HasFlag(Permission.Terminal));
        Assert.True(PermissionSets.FullAccess.HasFlag(Permission.FileDelete));
    }

    // ─────────────────────────────────────────────────────────
    //  ALL MESSAGE TYPES SERIALIZATION TEST
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void AllMessageTypes_Are_Registered()
    {
        // Ensure every defined type constant has a corresponding entry in the type map
        var fields = typeof(MessageTypes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string) && f.Name != nameof(MessageTypes.ProtocolVersion))
            .Select(f => (string)f.GetValue(null)!)
            .ToList();

        foreach (var messageType in fields)
        {
            Assert.True(
                MessageSerializer.IsKnownType(messageType),
                $"Message type '{messageType}' is not registered in the serializer TypeMap");
        }
    }
}
