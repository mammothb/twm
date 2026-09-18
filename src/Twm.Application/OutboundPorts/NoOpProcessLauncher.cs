namespace Twm.Application.OutboundPorts;

/// <summary>
/// Default <see cref="IProcessLauncher" /> used when no real launcher is
/// injected (e.g., in unit tests that don't care about launching). Always
/// returns a failure with a clearly-recognizable message so callers see
/// immediately when they've forgotten to wire a real launcher. Never
/// throws.
/// </summary>
public sealed class NoOpProcessLauncher : IProcessLauncher
{
    /// <summary>The error message returned for every launch attempt.</summary>
    public const string Message = "launcher not configured";

    /// <inheritdoc />
    public ProcessLaunchResult Launch(string commandLine) => new(0, Message);
}
