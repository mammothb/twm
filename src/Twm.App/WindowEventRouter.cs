using Twm.Adapters.Windows;
using Twm.Application.Coordination;
using Twm.Application.Diagnostics;
using Twm.Application.OutboundPorts;
using Twm.Domain.Tree;

namespace Twm.App;

/// <summary>
/// Routes <c>WinEventHook</c> callbacks into <see cref="WmSession" />.
/// Adopts new windows, removes destroyed/hidden/minimized ones, and
/// forwards foreground/focus changes to the session. The single
/// <see cref="WinEventKind" /> switch lives here so the WM event loop
/// stays in <c>Program.cs</c> focused on composition.
/// </summary>
internal sealed class WindowEventRouter(WmSession session, IWindowSystem windowSystem) : IDisposable
{
    private readonly WmSession _session = session;
    private readonly IWindowSystem _windowSystem = windowSystem;
    private readonly WinEventHook _winEventHook = new();

    public void Install() => _winEventHook.Install(Handle);

    public void Dispose() => _winEventHook.Dispose();

    internal void Handle(WindowEventKind kind, WindowId id)
    {
        Log.Line($"winevent {kind} 0x{id.Value:X}");
        switch (kind)
        {
            case WindowEventKind.Appeared:
                if (!_session.IsManaged(id) && _session.TryAdopt(_windowSystem.Describe(id)))
                {
                    Console.WriteLine(
                        $"managed - {_windowSystem.GetTitle(id)} ({_session.ManagedWindowCount} tiled)"
                    );
                }

                break;
            case WindowEventKind.Destroyed:
                if (_session.Remove(id))
                {
                    Console.WriteLine($"unmanaged ({_session.ManagedWindowCount} tiled)");
                }

                break;
            case WindowEventKind.Hidden:
                if (_session.HandleHidden(id))
                {
                    Console.WriteLine($"unmanaged ({_session.ManagedWindowCount} tiled)");
                }

                break;
            case WindowEventKind.Minimized:
                if (_session.HandleMinimized(id))
                {
                    Console.WriteLine($"unmanaged ({_session.ManagedWindowCount} tiled)");
                }

                break;
            case WindowEventKind.Cloaked:
                _session.HandleCloaked(id);
                break;
            case WindowEventKind.Foreground:
                _session.SyncFocus(id);
                break;
        }
    }
}
