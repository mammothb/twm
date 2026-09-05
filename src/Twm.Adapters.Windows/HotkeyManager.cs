using System.Runtime.InteropServices;
using Twm.Application.InboundPorts;

namespace Twm.Adapters.Windows;

/// <summary>
/// Registers a global hotkeys via <c>RegisterHotKey</c> with a null window, so
/// <c>WM_HOTKEY</c> arrives as a thread message on the pump, and resolves
/// incoming <c>WM_HOTKEY</c> ids back to their <see cref="KeyBinding" />.
/// </summary>
public sealed partial class HotkeyManager
{
    // MOD_NOREPEAT, suppress auto-repeat storms
    private const int ModNoRepeat = 0x4000;

    // WM_HOTKEY
    private const uint HotkeyMessage = 0x0312;

    private readonly Dictionary<int, KeyBinding> _idToBinding = [];
    private int _nextId = 1;

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnregisterHotKey(nint hWnd, int id);

    /// <summary>
    /// Registers a binding; returns whether the OS accepted it (a conflict
    /// returns false).
    /// </summary>
    public bool Register(KeyBinding binding)
    {
        int id = _nextId;
        uint modifiers = (uint)binding.Modifiers | ModNoRepeat;
        if (!RegisterHotKey(0, id, modifiers, binding.VirtualKey))
        {
            return false;
        }

        _idToBinding[id] = binding;
        _nextId++;
        return true;
    }

    /// <summary>
    /// If <paramref name="message" /> is a <c>WM_HOTKEY</c> we registered,
    /// yields its binding.
    /// </summary.
    public bool TryResolve(uint message, nint wParam, out KeyBinding binding)
    {
        binding = default;
        return message == HotkeyMessage && _idToBinding.TryGetValue((int)wParam, out binding);
    }

    public void UnregisterAll()
    {
        foreach (int id in _idToBinding.Keys)
        {
            UnregisterHotKey(0, id);
        }
        _idToBinding.Clear();
    }
}
