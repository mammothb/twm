using System.Threading;

namespace Twm.Adapters.Windows.Tests;

public sealed class MessageLoopTests
{
    public static bool IsWindows => OperatingSystem.IsWindows();

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void Run_PostedMessage_InvokesCallback()
    {
        uint pumpThreadId = 0;
        var threadIdReady = new ManualResetEventSlim();
        var messageReceived = new ManualResetEventSlim();
        uint receivedMessage = 0;

        var pumpThread = new Thread(() =>
        {
            pumpThreadId = MessageLoop.CurrentThreadId();
            threadIdReady.Set();
            MessageLoop.Run(
                (msg, _, _) =>
                {
                    receivedMessage = msg;
                    messageReceived.Set();
                    MessageLoop.Quit();
                }
            );
        })
        {
            IsBackground = true,
            Name = "twm-message-pump",
        };
        pumpThread.Start();

        try
        {
            threadIdReady
                .Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)
                .ShouldBeTrue();
            MessageLoop.Post(pumpThreadId, MessageLoop.WmApp).ShouldBeTrue();

            messageReceived
                .Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)
                .ShouldBeTrue();
            receivedMessage.ShouldBe(MessageLoop.WmApp);
        }
        finally
        {
            // If the test failed mid-way, the pump thread is still alive —
            // best-effort join so we don't leak it past the test boundary. If
            // it doesn't exit in 2s (Quit() never fired), it'll be cleaned
            // up at process exit (background thread).
            pumpThread.Join(TimeSpan.FromSeconds(2));
        }
    }

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void Post_ToExitedThread_ReturnsFalse()
    {
        uint threadId = 0;
        var ready = new ManualResetEventSlim();

        var thread = new Thread(() =>
        {
            threadId = MessageLoop.CurrentThreadId();
            ready.Set();
        })
        {
            IsBackground = true,
            Name = "twm-exited-thread-fixture",
        };
        thread.Start();

        ready.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ShouldBeTrue();
        thread.Join();

        // Thread is now exited; PostThreadMessageW against it returns false
        // (handle-died error branch).
        MessageLoop.Post(threadId, MessageLoop.WmApp).ShouldBeFalse();
    }

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void CurrentThreadId_ReturnsNonZero()
    {
        // Sanity check: if GetCurrentThreadId P/Invoke binding is broken,
        // threadId would be 0, which would mask Test 1's Post failure as
        // "invalid thread id" instead of the real cause.
        MessageLoop.CurrentThreadId().ShouldNotBe(0u);
    }
}
