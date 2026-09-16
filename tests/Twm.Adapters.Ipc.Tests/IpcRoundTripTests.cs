using System.Threading;
using Twm.Shared.Ipc;

namespace Twm.Adapters.Ipc.Tests;

public sealed class IpcRoundTripTests
{
    public static bool IsWindows => OperatingSystem.IsWindows();

    [Fact]
    public void Send_RoundTripsThroughTheDispatcher()
    {
        string pipeName = UniquePipeName();
        using var server = new IpcServer(request => $"echo:{request}", pipeName);
        server.Start();

        string response = IpcClient.Send("focus left", pipeName);

        response.ShouldBe("echo:focus left");
    }

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void Send_MultipleRequests_EachHandledInOrder()
    {
        string pipeName = UniquePipeName();
        int count = 0;
        using var server = new IpcServer(
            request => $"{Interlocked.Increment(ref count)}:{request}",
            pipeName
        );
        server.Start();

        string first = IpcClient.Send("get-tree", pipeName);
        string second = IpcClient.Send("focus left", pipeName);

        first.ShouldBe("1:get-tree");
        second.ShouldBe("2:focus left");
    }

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void Send_LargeResponse_ClientReceivesFullPayload()
    {
        // Response larger than the named-pipe buffer (~4KB on Windows) so
        // WriteLineAsync + WaitForPipeDrain has to span multiple writes for
        // the client to see the whole thing.
        string large = new string('a', 16 * 1024);
        string pipeName = UniquePipeName();
        using var server = new IpcServer(_ => large, pipeName);
        server.Start();

        string response = IpcClient.Send("any", pipeName);

        response.ShouldBe(large);
    }

    [Fact]
    public void Send_ThrowingDispatcher_ReturnsErrResponseNotCrash()
    {
        string pipeName = UniquePipeName();
        using var server = new IpcServer(
            _ => throw new InvalidOperationException("error"),
            pipeName
        );
        server.Start();

        string response = IpcClient.Send("focus left", pipeName);

        response.ShouldBe("err error");
    }

    [Fact]
    public void Send_OversizedRequest_ReturnsErrNotCrash()
    {
        string pipeName = UniquePipeName();
        using var server = new IpcServer(_ => "ok", pipeName);
        server.Start();

        string response = IpcClient.Send(new string('x', 20000), pipeName);

        response.ShouldStartWith("err");
    }

    [Fact]
    public void Send_NoServerListening_ThrowTimeoutQuickly()
    {
        Should.Throw<TimeoutException>(() =>
            IpcClient.Send("focus left", UniquePipeName(), connectTimeoutMs: 300)
        );
    }

    [Fact]
    public void Start_CalledTwice_Throws()
    {
        using var server = new IpcServer(_ => "ok", UniquePipeName());
        server.Start();

        Should.Throw<InvalidOperationException>(() => server.Start());
    }

    private static string UniquePipeName() => "twm-test-" + Guid.NewGuid().ToString("N");
}
