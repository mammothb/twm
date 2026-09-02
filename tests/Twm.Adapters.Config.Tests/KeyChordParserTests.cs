using Twm.Application.InboundPorts;

namespace Twm.Adapters.Config.Tests;

public class KeyChordParserTests
{
    [Theory]
    [InlineData("$mod+h", ModifierKeys.Alt, 'H')]
    [InlineData("$mod+shift+h", ModifierKeys.Alt | ModifierKeys.Shift, 'H')]
    [InlineData("$mod+ctrl+l", ModifierKeys.Alt | ModifierKeys.Control, 'L')]
    [InlineData("$mod+1", ModifierKeys.Alt, '1')]
    [InlineData("ctrl+shift+f", ModifierKeys.Control | ModifierKeys.Alt, 'F')]
    public void TryParse_ValidChord_WithAltMod(string chord, ModifierKeys mods, char key)
    {
        bool ok = KeyChordParser.TryParse(chord, ModifierKeys.Alt, out KeyBinding binding, out _);

        ok.ShouldBeTrue();
        binding.ShouldBe(new KeyBinding(mods, key));
    }

    [Theory]
    [InlineData(ModifierKeys.Alt)]
    [InlineData(ModifierKeys.Windows)]
    public void TryParse_DollarMod_ResolvesToConfiguredModifier(ModifierKeys mods)
    {
        bool ok = KeyChordParser.TryParse("$mod+h", mods, out KeyBinding binding, out _);

        ok.ShouldBeTrue();
        binding.ShouldBe(new KeyBinding(mods, 'H'));
    }

    [Theory]
    [InlineData("$mod+backslash", ModifierKeys.Alt, 0xDCu)]
    [InlineData("$mod+minus", ModifierKeys.Alt, 0xBDu)]
    [InlineData("$mod+shift+f1", ModifierKeys.Alt | ModifierKeys.Shift, 0x70u)]
    [InlineData("$mod+f12", ModifierKeys.Alt, 0x7Bu)]
    [InlineData("$mod+left", ModifierKeys.Alt, 0x25u)]
    [InlineData("$mod+space", ModifierKeys.Alt, 0x20u)]
    [InlineData("$mod+BACKSLASH", ModifierKeys.Alt, 0xDCu)]
    public void TryParse_NamedKey_MapsToVirtualKey(string chord, ModifierKeys mods, uint vk)
    {
        bool ok = KeyChordParser.TryParse(chord, mods, out KeyBinding binding, out _);

        ok.ShouldBeTrue();
        binding.ShouldBe(new KeyBinding(mods, vk));
    }

    [Theory]
    [InlineData("")]
    [InlineData("$mod+hyper+h")]
    [InlineData("$mod+notakey")]
    [InlineData("$mod+")]
    [InlineData("$mod++")]
    public void TryParse_Invalid_ReturnsFalseWithError(string chord)
    {
        bool ok = KeyChordParser.TryParse(chord, ModifierKeys.Alt, out _, out string? error);

        ok.ShouldBeFalse();
        error.ShouldNotBeNullOrEmpty();
    }
}
