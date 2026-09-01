namespace Twm.Application.Diagnostics;

/// <summary>
/// Minimal, opt-in diagnostic log. The app installs a sink (file and/or
/// console) via <see cref="Init" />; when no sink is set, <see cref="Line" />
/// is a cheap no-op, so instrumented hot paths cost nothing in normal runs.
/// Reflection-free.
/// </summary>
public static class Log
{
    private static Action<string>? s_sink;
    public static bool Enabled => s_sink is not null;

    public static void Init(Action<string>? sink) => s_sink = sink;

    public static void Line(string message) =>
        s_sink?.Invoke($"{DateTimeOffset.Now:HH:mm:ss.fff} {message}");
}
