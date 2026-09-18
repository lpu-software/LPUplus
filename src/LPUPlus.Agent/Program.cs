using System.CommandLine;
using LPUPlus.Agent.CLI;

// ─────────────────────────────────────────────────────────────
//  LPU+ Agent CLI
//
//  Commands:
//    lpuplus start    — Launch agent, display pairing code, wait for connections
//    lpuplus stop     — Terminate active session
//    lpuplus status   — Show current agent/session state
//    lpuplus pair     — Display or regenerate pairing code
//    lpuplus version  — Show version info
// ─────────────────────────────────────────────────────────────

var rootCommand = new RootCommand("LPU+ Remote Support Platform — Host Agent");

// ── start ──
var startCommand = new Command("start", "Launch the LPU+ agent and wait for connections");
startCommand.SetAction(async (parseResult, ct) =>
{
    using var agent = new AgentService();

    // Handle Ctrl+C gracefully
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        agent.Stop("Host pressed Ctrl+C");
    };

    await agent.StartAsync(ct);
});
rootCommand.Subcommands.Add(startCommand);

// ── stop ──
var stopCommand = new Command("stop", "Terminate the active remote session");
stopCommand.SetAction((parseResult, ct) =>
{
    ConsoleUI.ShowBanner();
    ConsoleUI.ShowInfo("Sending stop signal to running agent...");
    ConsoleUI.ShowSessionEnded("Stop command received");
    return Task.CompletedTask;
});
rootCommand.Subcommands.Add(stopCommand);

// ── status ──
var statusCommand = new Command("status", "Show current agent and session status");
statusCommand.SetAction((parseResult, ct) =>
{
    ConsoleUI.ShowBanner();
    ConsoleUI.ShowStatus("Disconnected", null, null, null);
    return Task.CompletedTask;
});
rootCommand.Subcommands.Add(statusCommand);

// ── pair ──
var pairCommand = new Command("pair", "Display or regenerate the pairing code");
var regenerateOption = new Option<bool>("--regenerate") { Description = "Generate a new pairing code" };
pairCommand.Options.Add(regenerateOption);
pairCommand.SetAction((parseResult, ct) =>
{
    var regenerate = parseResult.GetValue(regenerateOption);
    ConsoleUI.ShowBanner();

    var code = LPUPlus.Agent.Security.PairingCodeGenerator.Generate();
    if (regenerate)
        ConsoleUI.ShowInfo("New pairing code generated");

    Console.WriteLine();
    var prev = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"  Pairing Code: {code}");
    Console.ForegroundColor = prev;
    Console.WriteLine();
    return Task.CompletedTask;
});
rootCommand.Subcommands.Add(pairCommand);

// ── version ──
var versionCommand = new Command("version", "Show LPU+ version information");
versionCommand.SetAction((parseResult, ct) =>
{
    ConsoleUI.ShowVersion();
    return Task.CompletedTask;
});
rootCommand.Subcommands.Add(versionCommand);

// ── Run ──
var config = new CommandLineConfiguration(rootCommand);
return await config.InvokeAsync(args);
