using Twm.Application.OutboundPorts;
using Twm.Domain.Geometry;
using Twm.Domain.Tree;

namespace Twm.Adapters.Windows;

/// <summary>
/// The Win32 <see cref="IWindowSystem" />: enumerates top-level windows with
/// the metadata the filter needs, and drives their
/// position/focus/visibility/closure.
/// </summary>
public sealed class WindowsWindowSystem : IWindowSystem
{
    public IReadOnlyList<NativeWindowInfo> EnumerateWindows()
    {
        List<NativeWindowInfo> result = [];
        foreach (nint window in NativeMethods.TopLevelWindows())
        {
            result.Add(Describe(new WindowId(window)));
        }
        return result;
    }

    /// <summary>
    /// Reads a fresh metadata snapshot for one window (used by the WinEvent
    /// hook).
    /// </summary>
    public NativeWindowInfo Describe(WindowId window)
    {
        nint handle = window.Value;
        return new NativeWindowInfo(
            Id: window,
            Title: NativeMethods.GetWindowText(handle),
            ClassName: NativeMethods.GetClassName(handle),
            Bounds: NativeMethods.GetBounds(handle),
            IsVisible: NativeMethods.IsVisible(handle),
            IsCloaked: NativeMethods.IsCloaked(handle),
            IsToolWindow: NativeMethods.IsToolWindow(handle),
            IsMinimized: NativeMethods.IsMinimized(handle),
            IsChild: NativeMethods.IsChildWindow(handle),
            IsElevated: NativeMethods.IsElevated(handle),
            IsNoActivate: NativeMethods.IsNoActivate(handle),
            IsMenuPopup: NativeMethods.IsMenuPopup(handle),
            IsLayered: NativeMethods.IsLayered(handle),
            HasCaption: NativeMethods.HasCaption(handle),
            HasWindowEdge: NativeMethods.HasWindowEdge(handle),
            Owner: NativeMethods.GetOwner(handle) is nint owner ? new WindowId(owner) : null
        );
    }

    /// <summary>The current title text of a window.</summary>
    public string GetTitle(WindowId window) => NativeMethods.GetWindowText(window.Value);

    public void Close(WindowId window) => NativeMethods.Close(window.Value);

    // "Hide" for workspace switching = cloak, not ShowWindow(SW_HIDE): a
    // cloaked window stays in the taskbar so its icon can be clicked to
    // navigate back to it
    public void Hide(WindowId window) => ImmersiveShell.Cloak(window.Value);

    public void SetForeground(WindowId window) => NativeMethods.Foreground(window.Value);

    public void SetWindowRect(WindowId window, Rect bounds) =>
        NativeMethods.SetBounds(window.Value, bounds);

    // "Show" for workspace switching = uncloak (keeps the window in the
    // taskbar)
    public void Show(WindowId window) => ImmersiveShell.Uncloak(window.Value);
}
