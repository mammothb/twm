using Twm.Application.OutboundPorts;
using Twm.Domain.Geometry;
using Twm.Domain.Tree;

namespace Twm.TestSupport.Fakes;

/// <summary>
/// An <see cref="IWindowSystem" /> that returns a fixed set of windows and
/// records every call the reconciler makes, so tests can assert what Twm would
/// push to the OS.
/// </summary>
public sealed class FakeWindowSystem(params NativeWindowInfo[] windows) : IWindowSystem
{
    private readonly IReadOnlyList<NativeWindowInfo> _windows = windows;
    public List<(WindowId Window, Rect Bounds)> Positioned { get; } = [];

    /// <summary>
    /// Ids for which <see cref="SetWindowRect" /> throws (reconcile
    /// failure-isolation tests).
    /// </summary>
    public HashSet<WindowId> ThrowOnRect { get; } = [];
    public List<WindowId> Foregrounded { get; } = [];
    public List<WindowId> Shown { get; } = [];
    public List<WindowId> Hidden { get; } = [];
    public List<WindowId> Closed { get; } = [];

    /// <summary>
    /// Every mutating call in order, e.g., "foreground:1", "hide:2", for
    /// ordering assertions.
    /// </summary>
    public List<string> Operations { get; } = [];

    public IReadOnlyList<NativeWindowInfo> EnumerateWindows() => _windows;

    public void SetWindowRect(WindowId window, Rect bounds)
    {
        if (ThrowOnRect.Contains(window))
        {
            throw new InvalidOperationException($"simulated SetWindowRect failure for {window}");
        }
        Positioned.Add((window, bounds));
        Operations.Add($"rect:{window.Value}");
    }

    public void SetForeground(WindowId window)
    {
        Foregrounded.Add(window);
        Operations.Add($"foreground:{window.Value}");
    }

    public void Show(WindowId window)
    {
        Shown.Add(window);
        Operations.Add($"show:{window.Value}");
    }

    public void Hide(WindowId window)
    {
        Hidden.Add(window);
        Operations.Add($"hide:{window.Value}");
    }

    public void Close(WindowId window) => Closed.Add(window);
}
