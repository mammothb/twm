namespace Twm;

public static class Log
{
    public static bool TraceEnabled => Environment.GetEnvironmentVariable("TWM_TRACE") == "1";

    public static void Info(string message) => Write("INFO ", message);

    public static void Warn(string message) => Write("WARN ", message);

    public static void Error(string message) => Write("ERROR", message);

    public static void Trace(string message)
    {
        if (TraceEnabled)
        {
            Write("TRACE", message);
        }
    }

    private static void Write(string level, string message) =>
        Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} [{level}] {message}");
}
