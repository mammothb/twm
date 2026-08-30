using System.Diagnostics;
using System.Threading;
using Twm.Platform;
using Twm.Platform.Config;
using Twm.Platform.Diagnostics;
using Twm.Platform.Windows;

bool dump = args.Contains("--dump");
bool useConsole = args.Contains("--console");

if (!OperatingSystem.IsWindows())
{
    Console.WriteLine("Twm runs on Windows only.");
    return 1;
}

if (dump)
{
    WindowsStartup.AttachParentConsole();
}
else if (useConsole)
{
    WindowsStartup.AttachParentConsole();
    WindowsStartup.AllocateConsole();
}

StreamWriter? logWriter = null;
if (args.Contains("--log"))
{
    string logPath = Path.Combine(Path.GetDirectoryName(ConfigPaths.Default())!, "twm.log");
    Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
    logWriter = new StreamWriter(logPath, append: false) { AutoFlush = true };
    StreamWriter sink = logWriter;
    Log.Init(line =>
    {
        sink.WriteLine(line);
        Console.WriteLine(line);
    });
    Console.WriteLine($"Logging to {logPath}");
}

// Physical-pixel coordinates everywhere. Must preceed any monitor/window
// enumeration
WindowsStartup.EnablePerMonitorDpiAwareness();

var monitors = new WindowsMonitorSystem();
var windows = new WindowsWindowSystem();

// Load YAML config (%USERPROFILE/.twm/config.yaml, or --config <path>). Absent
// or malformed -> built-in defaults, with any errors printed. Never fatal: a
// bad config still yields a working WM.
int configIndex = Array.IndexOf(args, "--config");
string configPath =
    0 <= configIndex && configIndex < args.Length - 1
        ? args[configIndex + 1]
        : ConfigPaths.Default();

string? configYaml = null;
if (File.Exists(configPath))
{
    try
    {
        configYaml = File.ReadAllText(configPath);
    }
    catch (Exception readError) when (readError is IOException or UnauthorizedAccessException)
    {
        Console.WriteLine($"config: could not read {configPath}: {readError.Message}");
    }
}

// Load config here

WindowFilter filter = new();

if (dump)
{
    Console.WriteLine("== Monitors ==");
    foreach (MonitorInfo monitor in monitors.EnumerateMonitors())
    {
        string primary = monitor.IsPrimary ? "*" : " ";
        Console.WriteLine($"  {primary} bounds={monitor.Bounds} work={monitor.WorkArea}");
    }

    Console.WriteLine();
    Console.WriteLine("== Windows ==");
    foreach (NativeWindowInfo window in windows.EnumerateWindows())
    {
        string decision = filter.IsManageable(window) ? "MANAGE" : "ignore";
        string[] candidates =
        [
            !window.HasCaption ? "nocaption" : "",
            !window.HasWindowEdge ? "nowindowedge" : "",
            window.IsChild ? "child" : "",
            window.IsCloaked ? "cloaked" : "",
            window.IsElevated ? "elevated" : "",
            window.IsLayered ? "layered" : "",
            window.IsMenuPopup ? "menu" : "",
            window.IsMinimized ? "min" : "",
            window.IsNoActivate ? "noactivate" : "",
            window.IsToolWindow ? "tool" : "",
        ];
        string flags = string.Join(",", candidates.Where(f => f.Length > 0));
        string suffix = flags.Length > 0 ? $"  {{{flags}}}" : "";
        Console.WriteLine($"  [{decision}] {window.ClassName, -28} \"{window.Title}\"{suffix}");
    }
    return 0;
}

// Single-instance guard so two Twm processes never fight over the same windows
using var mutex = new Mutex(initiallyOwned: true, "Twm.SingleInstance", out bool isOnlyInstance);
if (!isOnlyInstance)
{
    Console.WriteLine("Twm is already running.");
    return 1;
}

// Let keyboard driven focus changes actually bring windows to the foreground
WindowsStartup.DisableForegroundLockTimeout();

return 0;
