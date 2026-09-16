using Twm.Adapters.Windows;
using Twm.Application.Coordination;
using Twm.Application.InboundPorts;
using Twm.Application.OutboundPorts;
using Twm.Domain.Geometry;
using Twm.TestSupport.Fakes;

namespace Twm.App.Tests;

public sealed class WmMessageRouterTests
{
    [Fact]
    public void Handle_WmAppQuit_InvokesOnQuit()
    {
        (WmSession session, _, FakeWindowSystem windows) = BuildSession();
        List<string> quitCalls = [];
        WmMessageRouter router = new(
            session,
            windows,
            new HotkeyManager(),
            new Dictionary<KeyBinding, KeyEffect>(),
            new WmThreadDispatcher(wake: () => true, handleOnWmThread: _ => ""),
            statusBar: null,
            onQuit: () => quitCalls.Add("quit")
        );

        router.Handle(MessageLoop.WmAppQuit, wParam: 0, lParam: 0);

        quitCalls.ShouldContain("quit");
    }

    [Fact]
    public void Handle_WmApp_DoesNotInvokeOnQuit()
    {
        (WmSession session, _, FakeWindowSystem windows) = BuildSession();
        List<string> quitCalls = [];
        WmMessageRouter router = new(
            session,
            windows,
            new HotkeyManager(),
            new Dictionary<KeyBinding, KeyEffect>(),
            new WmThreadDispatcher(wake: () => true, handleOnWmThread: _ => ""),
            statusBar: null,
            onQuit: () => quitCalls.Add("quit")
        );

        router.Handle(MessageLoop.WmApp, wParam: 0, lParam: 0);

        quitCalls.ShouldBeEmpty();
    }

    [Fact]
    public void Handle_WmTimer_WithoutStatusBar_DoesNotThrow()
    {
        (WmSession session, _, FakeWindowSystem windows) = BuildSession();
        WmMessageRouter router = new(
            session,
            windows,
            new HotkeyManager(),
            new Dictionary<KeyBinding, KeyEffect>(),
            new WmThreadDispatcher(wake: () => true, handleOnWmThread: _ => ""),
            statusBar: null,
            onQuit: () => { }
        );

        Should.NotThrow(() => router.Handle(MessageLoop.WmTimer, wParam: 0, lParam: 0));
    }

    [Fact]
    public void Handle_HotkeyMessageNotInKeymap_IsNoOp()
    {
        (WmSession session, _, FakeWindowSystem windows) = BuildSession();
        List<string> quitCalls = [];
        WmMessageRouter router = new(
            session,
            windows,
            new HotkeyManager(),
            new Dictionary<KeyBinding, KeyEffect>(),
            new WmThreadDispatcher(wake: () => true, handleOnWmThread: _ => ""),
            statusBar: null,
            onQuit: () => quitCalls.Add("quit")
        );

        // WM_HOTKEY = 0x0312. hotkeyManager has no bindings so TryResolve
        // returns false; keymap is empty anyway. Neither path fires.
        router.Handle(0x0312, wParam: 1, lParam: 0);

        quitCalls.ShouldBeEmpty();
    }

    private static (
        WmSession session,
        FakeMonitorSystem monitors,
        FakeWindowSystem windows
    ) BuildSession()
    {
        var monitors = new FakeMonitorSystem(
            new MonitorInfo(
                new MonitorId(1),
                new Rect(0, 0, 1920, 1080),
                new Rect(0, 0, 1920, 1080),
                IsPrimary: true
            )
        );
        var windows = new FakeWindowSystem();
        var session = new WmSession(monitors, windows);
        return (session, monitors, windows);
    }
}
