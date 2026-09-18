using System.Runtime.InteropServices;
using LPUPlus.Protocol.Messages;

namespace LPUPlus.Agent.Input;

public class MacInputInjector : IInputInjector
{
    private const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

    public enum CGMouseButton
    {
        Left = 0,
        Right = 1,
        Center = 2
    }

    public enum CGEventType
    {
        LeftMouseDown = 1,
        LeftMouseUp = 2,
        RightMouseDown = 3,
        RightMouseUp = 4,
        MouseMoved = 5,
        LeftMouseDragged = 6,
        RightMouseDragged = 7,
        KeyDown = 10,
        KeyUp = 11,
        ScrollWheel = 22
    }

    public enum CGEventField
    {
        ScrollWheelEventDeltaAxis1 = 11,
        ScrollWheelEventDeltaAxis2 = 12
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CGPoint
    {
        public double X;
        public double Y;
        public CGPoint(double x, double y) { X = x; Y = y; }
    }

    [DllImport(CoreGraphics, EntryPoint = "CGEventCreateMouseEvent")]
    private static extern IntPtr CGEventCreateMouseEvent(IntPtr source, CGEventType mouseType, CGPoint mouseCursorPosition, CGMouseButton mouseButton);

    [DllImport(CoreGraphics, EntryPoint = "CGEventCreateKeyboardEvent")]
    private static extern IntPtr CGEventCreateKeyboardEvent(IntPtr source, ushort virtualKey, bool keyDown);

    [DllImport(CoreGraphics, EntryPoint = "CGEventCreateScrollWheelEvent")]
    private static extern IntPtr CGEventCreateScrollWheelEvent(IntPtr source, uint units, uint wheelCount, int wheel1);

    [DllImport(CoreGraphics, EntryPoint = "CGEventSetIntegerValueField")]
    private static extern void CGEventSetIntegerValueField(IntPtr @event, CGEventField field, long value);

    [DllImport(CoreGraphics, EntryPoint = "CGEventPost")]
    private static extern void CGEventPost(uint tapLocation, IntPtr @event);

    [DllImport("CoreFoundation", EntryPoint = "CFRelease")]
    private static extern void CFRelease(IntPtr cf);

    [DllImport(CoreGraphics, EntryPoint = "CGMainDisplayID")]
    private static extern uint CGMainDisplayID();

    [DllImport(CoreGraphics, EntryPoint = "CGDisplayBounds")]
    private static extern CGRect CGDisplayBounds(uint displayId);

    [StructLayout(LayoutKind.Sequential)]
    public struct CGRect
    {
        public CGPoint Origin;
        public CGSize Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CGSize
    {
        public double Width;
        public double Height;
    }

    private const uint kCGHIDEventTap = 0;

    private (double X, double Y) GetAbsoluteCoordinates(double percentX, double percentY)
    {
        var bounds = CGDisplayBounds(CGMainDisplayID());
        return (bounds.Size.Width * percentX, bounds.Size.Height * percentY);
    }

    public void InjectMouseMove(double percentX, double percentY)
    {
        var (absX, absY) = GetAbsoluteCoordinates(percentX, percentY);
        var evt = CGEventCreateMouseEvent(IntPtr.Zero, CGEventType.MouseMoved, new CGPoint(absX, absY), CGMouseButton.Left);
        if (evt != IntPtr.Zero)
        {
            CGEventPost(kCGHIDEventTap, evt);
            CFRelease(evt);
        }
    }

    public void InjectMouseDown(MouseButton button, double percentX, double percentY)
    {
        var (absX, absY) = GetAbsoluteCoordinates(percentX, percentY);
        var (cgEvent, cgButton) = button switch
        {
            MouseButton.Right => (CGEventType.RightMouseDown, CGMouseButton.Right),
            MouseButton.Middle => (CGEventType.LeftMouseDown, CGMouseButton.Center),
            _ => (CGEventType.LeftMouseDown, CGMouseButton.Left)
        };

        var evt = CGEventCreateMouseEvent(IntPtr.Zero, cgEvent, new CGPoint(absX, absY), cgButton);
        if (evt != IntPtr.Zero)
        {
            CGEventPost(kCGHIDEventTap, evt);
            CFRelease(evt);
        }
    }

    public void InjectMouseUp(MouseButton button, double percentX, double percentY)
    {
        var (absX, absY) = GetAbsoluteCoordinates(percentX, percentY);
        var (cgEvent, cgButton) = button switch
        {
            MouseButton.Right => (CGEventType.RightMouseUp, CGMouseButton.Right),
            MouseButton.Middle => (CGEventType.LeftMouseUp, CGMouseButton.Center),
            _ => (CGEventType.LeftMouseUp, CGMouseButton.Left)
        };

        var evt = CGEventCreateMouseEvent(IntPtr.Zero, cgEvent, new CGPoint(absX, absY), cgButton);
        if (evt != IntPtr.Zero)
        {
            CGEventPost(kCGHIDEventTap, evt);
            CFRelease(evt);
        }
    }

    public void InjectMouseWheel(int deltaX, int deltaY)
    {
        // deltaY is usually axis 1 (vertical), deltaX is axis 2 (horizontal).
        var evt = CGEventCreateScrollWheelEvent(IntPtr.Zero, 0, 2, deltaY);
        if (evt != IntPtr.Zero)
        {
            CGEventSetIntegerValueField(evt, CGEventField.ScrollWheelEventDeltaAxis2, deltaX);
            CGEventPost(kCGHIDEventTap, evt);
            CFRelease(evt);
        }
    }

    public void InjectKeyDown(string key)
    {
        // Simple mapping, proper implementation requires full keycode map
        ushort keyCode = MapKey(key);
        var evt = CGEventCreateKeyboardEvent(IntPtr.Zero, keyCode, true);
        if (evt != IntPtr.Zero)
        {
            CGEventPost(kCGHIDEventTap, evt);
            CFRelease(evt);
        }
    }

    public void InjectKeyUp(string key)
    {
        ushort keyCode = MapKey(key);
        var evt = CGEventCreateKeyboardEvent(IntPtr.Zero, keyCode, false);
        if (evt != IntPtr.Zero)
        {
            CGEventPost(kCGHIDEventTap, evt);
            CFRelease(evt);
        }
    }

    private ushort MapKey(string key)
    {
        // This is a minimal map. A production system needs a complete Virtual Keycode mapping table.
        if (string.IsNullOrEmpty(key)) return 0;
        
        char c = char.ToLowerInvariant(key[0]);
        return c switch
        {
            'a' => 0x00, 's' => 0x01, 'd' => 0x02, 'f' => 0x03, 'h' => 0x04, 'g' => 0x05, 'z' => 0x06,
            'x' => 0x07, 'c' => 0x08, 'v' => 0x09, 'b' => 0x0B, 'q' => 0x0C, 'w' => 0x0D, 'e' => 0x0E,
            'r' => 0x0F, 'y' => 0x10, 't' => 0x11, '1' => 0x12, '2' => 0x13, '3' => 0x14, '4' => 0x15,
            '6' => 0x16, '5' => 0x17, '=' => 0x18, '9' => 0x19, '7' => 0x1A, '-' => 0x1B, '8' => 0x1C,
            '0' => 0x1D, ']' => 0x1E, 'o' => 0x1F, 'u' => 0x20, '[' => 0x21, 'i' => 0x22, 'p' => 0x23,
            'l' => 0x25, 'j' => 0x26, '\'' => 0x27, 'k' => 0x28, ';' => 0x29, '\\' => 0x2A, ',' => 0x2B,
            '/' => 0x2C, 'n' => 0x2D, 'm' => 0x2E, '.' => 0x2F,
            _ => 0
        };
    }
}
