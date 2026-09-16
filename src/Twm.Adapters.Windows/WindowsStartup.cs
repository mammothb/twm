using System.Diagnostics.CodeAnalysis;

namespace Twm.Adapters.Windows;

/// <summary>
/// Process-wide Windows startup concerns that must run before any
/// monitor/window work. Wraps the internal <see cref="NativeMethods" /> so the
/// app never touches raw P/Invoke.
/// </summary>
public static class WindowsStartup
{
    /// <summary>
    /// Allocates a dedicated console window for the process, so the WM's
    /// diagnostic logging is visible even when launched without a terminal,
    /// e.g., double-clicked with <c>--console</c>. No-op when a console is
    /// already attached.
    /// </summary>
    // Process-wide one-shot; would conflict across tests in the same xUnit
    // process and there is no parent console under dotnet test.
    [ExcludeFromCodeCoverage]
    public static void AllocateConsole() => NativeMethods.AllocateConsole();

    /// <summary>
    /// Attaches the process to its parent terminal's console so
    /// <see cref="Console" /> output, e.g., <c>twm-msg</c> client response, is
    /// visible there. Safe no-op when launched without a parent console.
    /// </summary>
    // Process-wide console attachment via AttachConsole(ATTACH_PARENT_PROCESS);
    // no parent console under dotnet test, and the call would conflict across
    // test instances.
    [ExcludeFromCodeCoverage]
    public static void AttachParentConsole() => NativeMethods.AttachParentConsole();

    /// <summary>
    /// Sets the foreground lock timeout to 0 so keyboard-driven focus change
    /// can bring the target window to the foreground. Windows otherwise blocks
    /// a background process (like Twm) from setting the foreground window and
    /// only flashes the taskbar button.
    /// </summary>
    // Process-wide setting via SystemParametersInfoW. Once applied, the change
    // persists for the lifetime of the test runner process — exercising it
    // from a test would leak state into every other test in the run.
    [ExcludeFromCodeCoverage]
    public static void DisableForegroundLockTimeout() =>
        NativeMethods.DisableForegroundLockTimeout();

    /// <summary>
    /// Opts the process into Per-Monitor-v2 DPI awareness, so monitor and
    /// window coordinates are reported in physical pixels. Must be called
    /// before enumerating monitors or windows.
    /// </summary>
    // Sets DPI awareness context (HANDLE)-4 for the entire process. Can only
    // be called once per process; the CI runner already has it set from
    // AppHost.Build, so a test would either no-op or fail.
    [ExcludeFromCodeCoverage]
    public static void EnablePerMonitorDpiAwareness() => NativeMethods.EnablePerMonitorV2Dpi();
}
