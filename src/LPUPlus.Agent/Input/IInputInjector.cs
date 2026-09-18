using LPUPlus.Protocol.Messages;

namespace LPUPlus.Agent.Input;

/// <summary>
/// Platform-agnostic interface for injecting simulated user input.
/// </summary>
public interface IInputInjector
{
    void InjectMouseMove(double x, double y);
    void InjectMouseDown(MouseButton button, double x, double y);
    void InjectMouseUp(MouseButton button, double x, double y);
    void InjectMouseWheel(int deltaX, int deltaY);
    void InjectKeyDown(string key);
    void InjectKeyUp(string key);
}
