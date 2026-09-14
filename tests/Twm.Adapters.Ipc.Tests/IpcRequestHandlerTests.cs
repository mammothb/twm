using Twm.Application.Coordination;
using Twm.Application.OutboundPorts;
using Twm.Domain.Geometry;
using Twm.Domain.Tree;
using Twm.TestSupport.Fakes;

namespace Twm.Adapters.Ipc.Tests;

public sealed class IpcRequestHandlerTests
{
    private static MonitorInfo Primary =>
        new(
            new MonitorId(1),
            new Rect(0, 0, 1920, 1080),
            new Rect(0, 0, 1920, 1080),
            IsPrimary: true
        );

    [Fact]
    public void GetTree_ReturnsTreeJson()
    {
        var handler = new IpcRequestHandler(StartedSession(Win(1)), () => { });

        handler.Handle("get-tree").ShouldStartWith("{\"kind\":\"root\"");
    }

    [Fact]
    public void GetTree_WithTitleResolver_PopulatesTitleOnWindowNodes()
    {
        var handler = new IpcRequestHandler(
            StartedSession(Win(1)),
            () => { },
            titleGetter: id => $"win{id.Value}"
        );

        string json = handler.Handle("get-tree");

        json.ShouldContain("\"title\":\"win1\"");
    }

    [Fact]
    public void Exit_InvokesCallback_AndReturnsOk()
    {
        bool exited = false;
        var handler = new IpcRequestHandler(StartedSession(Win(1)), () => exited = true);

        handler.Handle("exit").ShouldBe("ok");
        exited.ShouldBeTrue();
    }

    [Fact]
    public void Close_ReturnsOk()
    {
        var handler = new IpcRequestHandler(StartedSession(Win(1)), () => { });

        handler.Handle("close").ShouldBe("ok");
    }

    [Fact]
    public void ReconcileDisplays_CallsSessionReconcileAndReturnsOk()
    {
        var handler = new IpcRequestHandler(StartedSession(Win(1)), () => { });

        handler.Handle("reconcile-displays").ShouldBe("ok");
    }

    [Fact]
    public void RunCommand_Success_ReturnsOk()
    {
        var handler = new IpcRequestHandler(StartedSession(Win(1)), () => { });

        handler.Handle("workspace 1").ShouldBe("ok");
    }

    [Fact]
    public void RunCommand_Failure_ReturnsErr()
    {
        var handler = new IpcRequestHandler(StartedSession(Win(1)), () => { });

        handler.Handle("workspace 99").ShouldStartWith("err");
    }

    [Fact]
    public void UnknownCommand_ReturnsErr()
    {
        var handler = new IpcRequestHandler(StartedSession(Win(1)), () => { });

        handler.Handle("badcommand").ShouldStartWith("err");
    }

    [Fact]
    public void EmptyRequest_ReturnsErr()
    {
        var handler = new IpcRequestHandler(StartedSession(Win(1)), () => { });

        string response = handler.Handle("");

        response.ShouldStartWith("err");
        response.ShouldContain("empty command");
    }

    private static NativeWindowInfo Win(int id) =>
        new(
            new WindowId(id),
            "App",
            "Notepad",
            new Rect(100, 100, 800, 600),
            IsVisible: true,
            IsCloaked: false,
            IsToolWindow: false,
            IsMinimized: false
        );

    private static WmSession StartedSession(params NativeWindowInfo[] windows)
    {
        var session = new WmSession(new FakeMonitorSystem(Primary), new FakeWindowSystem(windows));
        session.Start();
        return session;
    }
}
