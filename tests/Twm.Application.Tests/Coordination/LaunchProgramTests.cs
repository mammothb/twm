using Twm.Application.Coordination;
using Twm.Application.OutboundPorts;
using Twm.Domain.Geometry;
using Twm.TestSupport.Fakes;

namespace Twm.Application.Tests.Coordination;

/// <summary>
/// Verifies <see cref="WmSession.LaunchProgram" /> is a thin pass-through to
/// the injected <see cref="IProcessLauncher" />: forwards the command line,
/// propagates success and failure verbatim, never throws, and falls back to
/// <see cref="NoOpProcessLauncher" /> when no real launcher is supplied. Uses
/// a recording fake so no real process is forked.
/// </summary>
public class LaunchProgramTests
{
    private static MonitorInfo Primary =>
        new(
            new MonitorId(1),
            new Rect(0, 0, 1920, 1080),
            new Rect(0, 0, 1920, 1080),
            IsPrimary: true
        );

    [Fact]
    public void LaunchProgram_ForwardsCommandLineToLauncher()
    {
        RecordingProcessLauncher launcher = new();
        WmSession session = new(
            new FakeMonitorSystem(Primary),
            new FakeWindowSystem(),
            processLauncher: launcher
        );

        session.LaunchProgram("wt.exe --new-tab");

        launcher.LastCommandLine.ShouldBe("wt.exe --new-tab");
    }

    [Fact]
    public void LaunchProgram_PropagatesSuccess()
    {
        RecordingProcessLauncher launcher = new()
        {
            NextResult = new ProcessLaunchResult(1234, null),
        };
        WmSession session = new(
            new FakeMonitorSystem(Primary),
            new FakeWindowSystem(),
            processLauncher: launcher
        );

        ProcessLaunchResult result = session.LaunchProgram("wt.exe");

        result.Ok.ShouldBeTrue();
        result.Pid.ShouldBe(1234);
        result.Error.ShouldBeNull();
    }

    [Fact]
    public void LaunchProgram_PropagatesFailureWithoutThrowing()
    {
        RecordingProcessLauncher launcher = new()
        {
            NextResult = new ProcessLaunchResult(0, "boom"),
        };
        WmSession session = new(
            new FakeMonitorSystem(Primary),
            new FakeWindowSystem(),
            processLauncher: launcher
        );

        // Should not throw
        ProcessLaunchResult result = session.LaunchProgram("does-not-exist.exe");

        result.Failed.ShouldBeTrue();
        result.Pid.ShouldBe(0);
        result.Error.ShouldBe("boom");
    }

    [Fact]
    public void LaunchProgram_NoLauncherConfigured_ReturnsFailure()
    {
        // Construct WmSession WITHOUT specifying a launcher — exercises the
        // null-default path that falls back to NoOpProcessLauncher
        WmSession session = new(new FakeMonitorSystem(Primary), new FakeWindowSystem());

        ProcessLaunchResult result = session.LaunchProgram("wt.exe");

        result.Failed.ShouldBeTrue();
        result.Pid.ShouldBe(0);
        result.Error.ShouldBe(NoOpProcessLauncher.Message);
    }

    /// <summary>
    /// Records the most recent command line and returns a configurable result.
    /// </summary>
    private sealed class RecordingProcessLauncher : IProcessLauncher
    {
        public string? LastCommandLine { get; private set; }

        public ProcessLaunchResult NextResult { get; set; }

        public ProcessLaunchResult Launch(string commandLine)
        {
            LastCommandLine = commandLine;
            return NextResult;
        }
    }
}
