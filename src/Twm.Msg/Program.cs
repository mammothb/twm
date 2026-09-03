using Twm.Shared.Ipc;

string request = string.Join(' ', args);
if (string.IsNullOrWhiteSpace(request))
{
    Console.WriteLine("usage: twm-msg <command>  e.g. 'twm-msg focus left'");
    return 1;
}

try
{
    string response = IpcClient.Send(request);
    Console.WriteLine(response);
    return response.StartsWith("err ", StringComparison.Ordinal) ? 1 : 0;
}
catch (TimeoutException)
{
    Console.WriteLine("err twm is not running (no IPC server on the pipe).");
    return 1;
}
catch (IOException error)
{
    Console.WriteLine($"err ipc failure: {error.Message}");
    return 1;
}
