using System.Runtime.InteropServices;
using System.Text;

namespace Twm.Interop;

/// <summary>P/Invoke declarations for user32. x86/x64 covered for GetWindowLong.</summary>
internal static partial class User32
{
    // Window messages
    internal const uint WM_KEYDOWN = 0x0100;
    internal const uint WM_SYSKEYDOWN = 0x0104;
    internal const uint WM_CLOSE = 0x0010;
    internal const uint WM_QUIT = 0x0012;

    // SetWindowPos flags
    internal const uint SWP_NOZORDER = 0x0004;
    internal const uint SWP_NOACTIVATE = 0x0010;

    // Class styles / extended styles
    internal const long WS_EX_TOOLWINDOW = 0x00000080L;

    // Virtual keys
    internal const int VK_SHIFT = 0x10;
    internal const int VK_CONTROL = 0x11;
    internal const int VK_MENU = 0x12;
    internal const int VK_LWIN = 0x5B;
    internal const int VK_RWIN = 0x5C;

    // GetWindowPlacement
    internal const int SW_SHOWMINIMIZED = 2;
    internal const int SW_SHOWMAXIMIZED = 3;
    internal const int SW_RESTORE = 9;

    internal const uint MONITOR_DEFAULTTONEAREST = 2;
    internal const uint MONITOR_DEFAULTTONULL = 0;

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left,
            Top,
            Right,
            Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int X,
            Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MSG
    {
        public nint Hwnd;
        public uint Message;
        public nint WParam;
        public nint LParam;
        public uint Time;
        public POINT Pt;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WINDOWPLACEMENT
    {
        public uint Length;
        public uint Flags;
        public uint ShowCmd;
        public POINT PtMinPosition;
        public POINT PtMaxPosition;
        public RECT RcNormalPosition;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MONITORINFO
    {
        public uint CbSize;
        public RECT RcMonitor;
        public RECT RcWork;
        public uint DwFlags;
    }

    public delegate bool EnumWindowsProc(nint hWnd, nint lParam);
    public delegate bool MonitorEnumProc(nint hMonitor, nint hdc, ref RECT rect, nint data);
    public delegate nint LowLevelKeyboardProc(int nCode, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc proc, nint lParam);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(nint hWnd);

    [DllImport("user32.dll")]
    public static extern bool IsIconic(nint hWnd);

    [DllImport("user32.dll")]
    public static extern bool IsWindow(nint hWnd);

    [DllImport("user32.dll")]
    public static extern nint GetAncestor(nint hWnd, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowTextLength(nint hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(nint hWnd, StringBuilder text, int maxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetClassName(nint hWnd, StringBuilder className, int maxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(nint hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern nint GetWindowLongPtr(nint hWnd, int nIndex);

    /// <summary>Architect-portable GetWindowLong. nIndex: GWL_STYLE(-16)/GWL_EXSTYLE(-20).</summary>
    public static long GetWindowStyle(nint hWnd, int nIndex) =>
        IntPtr.Size == 8 ? GetWindowLongPtr(hWnd, nIndex) : GetWindowLong(hWnd, nIndex);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(nint hWnd, out RECT rect);

    [DllImport("user32.dll")]
    public static extern bool GetWindowPlacement(nint hWnd, ref WINDOWPLACEMENT placement);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(nint hWnd, int showCommand);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(
        nint hWnd,
        nint insertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint flags
    );

    [DllImport("user32.dll")]
    public static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool attach);

    [DllImport("user32.dll")]
    public static extern bool PostMessage(nint hWnd, uint msg, nuint wParam, nint lParam);

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    public static extern nint MonitorFromWindow(nint hWnd, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFO info);

    [DllImport("user32.dll")]
    public static extern bool EnumDisplayMonitors(
        nint hdc,
        nint clipRect,
        MonitorEnumProc proc,
        nint data
    );

    [DllImport("user32.dll")]
    public static extern nint SetWindowsHookEx(
        int idHook,
        LowLevelKeyboardProc proc,
        nint module,
        uint threadId
    );

    [DllImport("user32.dll")]
    public static extern bool UnhookWindowsHookEx(nint hook);

    [DllImport("user32.dll")]
    public static extern nint CallNextHookEx(nint hook, int nCode, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    public static extern int GetMessage(out MSG msg, nint hWnd, uint minFilter, uint maxFilter);

    [DllImport("user32.dll")]
    public static extern void DispatchMessage(ref MSG msg);

    [DllImport("user32.dll")]
    public static extern bool PostThreadMessage(uint threadId, uint msg, nuint wParam, nint lParam);

    [DllImport("user32.dll")]
    public static extern bool SetProcessDpiAwarenessContext(nint value);
}
