using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Twm.Application.OutboundPorts;

namespace Twm.Adapters.Windows;

/// <summary>
/// Win32 implementation of <see cref="IProcessLauncher" />. Hands the
/// caller-supplied <c>commandLine</c> to <c>cmd.exe /c</c> via
/// <see cref="Process.Start" /> — the Windows analog of i3's
/// <c>/bin/sh -c &lt;line&gt;</c>. <c>cmd.exe</c> parses the rest as a normal
/// command line, so <c>&amp;&amp;</c>, <c>||</c>, <c>|</c>, <c>&gt;</c>,
/// env-var expansion, and the <c>start</c> builtin all work the way every
/// stack-overflow answer assumes. Never throws — file-not-found, permission
/// denied, etc., are surfaced through <see cref="ProcessLaunchResult.Error" />.
/// </summary>
public sealed class WindowsProcessLauncher : IProcessLauncher
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public ProcessLaunchResult Launch(string commandLine)
    {
        ArgumentNullException.ThrowIfNull(commandLine);

        var psi = new ProcessStartInfo
        {
            // The shell. User writes: `exec "notepad.exe"`; we pass
            // `cmd.exe /c notepad.exe` to the OS. cmd parses the rest.
            FileName = "cmd.exe",
            Arguments = "/c " + commandLine,

            // Don't go through ShellExecuteEx: we want argv-style behaviour
            // (no file-association magic on .lnk / .exe path lookups), and
            // we want the parent cmd console suppressed.
            UseShellExecute = false,

            // Suppress the parent cmd.exe console flash. GUI-subsystem
            // children don't show one anyway; console-spawning children
            // inherit the (suppressed) parent console.
            CreateNoWindow = true,

            // cmd's noise ("The syntax of the command is incorrect") is
            // not actionable for the binding caller — drop it. The user
            // already has a terminal if they need the detail.
            RedirectStandardError = false,
        };

        try
        {
            using Process? p = Process.Start(psi);
            if (p is null)
            {
                return new ProcessLaunchResult(0, "Process.Start returned null");
            }

            int pid = SafePid(p);
            return pid > 0
                ? new ProcessLaunchResult(SafePid(p), null)
                : new ProcessLaunchResult(0, "Process started by PID could not be retrieved");
        }
        catch (Exception ex)
        {
            // Win32Exception("The system cannot find the file specified."),
            // IOException, InvalidOperationException, etc.
            return new ProcessLaunchResult(0, ex.Message);
        }
    }

    private static int SafePid(Process p)
    {
        // Process.Id reads from the cached snapshot of the handle's process
        // id; it can throw Win32Exception if the child has already exited and
        // the handle has been reaped. Pin the pid while we have a handle.
        try
        {
            return p.Id;
        }
        catch
        {
            return 0;
        }
    }
}
