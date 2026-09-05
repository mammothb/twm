using System.Runtime.InteropServices;

namespace Twm.Adapters.Windows;

internal static unsafe partial class NativeMethods
{
    // TRANSPARENT
    internal const int BkModeTransparent = 1;

    /// DEFAULT_GUI_FONT
    internal const int DefaultGuiFont = 17;

    [StructLayout(LayoutKind.Sequential)]
    internal struct PaintStruct
    {
        public nint Hdc;
        public int Erase;
        public Rect32 Paint;
        public int Restore;
        public int IncUpdate;
        public fixed byte Reserved[32];
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WndClassExW
    {
        public uint CbSize;
        public uint Style;
        public delegate* unmanaged<nint, uint, nint, nint, nint> WndProc;
        public int ClsExtra;
        public int WndExtra;
        public nint Instance;
        public nint Icon;
        public nint Cursor;
        public nint Background;
        public char* MenuName;
        public char* ClassName;
        public nint IconSm;
    }

    // ========================================================================
    // user32.dll
    // ========================================================================
    [LibraryImport("user32.dll")]
    internal static partial nint BeginPaint(nint hWnd, out PaintStruct lpPaint);

    [LibraryImport("user32.dll")]
    internal static partial nint CreateWindowExW(
        ExtendedWindowStyle dwExStyle,
        char* lpClassName,
        char* lpWindowName,
        WindowStyle dwStyle,
        int x,
        int y,
        int nWidth,
        int nHeight,
        nint hWndParent,
        nint hMenu,
        nint hInstance,
        nint lpParam
    );

    [LibraryImport("user32.dll")]
    internal static partial nint DefWindowProcW(nint hWnd, uint Msg, nint wParam, nint lParam);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyWindow(nint hWnd);

    [LibraryImport("user32.dll")]
    internal static partial int DrawTextW(
        nint hdc,
        char* lpchText,
        int cchText,
        ref Rect32 lprc,
        DrawTextFormat format
    );

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool EndPaint(nint hWnd, in PaintStruct lpPaint);

    [LibraryImport("user32.dll")]
    internal static partial int FillRect(nint hdc, in Rect32 lprc, nint hbr);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetClientRect(nint hWnd, out Rect32 lpRect);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool InvalidateRect(
        nint hWnd,
        nint lpRect,
        [MarshalAs(UnmanagedType.Bool)] bool bErase
    );

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool MoveWindow(
        nint hWnd,
        int x,
        int y,
        int nWidth,
        int nHeight,
        [MarshalAs(UnmanagedType.Bool)] bool bRepaint
    );

    [LibraryImport("user32.dll")]
    internal static partial int SetWindowRgn(
        nint hWnd,
        nint hRgn,
        [MarshalAs(UnmanagedType.Bool)] bool bRedraw
    );

    [LibraryImport("user32.dll")]
    internal static partial ushort RegisterClassExW(in WndClassExW lpwcx);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool UnregisterClassW(char* lpClassName, nint hInstance);

    // ========================================================================
    // gdi32.dll
    // ========================================================================
    [LibraryImport("gdi32.dll")]
    internal static partial nint CombineRgn(nint hrgnDst, nint hrgnSrc1, nint hrgnSrc2, int iMode);

    [LibraryImport("gdi32.dll")]
    internal static partial nint CreateRectRgn(int x1, int y1, int x2, int y2);

    [LibraryImport("gdi32.dll")]
    internal static partial nint CreateSolidBrush(uint color);

    [LibraryImport("gdi32.dll")]
    internal static partial nint GetStockObject(int i);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DeleteObject(nint ho);

    [LibraryImport("gdi32.dll")]
    internal static partial nint SelectObject(nint hdc, nint h);

    [LibraryImport("gdi32.dll")]
    internal static partial nint SetBkMode(nint hdc, int mode);

    [LibraryImport("gdi32.dll")]
    internal static partial nint SetTextColor(nint hdc, uint color);

    // ========================================================================
    // kernel32.dll
    // ========================================================================
    [LibraryImport("kernel32.dll")]
    internal static partial nint GetModuleHandleW(char* lpModuleName);
}
