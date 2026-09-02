using Twm.Domain.Geometry;
using Twm.Domain.Tree;

namespace Twm.Application.OutboundPorts;

/// <summary>
/// The OS window operations Twm needs: observe top-level windows and drive
/// their position, focus, visibility, and closure. Implemented by the Win32
/// layer and faked in tests, so the reconciler is verifiable on Linux. All
/// windows are addressed by opaque <see cref="WindowId" />.
/// </summary>
public interface IWindowSystem
{
    /// <summary>
    /// Enumerates current top-level windows with the metadata the filter needs.
    /// </summary>
    IReadOnlyList<NativeWindowInfo> EnumerateWindows();

    /// <summary>
    /// Moves and resizes a window to <paramref name="bounds" /> without
    /// changing z-order or actionvation.
    /// </summary>
    void SetWindowRect(WindowId window, Rect bounds);

    /// <summary>Brings a window to the foreground and activates it.</summary>
    void SetForeground(WindowId window);

    /// <summary>
    /// Shows a previously hidden window. Used by workspace switching.
    /// </summary>
    void Show(WindowId window);

    /// <summary>
    /// Hides a window without destroying it. Used by workspace switching.
    /// </summary>
    void Hide(WindowId window);

    /// <summary>
    /// Requets the window close (posts <c>WM_CLOSE</c>). The window's actual
    /// removal from the tree arrives asynchronously via the destroy WinEvent,
    /// not here.
    void Close(WindowId window);
}
