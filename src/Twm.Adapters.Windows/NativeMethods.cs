using System.Runtime.InteropServices;
using Twm.Domain.Geometry;

namespace Twm.Adapters.Windows;

/// <summary>
/// Raw Win32 interop, source-generated via
/// <see cref="LibraryImportAttribute" /> (AOT-ready). Plain net10.0: these
/// declarations compile on Linux but only resolve at runtime on Windows (x64).
/// Enumeration uses unmanaged function pointers + a <see cref="GCHandle" />
/// rather than delegate marshalling, since <c>LibraryImport</c> only marshals
/// blittable types. Kept internal; callers use the high-level helpers at the
/// bottom.
/// </summary>
internal static unsafe partial class NativeMethods
{
    // GW_OWNER
    private const uint GwOwner = 4;

    // MONITORINFOF_PRIMARY
    private const uint MonitorinfofPrimary = 0x00000001;

    // SPI_SETFOREGROUNDLOCKTIMEOUT
    private const uint SpiSetForegroundLockTimeout = 0x2001;

    // SPIF_SENDCHANGE
    private const uint SpifSendChange = 0x0002;

    // ATTACH_PARENT_PROCESS
    private const uint AttachParentProcess = 0xFFFFFFFF;

    // PROCESS_QUERY_LIMITED_INFORMATION
    private const uint ProcessQueryLimitedInformation = 0x1000;

    // TOKEN_QUERY
    private const uint TokenQuery = 0x0008;

    // TOKEN_INFORMATION_CLASS.TokenIntegrityLevel
    private const int TokenIntegrityLevel = 25;

    // SECURITY_MANDATORY_MEDIUM_RID
    private const int MediumIntegrityRid = 0x2000;

    // Twm's own process integrity level, read once. A window whose process
    // integrity is strictly higher can't be repositioned by us (UIPI), so it is
    // treated as elevated and left alone.
    private static readonly Lazy<int> s_ourIntegrityRid = new(ComputeOurIntegrityRid);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect32
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfoData
    {
        public int CbSize;
        public Rect32 Monitor;
        public Rect32 Work;
        public uint Flags;
    }

    // ========================================================================
    // user32.dll
    // ========================================================================
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool BringWindowToTop(nint hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EnumDisplayMonitors(
        nint hdc,
        nint lprcClip,
        delegate* unmanaged<nint, nint, nint, nint, int> lpfnEnum,
        nint dwData
    );

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EnumWindows(
        delegate* unmanaged<nint, nint, int> lpEnumFunc,
        nint lParam
    );

    [LibraryImport("user32.dll")]
    private static partial int GetClassNameW(nint hWnd, char* lpClassName, int nMaxCount);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetMonitorInfoW(nint hMonitor, ref MonitorInfoData lpmi);

    [LibraryImport("user32.dll")]
    private static partial nint GetWindow(nint hWnd, uint uCmd);

    [LibraryImport("user32.dll")]
    private static partial nint GetWindowLongPtrW(nint hWnd, GetWindowLong nIndex);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowRect(nint hWnd, out Rect32 lpRect);

    [LibraryImport("user32.dll")]
    private static partial int GetWindowTextW(nint hWnd, char* lpString, int nMaxCount);

    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsIconic(nint hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsWindowVisible(nint hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsZoomed(nint hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool PostMessageW(
        nint hWnd,
        WindowMessage Msg,
        nint wParam,
        nint lParam
    );

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(nint hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetProcessDpiAwarenessContext(nint value);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowPos(
        nint hWnd,
        nint hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        SetWindowPosFlags uFlags
    );

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(nint hWnd, ShowWindowCommand nCmdShow);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SystemParametersInfoW(
        uint uiAction,
        uint uiParam,
        nint pvParam,
        uint fWinIni
    );

    // ========================================================================
    // kernel32.dll
    // ========================================================================

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AllocConsole();

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AttachConsole(uint dwProcessId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(nint hObject);

    [LibraryImport("kernel32.dll")]
    private static partial nint GetCurrentProcess();

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint OpenProcess(
        uint dwDesiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
        uint dwProcessId
    );

    // ========================================================================
    // advapi32.dll
    // ========================================================================

    [LibraryImport("advapi32.dll", SetLastError = true)]
    private static partial uint* GetSidSubAuthority(nint pSid, uint nSubAuthority);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    private static partial byte* GetSidSubAuthorityCount(nint pSid);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetTokenInformation(
        nint TokenHandle,
        int TokenInformationClass,
        byte* TokenInformation,
        uint TokenInformationLength,
        out uint ReturnLength
    );

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenProcessToken(
        nint ProcessHandle,
        uint DesiredAccess,
        out nint TokenHandle
    );

    // ========================================================================
    // dwmapi.dll
    // ========================================================================

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmGetWindowAttribute(
        nint hWnd,
        DwmWindowAttribute dwAttribute,
        out int pvAttribute,
        int cbAttribute
    );

    [LibraryImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")]
    private static partial int DwmGetWindowFrameBounds(
        nint hWnd,
        DwmWindowAttribute dwAttribute,
        out Rect32 pvAttribute,
        int cbAttribute
    );

    [UnmanagedCallersOnly]
    private static int CollectWindow(nint hWnd, nint lParam)
    {
        GCHandle handle = GCHandle.FromIntPtr(lParam);
        if (handle.Target is List<nint> list)
        {
            list.Add(hWnd);
        }
        return 1; // TRUE: continue enumeration
    }

    [UnmanagedCallersOnly]
    private static int CollectMonitor(nint hMonitor, nint hdcMonitor, nint lprcMonitor, nint dwData)
    {
        GCHandle handle = GCHandle.FromIntPtr(dwData);
        if (handle.Target is List<nint> list)
        {
            list.Add(hMonitor);
        }
        return 1; // TRUE: continue enumeration
    }

    internal static List<nint> TopLevelWindows()
    {
        List<nint> handles = [];
        GCHandle gc = GCHandle.Alloc(handles);
        try
        {
            EnumWindows(&CollectWindow, GCHandle.ToIntPtr(gc));
        }
        finally
        {
            gc.Free();
        }
        return handles;
    }

    internal static List<nint> MonitorHandles()
    {
        List<nint> handles = [];
        GCHandle gc = GCHandle.Alloc(handles);
        try
        {
            EnumDisplayMonitors(0, 0, &CollectMonitor, GCHandle.ToIntPtr(gc));
        }
        finally
        {
            gc.Free();
        }
        return handles;
    }

    internal static bool TryGetMonitorInfo(
        nint monitor,
        out Rect bounds,
        out Rect workArea,
        out bool isPrimary
    )
    {
        MonitorInfoData info = default;
        info.CbSize = sizeof(MonitorInfoData);
        if (!GetMonitorInfoW(monitor, ref info))
        {
            bounds = default;
            workArea = default;
            isPrimary = false;
            return false;
        }

        bounds = ToRect(info.Monitor);
        workArea = ToRect(info.Work);
        isPrimary = (info.Flags & MonitorinfofPrimary) != 0;
        return true;
    }

    internal static Rect GetBounds(nint window) =>
        GetWindowRect(window, out Rect32 rect) ? ToRect(rect) : default;

    internal static string GetClassName(nint window)
    {
        Span<char> buffer = stackalloc char[256];
        int length;
        fixed (char* p = buffer)
        {
            length = GetClassNameW(window, p, buffer.Length);
        }
        return length > 0 ? new string(buffer[..length]) : "";
    }

    internal static string GetWindowText(nint window)
    {
        Span<char> buffer = stackalloc char[256];
        int length;
        fixed (char* p = buffer)
        {
            length = GetWindowTextW(window, p, buffer.Length);
        }
        return length > 0 ? new string(buffer[..length]) : "";
    }

    internal static void ClearTopmostIfSet(nint window)
    {
        var exStyle = (ExtendedWindowStyle)GetWindowLongPtrW(window, GetWindowLong.ExStyle);
        if ((exStyle & ExtendedWindowStyle.Topmost) != 0)
        {
            // HWND_NOTOPMOST == (HWND)-2; keep activation/position/size, only
            // change the z-band
            SetWindowPos(
                window,
                (nint)(-2),
                0,
                0,
                0,
                0,
                SetWindowPosFlags.NoActivate | SetWindowPosFlags.NoMove | SetWindowPosFlags.NoSize
            );
        }
    }

    /// <summary>WS_CAPTION set: the window has a real title bar.</summary>
    internal static bool HasCaption(nint window)
    {
        var style = (WindowStyle)GetWindowLongPtrW(window, GetWindowLong.Style);
        return (style & WindowStyle.Caption) == WindowStyle.Caption;
    }

    /// <summary>
    /// WS_EX_WINDOWEDGE set: a standar raised app-window frame.
    /// </summary>
    internal static bool HasWindowEdge(nint window)
    {
        var exStyle = (ExtendedWindowStyle)GetWindowLongPtrW(window, GetWindowLong.ExStyle);
        return (exStyle & ExtendedWindowStyle.WindowEdge) != 0;
    }

    internal static bool IsChildWindow(nint window)
    {
        var style = (WindowStyle)GetWindowLongPtrW(window, GetWindowLong.Style);
        return (style & WindowStyle.Child) != 0;
    }

    internal static bool IsCloaked(nint window)
    {
        return DwmGetWindowAttribute(
                window,
                DwmWindowAttribute.Cloaked,
                out int cloaked,
                sizeof(int)
            ) == 0
            && cloaked != 0;
    }

    /// <summary>
    /// Whether the window's process runs at a strictly higher integrity level
    /// than Twm (so we can't reposition it), or whose integrity we cannot read
    /// (assumed higher). Comparing against our own level means an elevated Twm
    /// correctly manages elevated windows.
    /// </summary>
    internal static bool IsElevated(nint window)
    {
        int windowRid = ProcessIntegrityRid(window);
        return windowRid < 0 || windowRid > s_ourIntegrityRid.Value;
    }

    /// <summary>
    /// WS_EX_LAYERED: a translucent overlay/toolbar (Teams sharing bar, capture
    /// overlays, PiP, flyouts), not a normal app window.
    /// </summary>
    internal static bool IsLayered(nint window)
    {
        var exStyle = (ExtendedWindowStyle)GetWindowLongPtrW(window, GetWindowLong.ExStyle);
        return (exStyle & ExtendedWindowStyle.Layered) != 0;
    }

    /// <summary>
    /// Owner window + no title bar: a menu/flyout popup (WinUI "PopupHost",
    /// etc.).
    /// </summary>
    internal static bool IsMenuPopup(nint window)
    {
        if (GetWindow(window, GwOwner) == 0)
        {
            return false;
        }
        var style = (WindowStyle)GetWindowLongPtrW(window, GetWindowLong.Style);
        return (style & WindowStyle.Caption) == 0;
    }

    internal static bool IsMinimized(nint window) => IsIconic(window);

    internal static bool IsNoActivate(nint window)
    {
        var exStyle = (ExtendedWindowStyle)GetWindowLongPtrW(window, GetWindowLong.ExStyle);
        return (exStyle & ExtendedWindowStyle.NoActivate) != 0;
    }

    internal static bool IsToolWindow(nint window)
    {
        var exStyle = (ExtendedWindowStyle)GetWindowLongPtrW(window, GetWindowLong.ExStyle);
        return (exStyle & ExtendedWindowStyle.ToolWindow) != 0;
    }

    internal static bool IsVisible(nint window) => IsWindowVisible(window);

    // Create a dedicated console window for this process (used by --console
    // when there is no launching terminal to attach to). No-op safe: fails if
    // a console is already attached.
    internal static void AllocateConsole() => AllocConsole();

    // Bind this process to its launching terminal's console so Console output
    // is visible there. If there is no parent console (launched detached),
    // AttachConsole fails and we ignore it
    internal static void AttachParentConsole() => AttachConsole(AttachParentProcess);

    internal static void Close(nint window) => PostMessageW(window, WindowMessage.Close, 0, 0);

    // Setting the foreground lock timeout to 0 lets keyboard-driven
    // SetForegroundWindow actually activate the target window instead of only
    // flashing its taskbar button
    internal static void DisableForegroundLockTimeout() =>
        SystemParametersInfoW(SpiSetForegroundLockTimeout, 0, 0, SpifSendChange);

    // DPI_AWARENESS_CONTEXT_PER_MONITOR_V2 = (HANDLE)-4
    internal static void EnablePerMonitorV2Dpi() => SetProcessDpiAwarenessContext((nint)(-4));

    internal static void Foreground(nint window)
    {
        SetForegroundWindow(window);
        BringWindowToTop(window);
    }

    internal static void SetBounds(nint window, Rect bounds)
    {
        // Drop any always-on-top flag so the window tiles flat with its
        // neighbors
        ClearTopmostIfSet(window);

        // A maximized window keeps its WS_MAXIMIZE state and won't tile cleanly
        // via SetWindowPos; restore it to a normal window first
        if (IsZoomed(window))
        {
            ShowWindow(window, ShowWindowCommand.Restore);
        }

        // GetWindowRect includes the invisible DWM resize border;
        // DWMWA_EXTENDED_FRAME_BOUNDS gives the visible frame. Expand the
        // target by their difference so adjacent windows' visible edges align
        // instead of gapping/overlapping
        if (
            GetWindowRect(window, out Rect32 outer)
            && DwmGetWindowFrameBounds(
                window,
                DwmWindowAttribute.ExtendedFrameBounds,
                out Rect32 visible,
                sizeof(Rect32)
            ) == 0
        )
        {
            int left = visible.Left - outer.Left;
            int top = visible.Top - outer.Top;
            int right = outer.Right - visible.Right;
            int bottom = outer.Bottom - visible.Bottom;

            SetWindowPos(
                hWnd: window,
                hWndInsertAfter: 0,
                x: bounds.X - left,
                y: bounds.Y - top,
                cx: bounds.Width + left + right,
                cy: bounds.Height + top + bottom,
                uFlags: SetWindowPosFlags.Tile
            );
            return;
        }
        SetWindowPos(
            hWnd: window,
            hWndInsertAfter: 0,
            x: bounds.X,
            y: bounds.Y,
            cx: bounds.Width,
            cy: bounds.Height,
            uFlags: SetWindowPosFlags.Tile
        );
    }

    private static int ProcessIntegrityRid(nint window)
    {
        GetWindowThreadProcessId(window, out uint processId);
        if (processId == 0)
        {
            return -1;
        }

        nint processHandle = OpenProcess(ProcessQueryLimitedInformation, false, processId);
        if (processHandle == 0)
        {
            // can't open a higher-integrity process from a medium-integrity Twm
            return -1;
        }

        try
        {
            if (!OpenProcessToken(processHandle, TokenQuery, out nint tokenHandle))
            {
                return -1;
            }

            try
            {
                return IntegrityRid(tokenHandle);
            }
            finally
            {
                CloseHandle(tokenHandle);
            }
        }
        finally
        {
            CloseHandle(processHandle);
        }
    }

    private static int IntegrityRid(nint tokenHandle)
    {
        GetTokenInformation(tokenHandle, TokenIntegrityLevel, null, 0, out uint needed);
        if (needed == 0 || needed > 256)
        {
            return -1;
        }

        byte* buffer = stackalloc byte[(int)needed];
        if (!GetTokenInformation(tokenHandle, TokenIntegrityLevel, buffer, needed, out _))
        {
            return -1;
        }

        // TOKEN_MANDATORY_LABEL { SID_AND_ATTRIBUTES { PSID Sid;
        // DWORD Attributes } }: the first pointer-sized field is the integrity
        // SID; its last sub-authority is the integrity RID
        nint sid = *(nint*)buffer;
        byte* countPtr = GetSidSubAuthorityCount(sid);
        if (countPtr is null || *countPtr == 0)
        {
            return -1;
        }

        uint* ridPtr = GetSidSubAuthority(sid, (uint)(*countPtr - 1));
        return ridPtr is null ? -1 : (int)*ridPtr;
    }

    private static int ComputeOurIntegrityRid()
    {
        if (!OpenProcessToken(GetCurrentProcess(), TokenQuery, out nint tokenHandle))
        {
            return MediumIntegrityRid;
        }

        try
        {
            int rid = IntegrityRid(tokenHandle);
            return rid < 0 ? MediumIntegrityRid : rid;
        }
        finally
        {
            CloseHandle(tokenHandle);
        }
    }

    private static Rect ToRect(Rect32 r) => new(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
}
