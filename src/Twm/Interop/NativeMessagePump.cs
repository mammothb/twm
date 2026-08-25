namespace Twm.Interop;

/// <summary>
/// Background thread that owns all native hooks and pumps the win32
/// message queue — both winevents and LL keyboard callbacks are delivered
/// through it. Handlers must be fast: they only enqueue into the main
/// loop's channel.
/// </summary>
public sealed class NativeMessagePump : IDisposable
{
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _ready = new(false);
    private readonly Action<uint, nint> _onWinEvent;
    private readonly KeyboardHook.ComboHandler _onCombo;

    private volatile bool _stopped;
    private uint _threadId;
    private WinEvent.Hook? _winevents;
    private KeyboardHook? _keyboard;

    private NativeMessagePump(Action<uint, nint> onWinEvent, KeyboardHook.ComboHandler onCombo)
    {
        _onWinEvent = onWinEvent;
        _onCombo = onCombo;
        _thread = new Thread(Run) { IsBackground = true, Name = "twm-native-pump" };
    }

    public static NativeMessagePump Start(
        Action<uint, nint> onWinEvent,
        KeyboardHook.ComboHandler onCombo
    )
    {
        var pump = new NativeMessagePump(onWinEvent, onCombo);
        pump._thread.Start();
        pump._ready.Wait(TimeSpan.FromSeconds(5));
        return pump;
    }

    private void Run()
    {
        _threadId = Kernel32.GetCurrentThreadId();
        _winevents = new WinEvent.Hook(_onWinEvent);
        _keyboard = new KeyboardHook(_onCombo);
        _ready.Set();

        while (!_stopped)
        {
            int result = User32.GetMessage(out User32.MSG msg, 0, 0, 0);
            if (result <= 0)
            {
                break;
            }

            User32.DispatchMessage(ref msg);
        }
    }

    public void Stop()
    {
        if (_stopped)
        {
            return;
        }

        _stopped = true;
        User32.PostThreadMessage(_threadId, User32.WM_QUIT, 0, 0); // wake GetMessage
        _thread.Join(2000);

        _keyboard?.Dispose();
        _winevents?.Dispose();
    }

    public void Dispose() => Stop();
}
