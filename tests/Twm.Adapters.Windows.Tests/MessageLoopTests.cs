using System.Diagnostics;
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
                .Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken)
                .ShouldBeTrue();

            // PostThreadMessageW can fail with ERROR_INVALID_THREAD_ID if the
            // pump thread hasn't yet entered GetMessageW (which is what
            // creates the message queue). Poll until the queue exists.
            bool posted = false;
            Stopwatch sw = Stopwatch.StartNew();
            while (!posted && sw.Elapsed < TimeSpan.FromSeconds(10))
            {
                posted = MessageLoop.Post(pumpThreadId, MessageLoop.WmApp);
                if (!posted)
                {
                    Thread.Sleep(10);
                }
            }

            posted.ShouldBeTrue();

            messageReceived
                .Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken)
                .ShouldBeTrue();
            receivedMessage.ShouldBe(MessageLoop.WmApp);
        }
        finally
        {
            // If the test failed before the callback ran, the pump thread is
            // still blocked in GetMessageW. Post WM_QUIT so it returns false
            // and Run exits cleanly; assert the thread actually died rather
            // than silently leaking it past the test boundary.
            if (pumpThreadId != 0)
            {
                MessageLoop.Post(pumpThreadId, MessageLoop.WmQuit);
            }

            pumpThread.Join(TimeSpan.FromSeconds(5));
            pumpThread.IsAlive.ShouldBeFalse();
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

        MessageLoop.Post(threadId, MessageLoop.WmApp).ShouldBeFalse();
    }

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void CurrentThreadId_ReturnsNonZero()
    {
        MessageLoop.CurrentThreadId().ShouldNotBe(0u);
    }
}
