using System.Runtime.InteropServices;
using Twm.Domain.Geometry;
using static Twm.Adapters.Windows.NativeMethods;

namespace Twm.Adapters.Windows;

/// <summary>
/// A thin colored border drawn around the focused window, using raw GDI.
/// </summary>
public sealed unsafe partial class BorderWindow : IDisposable
{
    private const string ClassName = "TwmBorder";

    private const int RgnDiff = 4; // CombineRgn mode: hrgnSrc1 minus hrgnSrc2
    private const uint DefaultColor = 0x0000FF00;

    private static readonly Dictionary<nint, uint> s_colors = [];

    private static bool s_classRegistered;

    private readonly int _width;
    private readonly uint _color;
    private nint _hWnd;

    public BorderWindow(uint color, int width)
    {
        _color = color;
        _width = Math.Max(1, width);
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

        s_colors[_hWnd] = _color;
    }

    /// <summary>
    /// Positions the border so its band traces the given frame (the focused
    /// window's visible frame). The band is drawn just inside the frame edges
    /// (<c>_width</c> px), so it never overlaps a neighboring tile.
    /// </summary>
    public void MoveTo(Rect frame)
    {
        if (_hWnd == 0)
        {
            return;
        }

        MoveWindow(_hWnd, frame.X, frame.Y, frame.Width, frame.Height, bRepaint: false);
        ApplyBandRegion(frame.Width, frame.Height);
        ShowWindow(_hWnd, ShowWindowCommand.ShowNoActivate);
        InvalidateRect(_hWnd, 0, bErase: false);
    }

    public void Hide()
    {
        if (_hWnd != 0)
        {
            ShowWindow(_hWnd, ShowWindowCommand.Hide);
        }
    }

    public void Dispose()
    {
        if (_hWnd != 0)
        {
            DestroyWindow(_hWnd); // WM_DESTROY drops the color entry
            _hWnd = 0;
        }
    }

    /// <summary>Unregisters the shared window class (clean teardown).</summary>
    public static void UnregisterSharedClass()
    {
        if (!s_classRegistered)
        {
            return;
        }

        fixed (char* cls = ClassName)
        {
            if (UnregisterClassW(cls, GetModuleHandleW(null)))
            {
                s_classRegistered = false;
            }
        }
    }

    // Clips the window to a hollow band: the full client rect minus an inset
    // rect. The system takes ownership of the region passed to SetWindowRgn
    // (and frees the previous one), so only the temporary inner region is
    // deleted here.
    private void ApplyBandRegion(int width, int height)
    {
        int inset = Math.Min(_width, Math.Min(width, height) / 2);
        nint outer = CreateRectRgn(0, 0, width, height);
        nint inner = CreateRectRgn(inset, inset, width - inset, height - inset);
        CombineRgn(outer, outer, inner, RgnDiff);
        DeleteObject(inner);
        SetWindowRgn(_hWnd, outer, bRedraw: true);
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
                s_colors.Remove(hWnd);
                return 0;
            default:
                return DefWindowProcW(hWnd, uMsg, wParam, lParam);
        }
    }

    private static void Paint(nint hWnd)
    {
        nint hdc = BeginPaint(hWnd, out PaintStruct ps);
        GetClientRect(hWnd, out Rect32 client);

        uint color = s_colors.TryGetValue(hWnd, out uint c) ? c : DefaultColor;
        nint brush = CreateSolidBrush(color);
        // The window region already clips painting to the band, so filling the
        // whole client paints only the border
        FillRect(hdc, in client, brush);
        DeleteObject(brush);

        EndPaint(hWnd, in ps);
    }
}
