using System.Runtime.InteropServices;

namespace Twm.Interop;

/// <summary>
/// Global low-level keyboard hook. Calls back with (modifiers, virtual key)
/// for non-injected key-downs; when the handler returns true the key event
/// is swallowed so Windows and other apps never see it.
/// </summary>
public sealed class KeyboardHook : IDisposable
{
    public const int WhKeyboardLl = 13;
    private const int LlkhfInjected = 0x10;

    public delegate bool ComboHandler(Modifiers mods, uint vKey);

    private readonly User32.LowLevelKeyboardProc _proc;
    private readonly ComboHandler _onCombo;
    private nint _handle;

    public KeyboardHook(ComboHandler onCombo)
    {
        _onCombo = onCombo;
        _proc = HookProc;
        _handle = User32.SetWindowsHookEx(WhKeyboardLl, _proc, 0, 0);
        if (_handle == nint.Zero)
            throw new InvalidOperationException(
                $"SetWindowsHookEx(WH_KEYBOARD_LL) failed (win32 error {System.Runtime.InteropServices.Marshal.GetLastWin32Error()})."
            );
    }

    private nint HookProc(int code, nint wParam, nint lParam)
    {
        if (code >= 0 && (nuint)wParam is User32.WM_KEYDOWN or User32.WM_SYSKEYDOWN)
        {
            var info = Marshal.PtrToStructure<Interop.KbdDllHookStruct>(lParam);
            if ((info.Flags & LlkhfInjected) == 0)
            {
                var mods = ReadModifiers();
                if (_onCombo(mods, info.VirtualKey))
                    return 1; // matched a binding: swallow
            }
        }
        return User32.CallNextHookEx(_handle, code, wParam, lParam);
    }

    private static Modifiers ReadModifiers()
    {
        var mods = Modifiers.None;
        if (Pressed(User32.VK_MENU))
            mods |= Modifiers.Alt;
        if (Pressed(User32.VK_SHIFT))
            mods |= Modifiers.Shift;
        if (Pressed(User32.VK_CONTROL))
            mods |= Modifiers.Ctrl;
        if (Pressed(User32.VK_LWIN) || Pressed(User32.VK_RWIN))
            mods |= Modifiers.Win;
        return mods;
    }

    private static bool Pressed(int vk) => (User32.GetAsyncKeyState(vk) & 0x8000) != 0;

    public void Dispose()
    {
        if (_handle != nint.Zero)
        {
            User32.UnhookWindowsHookEx(_handle);
            _handle = nint.Zero;
        }
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct KbdDllHookStruct
{
    public uint VirtualKey;
    public uint ScanCode;
    public uint Flags;
    public uint Time;
    public nint ExtraInfo;
}
