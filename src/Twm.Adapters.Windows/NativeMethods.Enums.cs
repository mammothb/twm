namespace Twm.Adapters.Windows;

internal static partial class NativeMethods
{
    /// <summary>GetWindowLongPtr indices.</summary>
    internal enum GetWindowLong
    {
        Style = -16,
        ExStyle = -20,
    }

    /// <summary>Window styles.</summary>
    [Flags]
    internal enum WindowStyle : uint
    {
        Caption = 0x00C00000, // WS_CAPTION (WS_BORDER | WS_DLGFRAME)
        Child = 0x40000000, // WS_CHILD
        Popup = 0x80000000, // WS_POPUP
    }

    /// <summary>Extended window styles.</summary>
    [Flags]
    internal enum ExtendedWindowStyle : uint
    {
        Topmost = 0x00000008, // WS_EX_TOPMOST
        Transparent = 0x00000020, // WS_EX_TRANSPARENT
        ToolWindow = 0x00000080, // WS_EX_TOOLWINDOW
        WindowEdge = 0x00000100, // WS_EX_WINDOWEDGE
        Layered = 0x00080000, // WS_EX_LAYERED
        NoActivate = 0x08000000, // WS_EX_NO_ACTIVATE
    }

    /// <summary>Window messages.</summary>
    internal enum WindowMessage : uint
    {
        Destroy = 0x0002, // WM_DESTROY
        Paint = 0x000F, // WM_PAINT
        Close = 0x0010, // WM_CLOSE
    }

    /// <summary>ShowWindow commands.</summary>
    internal enum ShowWindowCommand
    {
        Hide = 0, // SW_HIDE
        ShowNoActivate = 8, // SW_SHOWNOACTIVATE
        Restore = 9, // SW_RESTORE
    }

    /// <summary>SetWindowPos flags.<summary>
    internal enum SetWindowPosFlags : uint
    {
        NoSize = 0x0001, // SWP_NOSIZE
        NoMove = 0x0002, // SWP_NOMOVE
        NoZOrder = 0x0004, // SWP_NOZORDER
        NoActivate = 0x0010, // SWP_NOACTIVATE
        FrameChanged = 0x0020, // SWP_FRAMECHANGED
        NoCopyBits = 0x0100, // SWP_NOCOPYBITS
        NoOwnerZOrder = 0x0200, // SWP_NOOWNERZORDER
        NoSendChanging = 0x0400, // SWP_NOSENDCHANGING
        Tile = NoZOrder | NoActivate | NoOwnerZOrder | FrameChanged | NoCopyBits | NoSendChanging,
    }

    /// <summary>DWM window attributes.</summary>
    internal enum DwmWindowAttribute : uint
    {
        ExtendedFrameBounds = 9, // DWMWA_EXTENDED_FRAME_BOUNDS
        Cloaked = 14, // DWMWA_CLOAKED
    }
}
