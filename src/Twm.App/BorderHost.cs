using Twm.Adapters.Windows;
using Twm.Application.Config;
using Twm.Application.Coordination;
using Twm.Domain.Tree;

namespace Twm.App;

/// <summary>
/// Owns the focus <see cref="BorderWindow" /> when
/// <see cref="BorderOptions.Enabled" /> is true. Repaints to track the
/// currently focused window on each layout change. Constructed only when
/// the option enables it; otherwise <c>Program.cs</c> passes <c>null</c>
/// in place of this host.
/// </summary>
internal sealed class BorderHost : IDisposable
{
    private readonly BorderWindow _borderWindow;
    private readonly WmSession _session;
    private bool _disposed;

    public BorderHost(WmSession session, BorderOptions options)
    {
        _session = session;
        _borderWindow = new BorderWindow(options.Color, options.Width);

        _session.Subscribe<LayoutChangedEvent>(_ => Refresh());

        Refresh();
    }

    /// <summary>Moves the border over the focused window, or hides it if none.</summary>
    public void Refresh()
    {
        if (_session.Root.FocusedWindow is TilingWindow focused)
        {
            _borderWindow.MoveTo(focused.Bounds);
        }
        else
        {
            _borderWindow.Hide();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _borderWindow.Dispose();
        BorderWindow.UnregisterSharedClass();
    }
}
