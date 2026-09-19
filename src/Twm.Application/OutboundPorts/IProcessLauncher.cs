namespace Twm.Application.OutboundPorts;

/// <summary>
/// Spawns a child process on behalf of the WM. The single
/// <see cref="Launch" /> method hands the caller-supplied command line to
/// the platform's default shell and returns a
/// <see cref="ProcessLaunchResult" /> describing the outcome. Implementations
/// MUST NOT throw — file-not-found, permission denied, etc., are normal
/// outcomes surfaced through the result, not exceptions. Implemented by
/// the Win32 adapter in production and by a recording fake in tests, so
/// the whole launch path is verifiable without forking.
/// </summary>
public interface IProcessLauncher
{
    /// <summary>
    /// Runs <paramref name="commandLine" /> through the platform shell
    /// (Windows: <c>cmd.exe /c</c>). Returns the spawned process id on
    /// success, or <c>0</c> plus a human-readable error message on
    /// failure.
    /// </summary>
    ProcessLaunchResult Launch(string commandLine);
}
