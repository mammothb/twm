using Twm.Core.Tree;

namespace Twm.Platform;

/// <summary>
/// Pushes the container tree to the OS. Every window the tree says should be
/// visible (<see cref="TreeQueries.IsEffectivelyVisible" />, active workspace
/// and focused through every tabbed/stacked ancestor) is positioned and show;
/// every other managed window is hidden (cloaked, on Windows). The focused
/// window is then brought to the foreground.
/// </summary>
public sealed class Reconciler(IWindowSystem windows)
{
    private readonly IWindowSystem _windows =
        windows ?? throw new ArgumentNullException(nameof(windows));

    /// <summary>
    /// Pushes the tree's window bounds, visibility, and focus out to the
    /// window system.
    /// </system>
    public void Apply(RootContainer root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var windows = root.Descendants.OfType<TilingWindow>().ToList();

        // Pass 1: position + show every window that should be visible, and
        // foreground the focused one. This happens BEFORE any cloak. Cloaking
        // the window that is currently foreground makes Windows auto-activate
        // some other (possibly cloaked) window, firing a spurious foreground
        // event that SyncFocus would react to, in a tabbed/stacked container
        // that oscillates endlessly. Foregrounding the target first guarantees
        // the windows we cloak next are never the foreground one.
        foreach (TilingWindow window in windows)
        {
            if (!window.IsEffectivelyVisible())
            {
                continue;
            }

            try
            {
                _windows.SetWindowRect(window.WindowId, window.Bounds);
                _windows.Show(window.WindowId);
            }
            catch (Exception)
            {
                // Skip misbehaving window, must not abort tiling the rest
            }
        }

        if (root.FocusedWindow() is TilingWindow focused)
        {
            try
            {
                _windows.SetForeground(focused.WindowId);
            }
            catch (Exception)
            {
                // Ignore foregrounding a window that just vanished
            }
        }

        // Pass 2: cloak everything that should not be visible (now that the
        // focused window holds the foreground).
        foreach (TilingWindow window in windows)
        {
            if (window.IsEffectivelyVisible())
            {
                continue;
            }

            try
            {
                _windows.Hide(window.WindowId);
            }
            catch (Exception)
            {
                // Skip a window that refused to hide
            }
        }
    }
}
