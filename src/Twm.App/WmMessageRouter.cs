using Twm.Adapters.Windows;
using Twm.Application.Coordination;
using Twm.Application.Diagnostics;
using Twm.Application.InboundPorts;
using Twm.Application.OutboundPorts;
using Twm.Domain.Tree;

namespace Twm.App;

/// <summary>
/// Routes <see cref="MessageLoop" /> callbacks into the WM. Handles IPC
/// drain (<see cref="WmThreadDispatcher" />), quit, the status-bar clock
/// tick, and keymap-driven actions. The single message switch lives here
/// so <c>Program.cs</c> stays a thin orchestrator.
/// </summary>
internal sealed class WmMessageRouter(
    WmSession session,
    IWindowSystem windowSystem,
    HotkeyManager hotkeyManager,
    IReadOnlyDictionary<KeyBinding, KeyEffect> keymap,
    WmThreadDispatcher ipcDispatcher,
    StatusBarHost? statusBar,
    Action? quit = null
)
{
    private readonly WmSession _session = session;
    private readonly IWindowSystem _windowSystem = windowSystem;
    private readonly HotkeyManager _hotkeyManager = hotkeyManager;
    private readonly IReadOnlyDictionary<KeyBinding, KeyEffect> _keymap = keymap;
    private readonly WmThreadDispatcher _ipcDispatcher = ipcDispatcher;
    private readonly StatusBarHost? _statusBar = statusBar;
    private readonly Action _quit = quit ?? MessageLoop.Quit;

    public void Handle(uint message, nint wParam, nint lParam)
    {
        if (message == MessageLoop.WmApp)
        {
            _ipcDispatcher.Drain();
            return;
        }

        if (message == MessageLoop.WmAppQuit)
        {
            _quit();
            return;
        }

        if (message == MessageLoop.WmTimer)
        {
            _statusBar?.OnClockTick();
            return;
        }

        if (
            !_hotkeyManager.TryResolve(message, wParam, out KeyBinding binding)
            || !_keymap.TryGetValue(binding, out KeyEffect? effect)
        )
        {
            return;
        }

        Log.Line(
            $"hotkey {binding.Modifiers}+0x{binding.VirtualKey:X2} -> {effect.GetType().Name}"
        );
        switch (effect)
        {
            case RunCommand run:
                _session.Execute(run.Command);
                if (_session.Root.FocusedWindow is TilingWindow focused)
                {
                    Console.WriteLine($"focus - {_windowSystem.GetTitle(focused.WindowId)}");
                }

                break;
            case CloseFocusedWindow:
                _session.CloseFocused();
                break;
            case ExitWm:
                _quit();
                break;
            case ReconcileDisplays:
                _session.ReconcileDisplays();
                break;
        }
    }
}
