namespace Twm.Adapters.Windows;

/// <summary>
/// Process-wide Windows startup concerns that must run before any
/// monitor/window work. Wraps the internal <see cref="NativeMethods" /> so the
/// app never touches raw P/Invoke.
/// </summary>
public static class WindowsStartup
{
    /// <summary>
    /// Allocates a dedicated cnosole window for the process, so the WM's
    /// diagnostic logging is visible even when launched without a terminal,
    /// e.g., doubled-clicked with <c>--console</c>. No-op when a console is
    /// already attached.
    /// </summary>
    public static void AllocateConsole() => NativeMethods.AllocateConsole();

    /// <summary>
    /// Attaches the process to its parent terminal's console so
    /// <see cref="Console" />  output, e.g., <c>twm-msg</c> client response, is
    /// visible there. Safe no-op when launched without a parent console.
    /// </summary>
    public static void AttachParentConsole() => NativeMethods.AttachParentConsole();

    /// <summary>
    /// Sets the foreground lock timeout to 0 so keyboard driven focus change
    /// can bring the target window to the foreground. Windows otherwise blocks
    /// a background process (like Twm) from setting the foreground window and
    /// only flashes the taskbar button.
    /// </summary>
    public static void DisableForegroundLockTimeout() =>
        NativeMethods.DisableForegroundLockTimeout();

    /// <summary>
    /// Opts the process into Per-Monitor-v2 DPI awareness, so monitor and
    /// window coordinates are reported in physical pixels. Must be called
    /// before enumerating monitors or windows.
    /// </summary>
    public static void EnablePerMonitorDpiAwareness() => NativeMethods.EnablePerMonitorV2Dpi();
}
