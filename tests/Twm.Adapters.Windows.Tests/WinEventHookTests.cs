using System.Threading;
using Twm.Adapters.Windows.Tests.Fixtures;
using Twm.Domain.Tree;

namespace Twm.Adapters.Windows.Tests;

/// <summary>
/// End-to-end tests for <see cref="WinEventHook" />: install the OS hook,
/// trigger a window event by creating a real window, verify the callback
/// fired with the expected <see cref="WindowEventKind" />.
/// </summary>
public sealed class WinEventHookTests : IDisposable
{
    private readonly WinEventHook _hook = new();
    private readonly List<(WindowEventKind Kind, WindowId Id)> _events = [];
    private readonly ManualResetEventSlim _eventReceived = new();

    public static bool IsWindows => OperatingSystem.IsWindows();

    public void Dispose()
    {
        _hook.Dispose();
        _eventReceived.Dispose();
    }

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void Install_ThenCreateWindow_FiresAppeared()
    {
        _hook.Install(OnEvent);

        using var window = new TestWindow("twm-event-create");

        _eventReceived
            .Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)
            .ShouldBeTrue();
        lock (_events)
        {
            _events.ShouldContain(e =>
                e.Kind == WindowEventKind.Appeared && e.Id.Value == window.Handle
            );
        }
    }

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void Install_Twice_Throws()
    {
        _hook.Install(OnEvent);

        using var second = new WinEventHook();
        Should.Throw<InvalidOperationException>(() => second.Install(OnEvent));
    }

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void Dispose_UnhooksAndStopsReceiving()
    {
        _hook.Install(OnEvent);

        using var first = new TestWindow("twm-event-first");
        _eventReceived
            .Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)
            .ShouldBeTrue();
        _eventReceived.Reset();

        _hook.Dispose();

        using var second = new TestWindow("twm-event-after");

        // No event with second's HWND should have arrived: Dispose synchronously
        // unhooked, and the second window's HWND allocation is synchronous on
        // this thread, so no callback can fire after Dispose for it.
        lock (_events)
        {
            _events.ShouldNotContain(e => e.Id.Value == second.Handle);
        }
    }

    private void OnEvent(WindowEventKind kind, WindowId id)
    {
        // The WinEvent callback fires on the OS hook thread, so the list
        // mutations need locking for the test thread to read safely.
        lock (_events)
        {
            _events.Add((kind, id));
        }

        _eventReceived.Set();
    }
}
