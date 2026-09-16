using Twm.Application.InboundPorts;

namespace Twm.Adapters.Windows.Tests;

/// <summary>
/// End-to-end tests for <see cref="HotkeyManager" />: <c>RegisterHotKey</c>
/// against the OS, then <c>TryResolve</c> on a posted <c>WM_HOTKEY</c>.
/// </summary>
public sealed class HotkeyManagerTests : IDisposable
{
    private const uint VkF12 = 0x7B; // VK_F12
    private const uint VkF11 = 0x7A; // VK_F11
    private const uint VkOem3 = 0xC0; // VK_OEM_3 (backtick)

    // WM_HOTKEY. Exposed via HotkeyManager.HotkeyMessage (private); duplicated
    // here so the test doesn't depend on a private constant.
    private const uint WmHotkey = 0x0312;

    // Pick the first chord from this list that the OS accepts. Other processes
    // (other test classes, background apps on the runner) may already own a
    // given chord; a small fallback list covers the common collision cases.
    // The chosen binding is shared across all tests so the duplicate-register
    // test exercises the same chord as the happy-path test.
    private static readonly KeyBinding TestBinding = PickUsableBinding();

    private readonly HotkeyManager _hm = new();

    public static bool IsWindows => OperatingSystem.IsWindows();

    public void Dispose() => _hm.UnregisterAll();

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void Register_ThenTryResolve_HotkeyMessage_ReturnsBinding()
    {
        _hm.Register(TestBinding).ShouldBeTrue();

        // A fresh HotkeyManager assigns ID 1 to the first registration
        // (private _nextId starts at 1).
        _hm.TryResolve(WmHotkey, wParam: 1, out KeyBinding resolved).ShouldBeTrue();
        resolved.ShouldBe(TestBinding);
    }

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void Register_DuplicateHotkey_ReturnsFalse()
    {
        _hm.Register(TestBinding).ShouldBeTrue();

        // OS-detected conflict: second register of the same (mods, vk) returns
        // false even with a different internal id.
        _hm.Register(TestBinding).ShouldBeFalse();
    }

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void UnregisterAll_AfterRegister_ClearsAllMappings()
    {
        _hm.Register(TestBinding).ShouldBeTrue();
        _hm.TryResolve(WmHotkey, wParam: 1, out _).ShouldBeTrue();

        _hm.UnregisterAll();

        // Mapping gone; wParam 1 no longer resolves
        _hm.TryResolve(WmHotkey, wParam: 1, out _).ShouldBeFalse();
    }

    private static KeyBinding PickUsableBinding()
    {
        foreach (
            KeyBinding candidate in new[]
            {
                new KeyBinding(ModifierKeys.Alt, VkF12),
                new KeyBinding(ModifierKeys.Alt, VkF11),
                new KeyBinding(ModifierKeys.Alt | ModifierKeys.Control, VkF12),
                new KeyBinding(ModifierKeys.Alt, VkOem3),
            }
        )
        {
            var probe = new HotkeyManager();
            if (probe.Register(candidate))
            {
                probe.UnregisterAll();
                return candidate;
            }
        }

        throw new InvalidOperationException(
            "no usable hotkey chord on this runner; Alt+F12/F11/Ctrl+Alt+F12/Alt+` all claimed"
        );
    }
}
