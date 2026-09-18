using System.Runtime.InteropServices;
using LPUPlus.Protocol.Messages;

namespace LPUPlus.Agent.Input;

public class WindowsInputInjector : IInputInjector
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    private const uint INPUT_MOUSE = 0;
    private const uint INPUT_KEYBOARD = 1;

    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    private const uint MOUSEEVENTF_WHEEL = 0x0800;
    private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

    private void SendMouseInput(uint flags, int dx = 0, int dy = 0, uint data = 0)
    {
        INPUT[] inputs = new INPUT[1];
        inputs[0].type = INPUT_MOUSE;
        inputs[0].u.mi = new MOUSEINPUT
        {
            dx = dx,
            dy = dy,
            mouseData = data,
            dwFlags = flags,
            time = 0,
            dwExtraInfo = IntPtr.Zero
        };
        SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    public void InjectMouseMove(double x, double y)
    {
        // x and y are normalized [0.0, 1.0] from the frontend
        // SendInput absolute coordinates map the primary monitor to 0..65535
        int absX = (int)(Math.Clamp(x, 0, 1) * 65535);
        int absY = (int)(Math.Clamp(y, 0, 1) * 65535);

        SendMouseInput(MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE, absX, absY);
    }

    public void InjectMouseDown(MouseButton button, double x, double y)
    {
        InjectMouseMove(x, y);
        uint flags = button switch
        {
            MouseButton.Left => MOUSEEVENTF_LEFTDOWN,
            MouseButton.Right => MOUSEEVENTF_RIGHTDOWN,
            MouseButton.Middle => MOUSEEVENTF_MIDDLEDOWN,
            _ => MOUSEEVENTF_LEFTDOWN
        };
        SendMouseInput(flags);
    }

    public void InjectMouseUp(MouseButton button, double x, double y)
    {
        InjectMouseMove(x, y);
        uint flags = button switch
        {
            MouseButton.Left => MOUSEEVENTF_LEFTUP,
            MouseButton.Right => MOUSEEVENTF_RIGHTUP,
            MouseButton.Middle => MOUSEEVENTF_MIDDLEUP,
            _ => MOUSEEVENTF_LEFTUP
        };
        SendMouseInput(flags);
    }

    public void InjectMouseWheel(int deltaX, int deltaY)
    {
        if (deltaY != 0)
        {
            // Windows typically uses 120 as standard wheel tick
            // deltaY is usually smaller from JS, so multiply
            int tick = (int)(deltaY * -1.2);
            SendMouseInput(MOUSEEVENTF_WHEEL, 0, 0, (uint)tick);
        }
    }

    public void InjectKeyDown(string key)
    {
        ushort vkCode = MapKeyToVirtualKey(key);
        if (vkCode != 0) SendKeyboardInput(vkCode, false);
    }

    public void InjectKeyUp(string key)
    {
        ushort vkCode = MapKeyToVirtualKey(key);
        if (vkCode != 0) SendKeyboardInput(vkCode, true);
    }

    private void SendKeyboardInput(ushort wVk, bool keyUp)
    {
        INPUT[] inputs = new INPUT[1];
        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].u.ki = new KEYBDINPUT
        {
            wVk = wVk,
            dwFlags = keyUp ? 0x0002u : 0u, // KEYEVENTF_KEYUP = 0x0002
            time = 0,
            dwExtraInfo = IntPtr.Zero
        };
        SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    private ushort MapKeyToVirtualKey(string key)
    {
        if (key.Length == 1)
        {
            char c = char.ToUpperInvariant(key[0]);
            if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == ' ') return (ushort)c;
        }

        return key.ToLowerInvariant() switch
        {
            "enter" => 0x0D,
            "backspace" => 0x08,
            "tab" => 0x09,
            "escape" => 0x1B,
            "shift" => 0x10,
            "control" => 0x11,
            "alt" => 0x12,
            "arrowleft" => 0x25,
            "arrowup" => 0x26,
            "arrowright" => 0x27,
            "arrowdown" => 0x28,
            "delete" => 0x2E,
            _ => 0
        };
    }
}
