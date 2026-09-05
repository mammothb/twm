using System.Runtime.InteropServices;
using Twm.Domain.Tree;
using Twm.Presentation;
using static Twm.Adapters.Windows.NativeMethods;

namespace Twm.Adapters.Windows;

/// <summary>
/// A single tabbed/stacked container's bar, drawn with raw GDI.
///
/// Tabbed -> a horizontal row of equal cells across the container's top strip.
/// Stacked -> a vertical list of title-bar rows. The focused entry is drawn
/// with the accent background.
/// </summary>
public sealed unsafe partial class TabBarWindow : IDisposable
{
    private const string ClassName = "TwmTabBar";

    // DrawText flags: tabs centered, rows left-aligned; both single-line with
    // ellipsis
    private const DrawTextFormat DtTab =
        DrawTextFormat.Center
        | DrawTextFormat.VCenter
        | DrawTextFormat.SingleLine
        | DrawTextFormat.EndEllipsis
        | DrawTextFormat.NoPrefix;
    private const DrawTextFormat DtRow =
        DrawTextFormat.VCenter
        | DrawTextFormat.SingleLine
        | DrawTextFormat.EndEllipsis
        | DrawTextFormat.NoPrefix;
    private const int RowTextPad = 6;
    private const uint DefaultBackground = 0x00303030;

    private static readonly Dictionary<nint, TabBarRenderState> s_renderState = [];

    private static bool s_classRegistered;

    private readonly uint _background;
    private readonly uint _foreground;
    private readonly uint _accent;
    private readonly int _rowHeight;
    private nint _hWnd;

    public TabBarWindow(uint background, uint foreground, uint accent, int rowHeight)
    {
        _background = background;
        _foreground = foreground;
        _accent = accent;
        _rowHeight = Math.Max(1, rowHeight);
        EnsureClassRegistered();

        fixed (char* cls = ClassName)
        {
            _hWnd = CreateWindowExW(
                dwExStyle: ExtendedWindowStyle.ToolWindow
                    | ExtendedWindowStyle.Topmost
                    | ExtendedWindowStyle.NoActivate
                    | ExtendedWindowStyle.Transparent,
                lpClassName: cls,
                lpWindowName: null,
                dwStyle: WindowStyle.Popup,
                x: 0,
                y: 0,
                nWidth: 0,
                nHeight: 0,
                hWndParent: 0,
                hMenu: 0,
                hInstance: GetModuleHandleW(null),
                lpParam: 0
            );
        }
    }

    public void Render(TabBarView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (_hWnd == 0)
        {
            return;
        }

        int rowCount = view.Layout == Layout.Stacked ? Math.Max(1, view.Tabs.Count) : 1;
        int height = _rowHeight * rowCount;

        s_renderState[_hWnd] = new TabBarRenderState(
            view,
            _background,
            _foreground,
            _accent,
            _rowHeight
        );
        MoveWindow(_hWnd, view.Bounds.X, view.Bounds.Y, view.Bounds.Width, height, bRepaint: false);
        ShowWindow(_hWnd, ShowWindowCommand.ShowNoActivate);
        InvalidateRect(_hWnd, 0, bErase: false);
    }

    public void Dispose()
    {
        if (_hWnd != 0)
        {
            DestroyWindow(_hWnd); // WM_DESTORY drops the render state
            _hWnd = 0;
        }
    }

    /// <summary>
    /// Unregisteres the shared window class once every tab bar is gone.
    /// </summary>
    public static void UnregisterSharedClass()
    {
        if (!s_classRegistered)
        {
            return;
        }

        fixed (char* cls = ClassName)
        {
            UnregisterClassW(cls, GetModuleHandleW(null));
        }

        s_classRegistered = false;
    }

    private static void EnsureClassRegistered()
    {
        if (s_classRegistered)
        {
            return;
        }

        fixed (char* cls = ClassName)
        {
            WndClassExW wc = default;
            wc.CbSize = (uint)sizeof(WndClassExW);
            wc.WndProc = &WndProc;
            wc.Instance = GetModuleHandleW(null);
            wc.ClassName = cls;
            RegisterClassExW(in wc);
        }

        s_classRegistered = true;
    }

    [UnmanagedCallersOnly]
    private static nint WndProc(nint hWnd, uint uMsg, nint wParam, nint lParam)
    {
        switch ((WindowMessage)uMsg)
        {
            case WindowMessage.Paint:
                Paint(hWnd);
                return 0;
            case WindowMessage.Destroy:
                s_renderState.Remove(hWnd);
                return 0;
            default:
                return DefWindowProcW(hWnd, uMsg, wParam, lParam);
        }
    }

    private static void Paint(nint hWnd)
    {
        nint hdc = BeginPaint(hWnd, out PaintStruct ps);
        GetClientRect(hWnd, out Rect32 client);

        if (!s_renderState.TryGetValue(hWnd, out TabBarRenderState? state))
        {
            nint blank = CreateSolidBrush(DefaultBackground);
            FillRect(hdc, in client, blank);
            DeleteObject(blank);
            EndPaint(hWnd, in ps);
            return;
        }

        nint background = CreateSolidBrush(state.Background);
        FillRect(hdc, in client, background);
        DeleteObject(background);

        SelectObject(hdc, GetStockObject(DefaultGuiFont));
        SetBkMode(hdc, BkModeTransparent);

        if (state.View.Layout == Layout.Stacked)
        {
            PaintStacked(hdc, in client, state);
        }
        else
        {
            PaintTabbed(hdc, in client, state);
        }

        EndPaint(hWnd, in ps);
    }

    private static void PaintStacked(nint hdc, in Rect32 client, TabBarRenderState state)
    {
        nint accent = CreateSolidBrush(state.Accent);
        for (int i = 0; i < state.View.Tabs.Count; i++)
        {
            var row = new Rect32
            {
                Left = client.Left,
                Top = client.Top + (i * state.RowHeight),
                Right = client.Right,
                Bottom = client.Top + ((i + 1) * state.RowHeight),
            };
            TabItem tab = state.View.Tabs[i];
            if (tab.Focused)
            {
                FillRect(hdc, in row, accent);
            }

            SetTextColor(hdc, state.Foreground);
            fixed (char* text = tab.Title)
            {
                Rect32 textRect = row;
                textRect.Left = RowTextPad;
                DrawTextW(hdc, text, tab.Title.Length, ref textRect, DtRow);
            }
        }

        DeleteObject(accent);
    }

    private static void PaintTabbed(nint hdc, in Rect32 client, TabBarRenderState state)
    {
        int count = state.View.Tabs.Count;
        if (count == 0)
        {
            return;
        }

        int width = client.Right - client.Left;
        nint accent = CreateSolidBrush(state.Accent);
        for (int i = 0; i < count; i++)
        {
            int x = client.Left + (int)((long)width * i / count);
            int nextX = client.Left + (int)((long)width * (i + 1) / count);
            var cell = new Rect32
            {
                Left = x,
                Top = client.Top,
                Right = nextX,
                Bottom = client.Bottom,
            };
            TabItem tab = state.View.Tabs[i];
            if (tab.Focused)
            {
                FillRect(hdc, in cell, accent);
            }

            SetTextColor(hdc, state.Foreground);
            fixed (char* text = tab.Title)
            {
                Rect32 textRect = cell;
                textRect.Left = RowTextPad;
                DrawTextW(hdc, text, tab.Title.Length, ref textRect, DtRow);
            }
        }

        DeleteObject(accent);
    }

    private sealed record TabBarRenderState(
        TabBarView View,
        uint Background,
        uint Foreground,
        uint Accent,
        int RowHeight
    );
}
