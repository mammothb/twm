namespace Twm.Application.OutboundPorts;

/// <summary>
/// Outcome of an <see cref="IProcessLauncher.Launch" /> call. <see cref="Pid" />
/// is the spawned process id (or <c>0</c> on failure); <see cref="Error" /> is
/// <c>null</c> on success and a human-readable message on failure. The
/// two states are mutually exclusive and the <see cref="Ok" /> /
/// <see cref="Failed" /> helpers enforce the convention at call sites.
/// </summary>
public readonly record struct ProcessLaunchResult(int Pid, string? Error)
{
    /// <summary>True when the child was spawned cleanly (<see cref="Error" /> is null).</summary>
    public bool Ok => Error is null;

    /// <summary>True when spawning failed (<see cref="Error" /> carries the reason).</summary>
    public bool Failed => Error is not null;
}
