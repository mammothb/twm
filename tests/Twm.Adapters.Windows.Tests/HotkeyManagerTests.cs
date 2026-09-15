using Twm.Application.InboundPorts;

namespace Twm.Adapters.Windows.Tests;

/// <summary>
/// End-to-end tests for <see cref="HotkeyManager" />: <c>RegisterHotKey</c>
/// against the OS, then <c>TryResolve</c> on a posted <c>WM_HOTKEY</c>.
/// </summary>
public sealed class HotkeyManagerTests : IDisposable
{
    private const uint VkF12 = 0x7B; // VK_F12

    // WM_HOTKEY. Exposed via HotkeyManager.HotkeyMessage (private); duplicated
    // here so the test doesn't depend on a private constant.
    private const uint WmHotkey = 0x0312;

    private readonly HotkeyManager _hm = new();

    public static bool IsWindows => OperatingSystem.IsWindows();

    public void Dispose() => _hm.UnregisterAll();

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void Register_ThenTryResolve_HotkeyMessage_ReturnsBinding()
    {
        var binding = new KeyBinding(ModifierKeys.Alt, VkF12);
        _hm.Register(binding).ShouldBeTrue();

        // A fresh HotkeyManager assigns ID 1 to the first registration
        // (private _nextId starts at 1).
        _hm.TryResolve(WmHotkey, wParam: 1, out KeyBinding resolved).ShouldBeTrue();
        resolved.ShouldBe(binding);
    }

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void Register_DuplicateHotkey_ReturnsFalse()
    {
        var binding = new KeyBinding(ModifierKeys.Alt, VkF12);
        _hm.Register(binding).ShouldBeTrue();

        // OS-detected conflict: second register of the same (mods, vk) returns
        // false even with a different internal id.
        _hm.Register(binding).ShouldBeFalse();
    }

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void UnregisterAll_AfterRegister_ClearsAllMappings()
    {
        var binding = new KeyBinding(ModifierKeys.Alt, VkF12);
        _hm.Register(binding).ShouldBeTrue();
        _hm.TryResolve(WmHotkey, wParam: 1, out _).ShouldBeTrue();

        _hm.UnregisterAll();

        // Mapping gone; wParam 1 no longer resolves
        _hm.TryResolve(WmHotkey, wParam: 1, out _).ShouldBeFalse();
    }
}
