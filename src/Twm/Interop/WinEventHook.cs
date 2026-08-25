using System.Runtime.InteropServices;

namespace Twm.Interop;

internal static class WinEvent
{
    public const uint SystemForeground = 0x0003;
    public const uint SystemMoveSizeStart = 0x000E;
    public const uint SystemMoveSizeEnd = 0x000F;
    public const uint ObjectDestroy = 0x8001;
    public const uint ObjectShow = 0x8002;
    public const uint ObjectHide = 0x8003;
    public const uint ObjectLocationChange = 0x800B;
    public const uint ObjectCloaked = 0x8017;
    public const uint ObjectUncloaked = 0x8018;

    /// <summary>Range covered by our single hook.</summary>
    public const uint HookMin = SystemForeground;
    public const uint HookMax = ObjectUncloaked;

    public const int ObjIdWindow = 0;
    private const uint WineventOutOfContext = 0x0000;

    public delegate void Proc(
        nint hook,
        uint eventId,
        nint hwnd,
        int idObject,
        int idChild,
        uint thread,
        uint time
    );

    [DllImport("user32.dll")]
    private static extern nint SetWinEventHook(
        uint eventMin,
        uint eventMax,
        nint module,
        Proc proc,
        uint processId,
        uint threadId,
        uint flags
    );

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(nint hook);

    /// <summary>
    /// Wraps one global winevent hook range. The delegate reference is
    /// held for the hook's lifetime so it can't be collected.
    /// </summary>
    public sealed class Hook : IDisposable
    {
        private readonly Proc _proc;
        private readonly Action<uint, nint> _dispatch;
        private nint _handle;

        public Hook(Action<uint, nint> dispatch)
        {
            _dispatch = dispatch;
            _proc = OnEvent;
            _handle = SetWinEventHook(HookMin, HookMax, 0, _proc, 0, 0, WineventOutOfContext);
            if (_handle == nint.Zero)
            {
                throw new InvalidOperationException(
                    $"SetWinEventHook failed (win32 error {Marshal.GetLastWin32Error()})."
                );
            }
        }

        private void OnEvent(
            nint hook,
            uint eventId,
            nint hwnd,
            int idObject,
            int idChild,
            uint thread,
            uint time
        )
        {
            // Only top-level window events interest us; everything else is noise.
            if (idObject != ObjIdWindow || idChild != 0 || hwnd == nint.Zero)
            {
                return;
            }

            _dispatch(eventId, hwnd);
        }

        public void Dispose()
        {
            if (_handle != nint.Zero)
            {
                UnhookWinEvent(_handle);
                _handle = nint.Zero;
            }
        }
    }
}
