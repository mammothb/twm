using System.Threading;
using Twm.Application.Coordination;

namespace Twm.Application.Tests.Coordination;

public sealed class WmThreadDispatcherTests
{
    [Fact]
    public async Task DispatchFromBackground_MarshalsRequestToDrainingThread()
    {
        bool woken = false;
        var dispatcher = new WmThreadDispatcher(
            () =>
            {
                woken = true;
                return true;
            },
            request => $"handled:{request}"
        );

        Task<string> submit = Task.Run(() => dispatcher.DispatchFromBackground("focus left"));
        SpinWait.SpinUntil(() => woken, TimeSpan.FromSeconds(2));
        dispatcher.Drain();

        (await submit).ShouldBe("handled:focus left");
    }

    [Fact]
    public void DispatchFromBackground_WhenWakeFails_ReturnsErrorWithoutHanging()
    {
        var dispatcher = new WmThreadDispatcher(() => false, request => "ok");

        string response = dispatcher.DispatchFromBackground("focus left");

        response.ShouldStartWith("err");
    }

    [Fact]
    public void DispatchFromBackground_WhenNeverDrained_TimesOut()
    {
        var dispatcher = new WmThreadDispatcher(
            () => false,
            request => "ok",
            TimeSpan.FromMilliseconds(150)
        );

        string response = dispatcher.DispatchFromBackground("focus left");

        response.ShouldStartWith("err");
    }

    [Fact]
    public async Task Drain_WhenHandlerThrow_BecomesErrResponse()
    {
        bool woken = false;
        var dispatcher = new WmThreadDispatcher(
            () =>
            {
                woken = true;
                return true;
            },
            request => throw new InvalidOperationException("fail")
        );

        Task<string> submit = Task.Run(() => dispatcher.DispatchFromBackground("x"));
        SpinWait.SpinUntil(() => woken, TimeSpan.FromSeconds(2));
        dispatcher.Drain();

        (await submit).ShouldBe("err fail");
    }
}
