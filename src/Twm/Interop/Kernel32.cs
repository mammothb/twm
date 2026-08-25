using System.Runtime.InteropServices;
using System.Text;

namespace Twm.Interop;

internal static partial class Kernel32
{
    internal const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
    internal const uint TOKEN_QUERY = 0x0008;
    internal const int TokenElevation = 20;

    [DllImport("kernel32.dll")]
    public static extern uint GetCurrentThreadId();

    [DllImport("kernel32.dll")]
    public static extern nint OpenProcess(uint access, bool inheritHandle, uint processId);

    [DllImport("kernel32.dll")]
    public static extern bool CloseHandle(nint handle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern bool QueryFullProcessImageName(
        nint process,
        uint flags,
        StringBuilder exeName,
        ref uint size
    );

    [DllImport("advapi32.dll")]
    public static extern bool OpenProcessToken(
        nint process,
        uint desiredAccess,
        out nint tokenHandle
    );

    /// <summary>TOKEN_ELEVATION is a single DWORD.</summary>
    [DllImport("advapi32.dll")]
    public static extern bool GetTokenInformation(
        nint tokenHandle,
        int informationClass,
        out uint tokenElevation,
        int tokenInformationLength,
        out int returnLength
    );

    /// <summary>
    /// True when the owning process runs elevated (admin). Such windows
    /// cannot be moved or focused by a non-elevated process (UIPI), so
    /// twm filters them out instead of half-managing them.
    /// </summary>
    public static bool IsProcessElevated(uint processId)
    {
        var process = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
        if (process == nint.Zero)
            return true; // can't even query: protected process, treat as elevated

        try
        {
            if (!OpenProcessToken(process, TOKEN_QUERY, out var token))
                return true;
            try
            {
                if (
                    !GetTokenInformation(
                        token,
                        TokenElevation,
                        out uint elevation,
                        sizeof(uint),
                        out _
                    )
                )
                    return true;
                return elevation != 0;
            }
            finally
            {
                CloseHandle(token);
            }
        }
        finally
        {
            CloseHandle(process);
        }
    }
}

internal static class ProcessNames
{
    /// <summary>Friendly process name (e.g. "firefox") for a window's owner.</summary>
    public static string OfWindow(nint hwnd)
    {
        uint pid = User32.GetWindowThreadProcessId(hwnd, out _);
        var process = Kernel32.OpenProcess(Kernel32.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (process == nint.Zero)
            return $"pid:{pid}";

        try
        {
            var sb = new StringBuilder(1024);
            uint size = (uint)sb.Capacity;
            if (Kernel32.QueryFullProcessImageName(process, 0, sb, ref size))
                return Path.GetFileName(sb.ToString(0, (int)size));
            return $"pid:{pid}";
        }
        finally
        {
            Kernel32.CloseHandle(process);
        }
    }
}
