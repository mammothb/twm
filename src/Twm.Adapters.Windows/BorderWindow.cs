using System.Runtime.InteropServices;
using Twm.Domain.Geometry;
using static Twm.Adapters.Windows.NativeMethods;

namespace Twm.Adapters.Windows;

/// <summary>
/// A thin colored border drawn around the focused window, using
/// <c>WS_EX_LAYERED</c>.
///
/// <para>
///
/// The window is composited by DWM from a 32bpp premultiplied-BGRA bitmap: the
/// border band is opaque, the interior is fully transparent. Because the
/// interior is transparent.
/// </summary>
public sealed unsafe partial class BorderWindow : IDisposable
{
    private const string ClassName = "TwmBorder";

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
                    | ExtendedWindowStyle.Transparent
                    | ExtendedWindowStyle.Layered,
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

    /// <summary>
    /// Positions the border so its band traces the given frame (the focused
    /// window's visible frame). The band is drawn just inside the frame edges
    /// (<c>_width</c> px), so it never overlaps a neighboring tile.
    /// </summary>
    public void MoveTo(Rect frame)
    {
        if (_hWnd == 0 || frame.Width <= 0 || frame.Height <= 0)
        {
            return;
        }

        Render(frame.X, frame.Y, frame.Width, frame.Height);
        ShowWindow(_hWnd, ShowWindowCommand.ShowNoActivate);
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
            DestroyWindow(_hWnd);
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

    // Builds a 32bpp premultipled-BGRA bitmap
    private void Render(int x, int y, int width, int height)
    {
        int band = Math.Min(_width, Math.Min(width, height) / 2);
        if (band <= 0)
        {
            return;
        }

        BitmapInfoHeader header = default;
        header.Size = (uint)sizeof(BitmapInfoHeader);
        header.Width = width;
        header.Height = -height;
        header.Planes = 1;
        header.BitCount = 32;
        header.Compression = BiRgb;

        nint screenDc = GetDC(0);
        nint memDc = CreateCompatibleDC(screenDc);
        nint dib = CreateDIBSection(screenDc, in header, DibRgbColors, out nint bits, 0, 0);
        if (dib == 0 || bits == 0)
        {
            if (dib != 0)
            {
                DeleteObject(dib);
            }
            DeleteDC(memDc);
            _ = ReleaseDC(0, screenDc);
            return;
        }

        // Premultiplied BGRA. Alpha 255 -> RGB unchanged; the COLORREF is
        // 0x00BBGGRR.
        byte b = (byte)((_color >> 16) & 0xFF);
        byte g = (byte)((_color >> 8) & 0xFF);
        byte r = (byte)(_color & 0xFF);
        byte* px = (byte*)bits;
        for (int i = 0; i < height; i++)
        {
            bool isEdgeRow = i < band || i >= height - band;
            byte* line = px + ((nint)i * width * 4);
            for (int j = 0; j < width; j++)
            {
                if (isEdgeRow || j < band || j >= width - band)
                {
                    line[0] = b;
                    line[1] = g;
                    line[2] = r;
                    line[3] = 255;
                }
                line += 4;
            }
        }

        nint oldBitmap = SelectObject(memDc, dib);

        var src = new Point32 { X = 0, Y = 0 };
        var dst = new Point32 { X = x, Y = y };
        var size = new Size32 { Cx = width, Cy = height };
        var blend = new BlendFunction
        {
            BlendOp = AcSrcOver,
            BlendFlags = 0,
            SourceConstantAlpha = 255,
            AlphaFormat = AcSrcAlpha,
        };

        UpdateLayeredWindow(_hWnd, screenDc, in dst, in size, memDc, in src, 0, in blend, UlwAlpha);

        SelectObject(memDc, oldBitmap);
        DeleteObject(dib);
        DeleteDC(memDc);
        _ = ReleaseDC(0, screenDc);
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
    private static nint WndProc(nint hWnd, uint uMsg, nint wParam, nint lParam) =>
        DefWindowProcW(hWnd, uMsg, wParam, lParam);
}
