using System.Threading;
using Twm.Adapters.Windows;
using Twm.Adapters.Windows.Diagnostics;
using Twm.Application.Coordination;
using Twm.Application.OutboundPorts;
using Twm.Domain.Tree;

namespace Twm.App;

internal static class DiagnosticModes
{
    /// <summary>
    /// Prints the display topology and, for every top-level window, the
    /// manage/ignore decision plus the criteria that drove it. Read-only.
    /// Joins the filter snapshot (<see cref="IWindowSystem.EnumerateWindows" />)
    /// with the diagnostic projection
    /// (<see cref="WindowsWindowSystem.DescribeDiagnostics" />) so the
    /// filter hot path doesn't pay the <c>OpenProcess</c> + exe-lookup cost.
    /// </summary>
    public static int Dump(
        IMonitorSystem monitors,
        WindowsWindowSystem windows,
        WindowFilter filter
    )
    {
        Console.WriteLine("== Monitors ==");
        foreach (MonitorInfo monitor in monitors.EnumerateMonitors())
        {
            string primary = monitor.IsPrimary ? "*" : " ";
            Console.WriteLine($"  {primary} bounds={monitor.Bounds} work={monitor.WorkArea}");
        }

        Console.WriteLine("\n== Windows ==");
        List<NativeWindowInfo> all = [.. windows.EnumerateWindows()];
        Dictionary<WindowId, NativeWindowInfo> byWindow = all.ToDictionary(w => w.Id);
        Dictionary<WindowId, WindowDiagnostic> diagnostics = windows
            .DescribeDiagnostics()
            .ToDictionary(d => d.Id);
        foreach (NativeWindowInfo window in all)
        {
            string decision = filter.IsManageable(window) ? "MANAGE" : "ignore";
            string[] candidates =
            [
                !window.HasCaption ? "nocaption" : "",
                !window.HasWindowEdge ? "nowindowedge" : "",
                window.IsDlgModalFrame ? "dlgmodalframe" : "",
                window.IsLayered ? "layered" : "",
                window.IsToolWindow ? "tool" : "",
                window.IsChild ? "child" : "",
                window.IsNoActivate ? "noactivate" : "",
                window.IsMenuPopup ? "menu" : "",
                window.IsCloaked ? "cloaked" : "",
                window.IsMinimized ? "min" : "",
                window.IsElevated ? "elevated" : "",
            ];
            string flags = string.Join(',', candidates.Where(f => f.Length > 0));
            string flagSuffix = flags.Length > 0 ? $"  {{{flags}}}" : "";
            string exeSuffix =
                diagnostics.TryGetValue(window.Id, out WindowDiagnostic? d)
                && d.ProcessName is not null
                    ? $"  pid={d.ProcessId} exe={d.ProcessName}"
                    : "";
            string ownerText = "";
            if (window.Owner is WindowId owner)
            {
                string ownerTitle = byWindow.TryGetValue(owner, out NativeWindowInfo? ownerInfo)
                    ? ownerInfo.Title
                    : "";
                string ownerExe =
                    diagnostics.TryGetValue(owner, out WindowDiagnostic? od)
                    && od.ProcessName is not null
                        ? $" exe={od.ProcessName}"
                        : "";
                string ownerClass = d?.OwnerClass is not null ? $" class={d.OwnerClass}" : "";
                ownerText = $"0x{owner.Value:X} (\"{ownerTitle}\"{ownerExe}{ownerClass})";
            }
            Console.WriteLine(
                $"  [{decision}] {window.ClassName, -28} \"{window.Title}\" 0x{window.Id.Value:X} owner={ownerText}{exeSuffix}{flagSuffix}"
            );
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
