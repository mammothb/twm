using Twm.Application.OutboundPorts;

namespace Twm.Application.Config;

/// <summary>
/// What a matching <see cref="WindowRule" /> does to a window.
/// </summary>
public enum WindowRuleAction
{
    /// <summary>
    /// Never tile it (overrides the built-in "manageable" defaults).
    /// </summary>
    Ignore,

    /// <summary>
    /// Always tile it (rescues a window the built-in defaults would drop).
    /// </summary>
    Manage,
}

public sealed record WindowRule(string? ClassName, string? TitleSubstring, WindowRuleAction Action)
{
    public bool Matches(NativeWindowInfo window)
    {
        if (
            ClassName is not null
            && !string.Equals(window.ClassName, ClassName, StringComparison.Ordinal)
        )
        {
            return false;
        }

        if (
            TitleSubstring is not null
            && (
                window.Title is null
                || !window.Title.Contains(TitleSubstring, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            return false;
        }

        // At least one criterion is guaranteed set by Compile, and all set
        // criteria matched
        return true;
    }
}
