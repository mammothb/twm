using System.IO.Pipes;
using System.Threading;
using Twm.Shared.Ipc;

namespace Twm.Adapters.Ipc;

/// <summary>
/// A named-pipe sever that answers one request line per connection. It accepts
/// on a background task; each connection reads a single line, runs it through
/// the injected dispatcher, and writes one response line.
/// </summary>
public sealed class IpcServer : IDisposable
{
    /// <summary>
    /// The fixed pipe name the <c>twm-msg</c> client connects to.
    /// Single-sourced in the shared Twm.Shared.Ipc lib; forwarded here for the
    /// existing API.
    /// </summary>
    public const string DefaultPipeName = IpcPipe.DefaultPipeName;

    /// <summary> Length cap on one request line.</summary>
    private const int MaxRequestLength = 8192;

    private readonly string _pipeName;
    private readonly Func<string, string> _dispatch;
    private readonly CancellationTokenSource _cancellation = new();
    private Task? _acceptLoop;

    public IpcServer(Func<string, string> dispatch, string pipeName = DefaultPipeName)
    {
        ArgumentNullException.ThrowIfNull(dispatch);
        ArgumentException.ThrowIfNullOrEmpty(pipeName);
        _dispatch = dispatch;
        _pipeName = pipeName;
    }

    public void Start()
    {
        if (_acceptLoop is not null)
        {
            throw new InvalidOperationException("The IPC server is already started.");
        }
        _acceptLoop = Task.Run(() => AcceptLoopAsync(_cancellation.Token));
    }

    public void Dispose()
    {
        _cancellation.Cancel();
        try
        {
            _acceptLoop?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            // The loop unwinds via cancellation
        }
        _cancellation.Dispose();
    }

    private async Task AcceptLoopAsync(CancellationToken cancellation)
    {
        while (!cancellation.IsCancellationRequested)
        {
            var server = new NamedPipeServerStream(
                pipeName: _pipeName,
                direction: PipeDirection.InOut,
                maxNumberOfServerInstances: NamedPipeServerStream.MaxAllowedServerInstances,
                transmissionMode: PipeTransmissionMode.Byte,
                options: PipeOptions.Asynchronous
            );

            try
            {
                await server.WaitForConnectionAsync(cancellation).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Shutdown cancellation or a failed accept: drop this instance,
                // then exit or try the next one
                await server.DisposeAsync().ConfigureAwait(false);
                if (cancellation.IsCancellationRequested)
                {
                    return;
                }
                continue;
            }
            // Handle off the accept loop so the next listener is ready, so a
            // client reconnecting back-to-back never hits an empty gap
            _ = HandleConnectionAsync(server, cancellation);
        }
    }

    private async Task HandleConnectionAsync(
        NamedPipeServerStream server,
        CancellationToken cancellation
    )
    {
        try
        {
            await HandleAsync(server, cancellation).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // per-connection fault must never take down the server
        }
        finally
        {
            await server.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task HandleAsync(NamedPipeServerStream server, CancellationToken cancellation)
    {
        using var reader = new StreamReader(server, leaveOpen: true);
        using var writer = new StreamWriter(server, leaveOpen: true) { AutoFlush = true };

        string? request = await reader.ReadLineAsync(cancellation).ConfigureAwait(false);
        if (request is null)
        {
            return;
        }

        string response;
        if (request.Length > MaxRequestLength)
        {
            response = "err request too long";
        }
        else
        {
            try
            {
                response = _dispatch(request);
            }
            catch (Exception ex)
            {
                // A throwing dispatcher must never take down the server
                response = $"err {ex.Message}";
            }
        }

        await writer.WriteLineAsync(response.AsMemory(), cancellation).ConfigureAwait(false);

        if (OperatingSystem.IsWindows())
        {
            server.WaitForPipeDrain();
        }
        else
        {
            byte[] scratch = new byte[1];
            try
            {
                while (await server.ReadAsync(scratch, cancellation).ConfigureAwait(false) > 0)
                {
                    // read until pipe is drained
                }
            }
            catch (IOException)
            {
                // client already vanished
            }
        }
    }
}
