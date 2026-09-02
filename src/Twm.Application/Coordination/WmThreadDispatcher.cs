using System.Collections.Concurrent;
using System.Threading;

namespace Twm.Application.Coordination;

/// <summary>
/// Marshals IPC requests from a background thread (the pipe reader) onto the
/// WM's single message-loop thread, so <see cref="WmSession" />
/// (single-threaded) and all Win32 calls un on that one thread. The background
/// thread enqueues a request and asks the loop to wake (via the injected
/// <c>wake</c> delegate); the loop later calls <see cref="Drain" /> to run the
/// queued work and hand each response back.
/// </summary>
public sealed class WmThreadDispatcher
{
    private static readonly TimeSpan s_defaultResponseTimeout = TimeSpan.FromSeconds(5);

    private readonly Func<bool> _wake;
    private readonly Func<string, string> _handle;
    private readonly TimeSpan _responseTimeout;
    private readonly ConcurrentQueue<WorkItem> _queue = [];

    /// <param name="wake">Singles the WM thread to call <see cref="Drain" />;
    /// returns false if it could not.</param>
    /// <param name="handleOnWmThread">Runs one request and produces its
    /// response, on the WM thread.</param>
    /// <param name="responseTimeout">How long a caller waits for a response
    /// before giving up.</param>
    public WmThreadDispatcher(
        Func<bool> wake,
        Func<string, string> handleOnWmThread,
        TimeSpan? responseTimeout = null
    )
    {
        ArgumentNullException.ThrowIfNull(wake);
        ArgumentNullException.ThrowIfNull(handleOnWmThread);
        _wake = wake;
        _handle = handleOnWmThread;
        _responseTimeout = responseTimeout ?? s_defaultResponseTimeout;
    }

    /// <summary>
    /// Called on the background thread. Hands <paramref name="request" /> to
    /// the WM thread and blocks (bounded by the response timeout) for its
    /// response. Suitable as the IPC server dispatcher.
    /// </summary>
    public string DispatchFromBackground(string request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = new WorkItem(request);
        _queue.Enqueue(item);

        if (!_wake())
        {
            return "err twm is not accepting commands.";
        }

        if (!item.Done.Wait(_responseTimeout))
        {
            return "err timed out waiting for the window manager.";
        }

        string response = item.Response;
        item.Done.Dispose();
        return response;
    }

    /// <summary>
    /// Runs every queued request. Must be called on the WM (message-loop)
    /// thread.
    /// </summary>
    public void Drain()
    {
        while (_queue.TryDequeue(out WorkItem? item))
        {
            try
            {
                item.Response = _handle(item.Request);
            }
            catch (Exception ex)
            {
                item.Response = $"err {ex.Message}";
            }
            finally
            {
                item.Done.Set();
            }
        }
    }

    private sealed class WorkItem(string request)
    {
        public string Request { get; } = request;
        public string Response { get; set; } = "";
        public ManualResetEventSlim Done { get; } = new(false);
    }
}
