namespace LPUPlus.Protocol.Messages;

public enum MouseButton
{
    None,
    Left,
    Right,
    Middle
}

public class MouseMoveMessage : IProtocolMessage
{
    public string Type => MessageTypes.MouseMove;
    public int X { get; set; }
    public int Y { get; set; }
}

public class MouseDownMessage : IProtocolMessage
{
    public string Type => MessageTypes.MouseDown;
    public MouseButton Button { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}

public class MouseUpMessage : IProtocolMessage
{
    public string Type => MessageTypes.MouseUp;
    public MouseButton Button { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}

public class MouseWheelMessage : IProtocolMessage
{
    public string Type => MessageTypes.MouseWheel;
    public int DeltaX { get; set; }
    public int DeltaY { get; set; }
}

public class KeyDownMessage : IProtocolMessage
{
    public string Type => MessageTypes.KeyDown;
    public string Key { get; set; } = string.Empty;
}

public class KeyUpMessage : IProtocolMessage
{
    public string Type => MessageTypes.KeyUp;
    public string Key { get; set; } = string.Empty;
}
