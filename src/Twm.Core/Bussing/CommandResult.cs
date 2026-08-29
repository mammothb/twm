namespace Twm.Core.Bussing;

/// <summary>The outcome of invoking a command.</summary>
public readonly record struct CommandResult(bool Success, string? Error = null)
{
    /// <summary>A successful result.</summary>
    public static CommandResult Ok => new(true);

    /// <summary>A failed result carrying an error message.</summary>
    public static CommandResult Fail(string error)
    {
        return new CommandResult(false, error);
    }
}
