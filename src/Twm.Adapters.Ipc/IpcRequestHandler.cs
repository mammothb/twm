using Twm.Application.Commands;
using Twm.Application.Coordination;
using Twm.Application.Messaging;
using Twm.Domain.Tree;

namespace Twm.Adapters.Ipc;

/// <summary>
/// Turns one text request line into a response line, driving the WM. Parses via
/// the shared <see cref="CommandParser" /> grammar and dispatches to the
/// <see cref="WmSession" />: <c>get-tree</c> serializes the live tree,
/// <c>close</c> closes the focused window, <c>exit</c> invokes
/// <paramref name="onExit" />, and any other verb runs its core command through
/// the bus. The WM thread runs <see cref="Handle" /> via the dispatcher. The
/// optional title resolver attaches window titles the core tree does not store.
/// </summary>
public sealed class IpcRequestHandler(
    WmSession session,
    Action onExit,
    Func<WindowId, string?>? titleOf = null
)
{
    public string Handle(string request)
    {
        if (!CommandParser.TryParse(request, out WmRequest? parsed, out string? parseError))
        {
            return $"err {parseError}";
        }

        switch (parsed)
        {
            case RunCommandRequest run:
                CommandResult result = session.Execute(run.Command);
                return result.Success ? "ok" : $"err {result.Error ?? "command failed"}";
            case GetTreeRequest:
                return TreeSnapshotMapper.ToJson(session.Root, titleOf);
            case CloseRequest:
                session.CloseFocused();
                return "ok";
            case ExitRequest:
                onExit();
                return "ok";
            default:
                return "err unhandled request";
        }
    }
}
