namespace Twm.Shared.Ipc;

/// <summary>
/// The fixed named-pipe identity shared by the WM's IPC server and the
/// <c>twm-msg</c>.
/// </summary>
public static class IpcPipe
{
    /// <summary>The pipe name the <c>twm-msg</c> client connects to.</summary>
    public const string DefaultPipeName = "twm-msg";
}
