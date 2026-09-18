namespace LPUPlus.Agent.CLI;

/// <summary>
/// Branded console output formatting for the LPU+ CLI.
/// Provides consistent, professional terminal output.
/// </summary>
public static class ConsoleUI
{
    // ── Colors ──
    private static readonly ConsoleColor Accent = ConsoleColor.Cyan;
    private static readonly ConsoleColor Success = ConsoleColor.Green;
    private static readonly ConsoleColor Warning = ConsoleColor.Yellow;
    private static readonly ConsoleColor Danger = ConsoleColor.Red;
    private static readonly ConsoleColor Muted = ConsoleColor.DarkGray;
    private static readonly ConsoleColor Info = ConsoleColor.White;

    /// <summary>
    /// Display the LPU+ branded banner.
    /// </summary>
    public static void ShowBanner()
    {
        WriteColored(Accent, """
        ╔══════════════════════════════════════╗
        ║              LPU+                    ║
        ║       Remote Support Platform        ║
        ╚══════════════════════════════════════╝
        """);
        Console.WriteLine();
    }

    /// <summary>
    /// Show the startup status with device info and pairing code.
    /// </summary>
    public static void ShowStartupStatus(string deviceName, string platform, string display, string pairingCode)
    {
        WriteLabeled("Status", "Ready", Success);
        WriteLabeled("Device", deviceName, Info);
        WriteLabeled("Platform", platform, Info);
        WriteLabeled("Display", display, Info);
        WriteLabeled("Session", "Not Connected", Muted);
        Console.WriteLine();

        WriteColored(Accent, "  Pairing Code:");
        Console.WriteLine();
        WriteColored(ConsoleColor.White, $"      {pairingCode}");
        Console.WriteLine();
        Console.WriteLine();
        WriteColored(Muted, "  Waiting for authorized controller...");
        Console.WriteLine();
    }

    /// <summary>
    /// Display the session approval prompt with permission checkboxes.
    /// Returns true if the host approves.
    /// </summary>
    public static bool ShowApprovalPrompt(string controllerInfo)
    {
        Console.WriteLine();
        WriteColored(Warning, "  ── Remote Support Request ──");
        Console.WriteLine();
        WriteColored(Info, $"  Controller: {controllerInfo}");
        Console.WriteLine();
        Console.WriteLine();

        WriteColored(Info, "  Permissions:");
        WritePermission("Screen viewing", true);
        WritePermission("Mouse control", false);
        WritePermission("Keyboard control", false);
        WritePermission("File access", false);
        WritePermission("Terminal access", false);

        Console.WriteLine();
        WriteColored(Accent, "  Approve session? [Y/N]: ");
        
        var key = Console.ReadKey(intercept: true);
        Console.WriteLine(key.KeyChar);
        
        return key.Key is ConsoleKey.Y;
    }

    /// <summary>
    /// Show the active session indicator.
    /// </summary>
    public static void ShowActiveSession(string controllerName, bool screen, bool mouse, bool keyboard)
    {
        Console.WriteLine();
        WriteColored(Success, """
        ╔══════════════════════════════════════╗
        ║     LPU+ REMOTE SESSION ACTIVE       ║
        """);
        WriteColored(Success, $"        ║  Controller: {controllerName,-24}║");
        Console.WriteLine();
        WriteColored(Success, $"        ║  Screen: {(screen ? "ON " : "OFF")}                          ║");
        Console.WriteLine();
        WriteColored(Success, $"        ║  Mouse: {(mouse ? "ON " : "OFF")}                           ║");
        Console.WriteLine();
        WriteColored(Success, $"        ║  Keyboard: {(keyboard ? "ON " : "OFF")}                        ║");
        Console.WriteLine();
        WriteColored(Success, """
        ╚══════════════════════════════════════╝
        """);
        Console.WriteLine();
        WriteColored(Muted, "  Press Ctrl+C or run 'lpuplus stop' to end session");
        Console.WriteLine();
    }

    /// <summary>
    /// Show session ended message.
    /// </summary>
    public static void ShowSessionEnded(string reason)
    {
        Console.WriteLine();
        WriteColored(Warning, "  ── Session Ended ──");
        Console.WriteLine();
        WriteColored(Info, $"  Reason: {reason}");
        Console.WriteLine();
    }

    /// <summary>
    /// Show status information.
    /// </summary>
    public static void ShowStatus(string state, string? sessionId, string? controller, TimeSpan? duration)
    {
        ShowBanner();
        WriteLabeled("State", state, state == "Active" ? Success : Muted);

        if (sessionId is not null)
            WriteLabeled("Session", sessionId, Info);
        if (controller is not null)
            WriteLabeled("Controller", controller, Info);
        if (duration.HasValue)
            WriteLabeled("Duration", FormatDuration(duration.Value), Info);
    }

    /// <summary>
    /// Show version information.
    /// </summary>
    public static void ShowVersion()
    {
        ShowBanner();
        WriteLabeled("Version", "1.0.0-alpha", Info);
        WriteLabeled("Protocol", $"v{LPUPlus.Protocol.Messages.MessageTypes.ProtocolVersion}", Info);
        WriteLabeled("Runtime", System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription, Muted);
        WriteLabeled("OS", System.Runtime.InteropServices.RuntimeInformation.OSDescription, Muted);
    }

    /// <summary>
    /// Show an error message.
    /// </summary>
    public static void ShowError(string message)
    {
        WriteColored(Danger, $"  ✗ Error: {message}");
        Console.WriteLine();
    }

    /// <summary>
    /// Show a success message.
    /// </summary>
    public static void ShowSuccess(string message)
    {
        WriteColored(Success, $"  ✓ {message}");
        Console.WriteLine();
    }

    /// <summary>
    /// Show an info message.
    /// </summary>
    public static void ShowInfo(string message)
    {
        WriteColored(Muted, $"  ℹ {message}");
        Console.WriteLine();
    }

    // ── Helpers ──

    private static void WriteLabeled(string label, string value, ConsoleColor valueColor)
    {
        WriteColored(Muted, $"  {label}: ");
        WriteColored(valueColor, value);
        Console.WriteLine();
    }

    private static void WritePermission(string name, bool enabled)
    {
        var check = enabled ? "✓" : " ";
        var color = enabled ? Success : Muted;
        WriteColored(color, $"    [{check}] {name}");
        Console.WriteLine();
    }

    private static void WriteColored(ConsoleColor color, string text)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = prev;
    }

    private static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalHours >= 1) return $"{ts.Hours}h {ts.Minutes}m {ts.Seconds}s";
        if (ts.TotalMinutes >= 1) return $"{ts.Minutes}m {ts.Seconds}s";
        return $"{ts.Seconds}s";
    }
}
