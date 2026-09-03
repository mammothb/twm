using System.IO.Pipes;
using System.Text;

namespace Twm.Shared.Ipc;

/// <summary>
/// The <c>twm-msg</c> client half: connect to the WM's named pipe, send one
/// request line, and return the one response line. Synchronous and one-shot (a
/// single CLI invocation).
/// </summary>
public static class IpcClient
{
    private const int DefaultConnectTimeoutMs = 2000;

    public static string Send(
        string request,
        string pipeName = IpcPipe.DefaultPipeName,
        int connectTimeoutMs = DefaultConnectTimeoutMs
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
        client.Connect(connectTimeoutMs);
        using var reader = new StreamReader(client, leaveOpen: true);
        // Write the request as raw bytes rather than a StreamWriter, whose
        // dispose-time flush throw "pipe is broken" if the server has already
        // replied and closed. Writing directly avoids that hazard
        byte[] payload = Encoding.UTF8.GetBytes(request + '\n');
        client.Write(payload, 0, payload.Length);
        client.Flush();
        return reader.ReadLine() ?? "";
    }
}
