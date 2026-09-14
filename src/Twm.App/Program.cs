using Twm.Adapters.Config;
using Twm.Adapters.Windows;
using Twm.App;
using Twm.Application.Config;
using Twm.Application.Coordination;

CliArgs cliArgs = CliArgs.Parse(args);

if (!OperatingSystem.IsWindows())
{
    Console.WriteLine("Twm runs on Windows only.");
    return 1;
}

if (cliArgs.Dump || cliArgs.UseConsole)
{
    WindowsStartup.AttachParentConsole();
}

if (cliArgs.UseConsole)
{
    WindowsStartup.AllocateConsole();
}

if (cliArgs.Dump)
{
    WindowsStartup.EnablePerMonitorDpiAwareness();
    var monitors = new WindowsMonitorSystem();
    var windows = new WindowsWindowSystem();
    ResolvedConfig config = new YamlConfigSource(cliArgs.ConfigPath).Load(
        monitors.EnumerateMonitors().Count
    );
    foreach (string configError in config.Errors)
    {
        Console.WriteLine($"config: {configError}");
    }

    return DiagnosticModes.Dump(monitors, windows, new WindowFilter(config.WindowRules));
}

using var host = AppHost.Build(cliArgs);
return host.Run();
