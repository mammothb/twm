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
        IMonitorSystem monitorSystem,
        WindowsWindowSystem windowSystem,
        WindowFilter filter
    )
    {
        Console.WriteLine("== Monitors ==");
        PrintMonitors(monitorSystem);

        Console.WriteLine("\n== Windows ==");
        IReadOnlyList<NativeWindowInfo> windows = windowSystem.EnumerateWindows();
        Dictionary<WindowId, NativeWindowInfo> idToWindow = windows.ToDictionary(w => w.Id);
        Dictionary<WindowId, WindowDiagnostic> idToDiagnostic = windowSystem
            .DescribeDiagnostics()
            .ToDictionary(d => d.Id);
        foreach (NativeWindowInfo window in windows)
        {
            Console.WriteLine(FormatWindowLine(window, filter, idToWindow, idToDiagnostic));
        }

        return 0;
    }

    private static void PrintMonitors(IMonitorSystem monitorSystem)
    {
        foreach (MonitorInfo monitor in monitorSystem.EnumerateMonitors())
        {
            char primaryIndicator = monitor.IsPrimary ? '*' : ' ';
            Console.WriteLine(
                $"  {primaryIndicator} bounds={monitor.Bounds} workarea={monitor.WorkArea}"
            );
        }
    }

    private static string FormatWindowLine(
        NativeWindowInfo window,
        WindowFilter filter,
        Dictionary<WindowId, NativeWindowInfo> idToWindow,
        Dictionary<WindowId, WindowDiagnostic> idToDiagnostic
    )
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
            idToDiagnostic.TryGetValue(window.Id, out WindowDiagnostic? d)
            && d.ProcessName is not null
                ? $"  pid={d.ProcessId} exe={d.ProcessName}"
                : "";
        string ownerText = "";
        if (window.Owner is WindowId owner)
        {
            string ownerTitle = idToWindow.TryGetValue(owner, out NativeWindowInfo? ownerWindow)
                ? ownerWindow.Title
                : "";
            string ownerExe =
                idToDiagnostic.TryGetValue(owner, out WindowDiagnostic? od)
                && od.ProcessName is not null
                    ? $" exe={od.ProcessName}"
                    : "";
            string ownerClass = d?.OwnerClass is not null ? $" class={d.OwnerClass}" : "";
            ownerText = $"0x{owner.Value:X} (\"{ownerTitle}\"{ownerExe}{ownerClass})";
        }

        return $"  [{decision}] {window.ClassName, -28} \"{window.Title}\" 0x{window.Id.Value:X} owner={ownerText}{exeSuffix}{flagSuffix}";
    }
}
