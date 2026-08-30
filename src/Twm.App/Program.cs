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

return 0;
