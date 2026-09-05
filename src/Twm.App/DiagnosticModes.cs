using System.Threading;
using Twm.Application.Coordination;
using Twm.Application.OutboundPorts;

namespace Twm.App;

internal static class DiagnosticModes
{
    /// <summary>
    /// Prints the display topology and, for every top-level window, the
    /// manage/ignore decision plus the criteria that drove it. Read-only.
    /// </summary>
    public static int Dump(IMonitorSystem monitors, IWindowSystem windows, WindowFilter filter)
    {
        Console.WriteLine("== Monitors ==");
        foreach (MonitorInfo monitor in monitors.EnumerateMonitors())
        {
            string primary = monitor.IsPrimary ? "*" : " ";
            Console.WriteLine($"  {primary} bounds={monitor.Bounds} work={monitor.WorkArea}");
        }

        Console.WriteLine("\n== Windows ==");
        foreach (NativeWindowInfo window in windows.EnumerateWindows())
        {
            string decision = filter.IsManageable(window) ? "MANAGE" : "ignore";
            string[] candidates =
            [
                !window.HasCaption ? "nocaption" : "",
                !window.HasWindowEdge ? "nowindowedge" : "",
                window.IsLayered ? "layered" : "",
                window.IsToolWindow ? "noactivate" : "",
                window.IsChild ? "child" : "",
                window.IsNoActivate ? "noactivate" : "",
                window.IsMenuPopup ? "menu" : "",
                window.IsCloaked ? "cloaked" : "",
                window.IsMinimized ? "min" : "",
                window.IsElevated ? "elevated" : "",
            ];
            string flags = string.Join(',', candidates.Where(f => f.Length > 0));
            string suffix = flags.Length > 0 ? $"  {{{flags}}}" : "";
            Console.WriteLine($"  [{decision}] {window.ClassName, -28} \"{window.Title}\"{suffix}");
        }

        return 0;
    }

    /// <summary>Isolated check of the cloak COM.</summary>
    public static int CloakTest(IWindowSystem windows, WindowFilter filter)
    {
        List<NativeWindowInfo> manageable =
        [
            .. windows.EnumerateWindows().Where(filter.IsManageable),
        ];
        if (manageable.Count == 0)
        {
            Console.WriteLine("No manageable windows to cloak-test.");
            return 0;
        }

        NativeWindowInfo target = manageable[0];
        Console.WriteLine(
            $"Cloaking \"{target.Title}\" for 3s (should vanish, stay in taskbar)..."
        );
        windows.Hide(target.Id);
        Thread.Sleep(3000);
        windows.Show(target.Id);
        Console.WriteLine("Uncloaked.");
        return 0;
    }
}
