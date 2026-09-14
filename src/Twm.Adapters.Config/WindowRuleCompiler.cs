using Twm.Application.Config;

namespace Twm.Adapters.Config;

/// <summary>
/// Outcome of compiling window-rule DTOs: the valid rules plus one error per
/// rejected DTO.
/// </summary>
public sealed record WindowRuleCompileResult(
    IReadOnlyList<WindowRule> Rules,
    IReadOnlyList<string> Errors
);

public static class WindowRuleCompiler
{
    /// <summary>
    /// Compiles window-rule DTOs into <see cref="WindowRule" />s, validating
    /// the action and that each rule has at least one match criterion. Returns
    /// the valid rules plus an error per rejected DTO (never throws). This is
    /// the adapter half of the rule model: the neutral
    /// <see cref="WindowRule" /> lives in Application; turning the YAML DTO
    /// into it is the adapter's job.
    /// </summary>
    public static WindowRuleCompileResult Compile(IEnumerable<WindowRuleDto>? dtos)
    {
        List<WindowRule> rules = [];
        List<string> errors = [];
        if (dtos is null)
        {
            return new WindowRuleCompileResult(rules, errors);
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

        return new WindowRuleCompileResult(rules, errors);
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
