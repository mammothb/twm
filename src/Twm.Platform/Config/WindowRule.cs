namespace Twm.Platform.Config;

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

    /// <summary>
    /// Compiles config DTOs into rules, validating the action and that each
    /// rule has at least one match criterion. Returns the valid rules plus an
    /// error per rejected DTO (never throws).
    /// </summary>
    public static (IReadOnlyList<WindowRule> Rules, IReadOnlyList<string> Errors) Compile(
        IEnumerable<WindowRuleDto>? dtos
    )
    {
        List<WindowRule> rules = [];
        List<string> errors = [];
        if (dtos is null)
        {
            return (rules, errors);
        }

        int index = 0;
        foreach (WindowRuleDto dto in dtos)
        {
            index++;
            string? className = string.IsNullOrWhiteSpace(dto.Class) ? null : dto.Class;
            string? title = string.IsNullOrWhiteSpace(dto.Title) ? null : dto.Title;

            if (className is null && title is null)
            {
                errors.Add($"windowRule #{index}: needs a 'class' or 'title'");
                continue;
            }

            if (!TryParseAction(dto.Action, out WindowRuleAction action))
            {
                errors.Add(
                    $"windowRule #{index}: invalid action '{dto.Action}' (expected ignore or manage)"
                );
                continue;
            }

            rules.Add(new WindowRule(className, title, action));
        }

        return (rules, errors);
    }

    private static bool TryParseAction(string? action, out WindowRuleAction result)
    {
        switch ((action ?? "").Trim().ToLowerInvariant())
        {
            case "ignore":
                result = WindowRuleAction.Ignore;
                return true;
            case "manage":
                result = WindowRuleAction.Manage;
                return true;
            default:
                result = WindowRuleAction.Ignore;
                return false;
        }
    }
}
