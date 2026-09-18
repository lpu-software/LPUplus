using LPUPlus.Protocol.Messages;

namespace LPUPlus.Agent.Input;

/// <summary>
/// Platform-agnostic interface for injecting simulated user input.
/// </summary>
public interface IInputInjector
{
    void InjectMouseMove(int x, int y);
    void InjectMouseDown(MouseButton button, int x, int y);
    void InjectMouseUp(MouseButton button, int x, int y);
    void InjectMouseWheel(int deltaX, int deltaY);
    void InjectKeyDown(string key);
    void InjectKeyUp(string key);
}
