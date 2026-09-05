using System.Runtime.InteropServices;
using Twm.Application.Config;
using Twm.Domain.Geometry;
using Twm.Presentation;
using static Twm.Adapters.Windows.NativeMethods;

namespace Twm.Adapters.Windows;

/// <summary>
/// A single Win32 status-bar window painted with raw GDI. Self-contained
/// interop (like <see cref="WinEventHook" />): a shared window class with a
/// static <c>[UnmanagedCallersOnly]</c> WndProc + function-pointer
/// registration. The window is <c>WS_EX_TOOLWINDOW | WS_EX_TOPMOST |
/// WS_EX_NOACTIVATE</c>, so Twm's own <c>WindowFilter</c> ignores it (tool
/// window) and it never steals focus.
///
/// The static WndProc can't capture instance state, so each window's
/// <see cref="StatusBarRenderState" /> (its <see cref="MonitorBarView" />, clock, and
/// <see cref="BarOptions" /> theme) lives in a static map keyed by HWND;
/// everything runs on the single WM thread, so no locking is needed.
/// </summary>
public sealed unsafe partial class StatusBarWindow : IDisposable
{
    private const string ClassName = "TwmBar";

    // DrawText format flags for each drawn element
    private const DrawTextFormat DtChip =
        DrawTextFormat.Center | DrawTextFormat.VCenter | DrawTextFormat.SingleLine;
    private const DrawTextFormat DtClock =
        DrawTextFormat.Center | DrawTextFormat.VCenter | DrawTextFormat.SingleLine;
    private const DrawTextFormat DtTitle =
        DrawTextFormat.VCenter
        | DrawTextFormat.SingleLine
        | DrawTextFormat.EndEllipsis
        | DrawTextFormat.NoPrefix;

    // used before first snapshot arrives
    private const uint DefaultBackground = 0x00303030;

    private const int LeftPadding = 6;
    private const int RightPadding = 6;
    private const int ChipWidth = 26;
    private const int ChipGap = 2;
    private const int ClockWidth = 52;
    private const int TitleGap = 12;

    private static readonly Dictionary<nint, StatusBarRenderState> s_renderState = [];

    private static bool s_classRegistered;

    private readonly BarOptions _options;
    private nint _hwnd;

    public StatusBarWindow(Rect bounds, BarOptions options)
    {
        _options = options;
        EnsureClassRegistered();

        fixed (char* cls = ClassName)
        {
            _hwnd = CreateWindowExW(
                dwExStyle: ExtendedWindowStyle.ToolWindow
                    | ExtendedWindowStyle.Topmost
                    | ExtendedWindowStyle.NoActivate,
                lpClassName: cls,
                lpWindowName: null,
                dwStyle: WindowStyle.Popup,
                x: bounds.X,
                y: bounds.Y,
                nWidth: bounds.Width,
                nHeight: bounds.Height,
                hWndParent: 0,
                hMenu: 0,
                hInstance: GetModuleHandleW(null),
                lpParam: 0
            );
        }

        ShowWindow(_hwnd, ShowWindowCommand.ShowNoActivate);
    }

    /// <summary>Sets what this bar shows and requests a repaint. </summary>
    public void Render(MonitorBarView view, string clock)
    {
        s_renderState[_hwnd] = new StatusBarRenderState(view, clock, _options);
        InvalidateRect(_hwnd, 0, bErase: false);
    }

    public void Dispose()
    {
        if (_hwnd != 0)
        {
            DestroyWindow(_hwnd); // WM_DESTROY drops the render state
            _hwnd = 0;
        }
    }

    /// <summary>
    /// Unregisters the shared window class once every bar is gone (clean tear
    /// down).
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

        bool hasState = s_renderState.TryGetValue(hWnd, out StatusBarRenderState? state);
        nint background = CreateSolidBrush(
            hasState ? state!.Options.Background : DefaultBackground
        );
        FillRect(hdc, in client, background);
        DeleteObject(background);

        if (hasState)
        {
            SelectObject(hdc, GetStockObject(DefaultGuiFont));
            SetBkMode(hdc, BkModeTransparent);
            PaintContent(hdc, in client, state!);
        }

        EndPaint(hWnd, in ps);
    }

    private static void PaintContent(nint hdc, in Rect32 client, StatusBarRenderState state)
    {
        BarOptions options = state.Options;

        int x = LeftPadding;
        nint accent = CreateSolidBrush(options.ActiveBackground);
        foreach (WorkspaceItem workspace in state.View.Workspaces)
        {
            var chip = new Rect32
            {
                Left = x,
                Top = 0,
                Right = x + ChipWidth,
                Bottom = client.Bottom,
            };
            if (workspace.Active)
            {
                FillRect(hdc, in chip, accent);
            }

            // Active/occupied used the foreground; empty workspaces are dimmed.
            SetTextColor(
                hdc,
                workspace.Active || workspace.Occupied
                    ? options.Foreground
                    : Dim(options.Foreground)
            );
            fixed (char* name = workspace.Name)
            {
                Rect32 nameRect = chip;
                DrawTextW(hdc, name, workspace.Name.Length, ref nameRect, DtChip);
            }

            x += ChipWidth + ChipGap;
        }

        DeleteObject(accent);

        int clockReserve = 0;
        if (options.ShowClock)
        {
            clockReserve = ClockWidth + RightPadding;
            SetTextColor(hdc, options.Foreground);
            var clockRect = new Rect32
            {
                Left = client.Right - ClockWidth,
                Top = 0,
                Right = client.Right - RightPadding,
                Bottom = client.Bottom,
            };
            fixed (char* clock = state.Clock)
            {
                DrawTextW(hdc, clock, state.Clock.Length, ref clockRect, DtClock);
            }
        }

        if (options.ShowTitle && !string.IsNullOrEmpty(state.View.FocusedTitle))
        {
            var titleRect = new Rect32
            {
                Left = x + TitleGap,
                Top = 0,
                Right = client.Right - (clockReserve == 0 ? RightPadding : clockReserve),
                Bottom = client.Bottom,
            };
            SetTextColor(hdc, options.Foreground);
            fixed (char* title = state.View.FocusedTitle)
            {
                DrawTextW(hdc, title, state.View.FocusedTitle.Length, ref titleRect, DtTitle);
            }
        }
    }

    /// <summary>
    /// Halves each RGB channel of a COLORREF, for a dimmed (empty-workspace)
    /// foreground.
    /// </summary>
    private static uint Dim(uint color)
    {
        return (color >> 1) & 0x007F7F7F;
    }

    private sealed record StatusBarRenderState(
        MonitorBarView View,
        string Clock,
        BarOptions Options
    );
}
