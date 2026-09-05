using System.Runtime.InteropServices;

namespace Twm.Adapters.Windows;

/// <summary>
/// The Win32 message pump for the WM thread. A thread-message is sufficient,
/// <c>RegisterHotKey</c> (with a null window) posts <c>WM_HOTKEY</c> as a
/// thread message, and <c>WINEVENT_OUTOFCONTEXT</c> hooks are delivered while
/// this loops pumps, so no message-only window is needed.
/// </summary>
public static partial class MessageLoop
{
    /// <summary>
    /// Thread message Twm posts to wake the pump to drain queued IPC work
    /// (WM_APP).
    /// </summary>
    public const uint WmApp = 0x8000;

    /// <summary>
    /// Thread message Twm posts to ask the pump to quit gracefully, e.g., on
    /// Ctrl+C.
    /// </summary>
    public const uint WmAppQuit = 0x8001;

    /// <summary>
    /// Timer message (WM_TIMER) delivered as a thread message for a
    /// null-window timer.
    /// </summary>
    public const uint WmTimer = 0x0113;

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMessage
    {
        public nint Hwnd;
        public uint Message;
        public nint WParam;
        public nint LParam;
        public uint Time;
        public int PointX;
        public int PointY;
    }

    [LibraryImport("user32.dll")]
    private static partial nint DispatchMessageW(in NativeMessage lpMsg);

    [LibraryImport("user32.dll")]
    private static partial uint GetCurrentThreadId();

    [LibraryImport("user32.dll")]
    private static partial int GetMessageW(
        out NativeMessage lpMsg,
        nint hWnd,
        uint wMsgFilterMin,
        uint wMsgFilterMax
    );

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool KillTimer(nint hWnd, nuint uIDEvent);

    [LibraryImport("user32.dll")]
    private static partial void PostQuitMessage(int nExitCode);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool PostThreadMessageW(
        uint idThread,
        uint Msg,
        nint wParam,
        nint lParam
    );

    [LibraryImport("user32.dll")]
    private static partial nuint SetTimer(
        nint hWnd,
        nuint nIDEvent,
        uint uElapse,
        nint lpTimerFunc
    );

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool TranslateMessage(in NativeMessage lpMsg);

    /// <summary>
    /// The id of the calling thread, for posting wake messages back to the
    /// pump.
    /// </summary>
    public static uint CurrentThreadId() => GetCurrentThreadId();

    /// <summary>
    /// Posts <paramref name="message" /> to <paramref name="threadId" />'s
    /// message queue.
    /// </summary>
    public static bool Post(uint threadId, uint message) =>
        PostThreadMessageW(threadId, message, 0, 0);

    /// <summary>
    /// Requests the pump stop by posting <c>WM_QUIT</c> to this thread.
    /// </summary>
    public static void Quit() => PostQuitMessage(0);

    /// <summary>
    /// Pumps messages until <c>WM_QUIT</c>. <paramref name="onThreadMessage" />
    /// is invoked for every message to handle <c>WM_HOTKEY</c>, which arrives
    /// as a thread message with no target window.
    /// </summary>
    public static void Run(Action<uint, nint, nint>? onThreadMessage = null)
    {
        while (true)
        {
            int result = GetMessageW(out NativeMessage message, 0, 0, 0);
            if (result is 0 or -1)
            {
                break; // WM_QUIT (0) or error (-1)
            }

            onThreadMessage?.Invoke(message.Message, message.WParam, message.LParam);
            TranslateMessage(in message);
            DispatchMessageW(in message);
        }
    }

    /// <summary>
    /// Starts a null-window timer; <c>WM_TIMER</c> then arrives as a thread
    /// message on the pump (surfaced via <see cref="Run" />'s callback).
    /// Returns the timer id for <see cref="StopTimer" />.
    /// </summary>
    public static nuint StartTimer(uint intervalMs) => SetTimer(0, 0, intervalMs, 0);

    /// <summary>Stops a timer started by <see cref="StartTimer" />.</summary>
    public static void StopTimer(nuint id) => KillTimer(0, id);
}
