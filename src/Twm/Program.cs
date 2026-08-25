using System.Runtime.InteropServices;
using Twm.Config;
using Twm.Core;
using Twm.Interop;

namespace Twm;

internal static class Program
{
    private static async Task<int> Main()
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("twm only runs on Windows.");
            return 1;
        }

        EnableDpiAwareness();

        TwmConfig config;
        try
        {
            config = ConfigLoader.Load();
        }
        catch (ConfigException ex)
        {
            Console.Error.WriteLine($"config error: {ex.Message}");
            return 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"failed to load config: {ex.Message}");
            return 2;
        }

        Console.WriteLine(
            $"twm — tiling window manager | mod={config.ModKey} | "
                + $"{config.Bindings.Count} bindings | TWM_TRACE=1 for verbose logs"
        );

        var manager = new WindowManager(config);
        using var pump = NativeMessagePump.Start(manager.EnqueueWinEvent, manager.MatchHotkey);

        manager.ScanExisting();
        return await manager.RunAsync();
    }

    [DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(nint value);

    [DllImport("shcore.dll")]
    private static extern int SetProcessDpiAwareness(int value);

    private static void EnableDpiAwareness()
    {
        const nint perMonitorV2 = -4;
        if (!SetProcessDpiAwarenessContext(perMonitorV2))
        {
            _ = SetProcessDpiAwareness(2); // best effort: system-DPI-aware
        }
    }
}
