using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Twm.Application.Messaging;
using Twm.Domain.Geometry;
using Twm.Domain.Tree;

namespace Twm.Application.Commands;

/// <summary>
/// Parses one line of the <c>twm-msg</c> text protocol (i3-msg-style verbs)
/// into an <see cref="IpcRequest" />. Pure and platform-neutral. Bad inputs
/// yields <c>false</c> plus an error string rather than an exception, so the
/// IPC server can always answer with <c>err &lt;msg&gt;</c>.
public static class CommandParser
{
    /// <summary>
    /// Resize step (percent of the axis) when <c>resize</c> is given no amount;
    /// matches the keymap's 5%.
    /// </summary>
    private const double DefaultResizePercent = 5.0;

    /// <summary>
    /// Grammar: <c>focus|move &lt;dir&gt;</c>,
    /// <c>resize &lt;dir&gt; [percent]</c>, <c>split h|v</c>,
    /// <c>toggle-split</c>,
    /// <c>layout stacked|tabbed|splith|splitv|toggle-split</c>,
    /// <c>workspace &lt;name&gt;</c>, <c>move-to-workspace &lt;name&gt;</c>,
    /// <c>close</c>, <c>exit</c>, <c>get-tree</c>.
    /// </summary>
    public static bool TryParse(
        string line,
        [NotNullWhen(true)] out WmRequest? request,
        [NotNullWhen(false)] out string? error
    )
    {
        ArgumentNullException.ThrowIfNull(line);
        request = null;
        error = null;

        string[] tokens = line.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        if (tokens.Length == 0)
        {
            error = "empty command";
            return false;
        }

        switch (tokens[0].ToLowerInvariant())
        {
            case "focus":
                return TryDirection(
                    tokens,
                    direction => new FocusInDirectionCommand(direction),
                    out request,
                    out error
                );
            case "move":
                return TryDirection(
                    tokens,
                    direction => new MoveInDirectionCommand(direction),
                    out request,
                    out error
                );
            case "resize":
                return TryResize(tokens, out request, out error);
            case "split":
                return TrySplit(tokens, out request, out error);
            case "layout":
                return TryLayout(tokens, out request, out error);
            case "toggle-split":
                return TryNoArg(
                    tokens,
                    new RunCommandRequest(new ToggleSplitDirectionCommand()),
                    out request,
                    out error
                );
            case "workspace":
                return TryWorkspace(
                    tokens,
                    name => new FocusWorkspaceCommand(name),
                    out request,
                    out error
                );
            case "move-to-workspace":
                return TryWorkspace(
                    tokens,
                    name => new MoveWindowToWorkspaceCommand(name),
                    out request,
                    out error
                );
            case "close":
                return TryNoArg(tokens, new CloseRequest(), out request, out error);
            case "exit":
                return TryNoArg(tokens, new ExitRequest(), out request, out error);
            case "get-tree":
                return TryNoArg(tokens, new GetTreeRequest(), out request, out error);
            default:
                error = $"unknown command '{tokens[0]}'";
                return false;
        }
    }

    private static bool TryDirection(
        string[] tokens,
        Func<Direction, ICommand> factory,
        out WmRequest? request,
        out string? error
    )
    {
        request = null;
        error = null;
        if (tokens.Length != 2)
        {
            error = $"usage: {tokens[0]} <left|right|up|down>";
            return false;
        }

        if (!TryParseDirection(tokens[1], out Direction direction))
        {
            error = $"invalid direction '{tokens[1]}' (expected left|right|up|down)";
            return false;
        }

        request = new RunCommandRequest(factory(direction));
        return true;
    }

    private static bool TryResize(string[] tokens, out WmRequest? request, out string? error)
    {
        request = null;
        error = null;
        if (tokens.Length is < 2 or > 3)
        {
            error = $"usage: resize <left|right|up|down> [percent]";
            return false;
        }

        if (!TryParseDirection(tokens[1], out Direction direction))
        {
            error = $"invalid direction '{tokens[1]}' (expected left|right|up|down)";
            return false;
        }

        double percent = DefaultResizePercent;
        if (
            tokens.Length == 3
            && (
                !double.TryParse(
                    tokens[2],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out percent
                )
                || percent <= 0
            )
        )
        {
            error = $"invalid resize amount '{tokens[2]}' (expected a positive percent)";
            return false;
        }

        request = new RunCommandRequest(new ResizeInDirectionCommand(direction, percent / 100.0));
        return true;
    }

    private static bool TrySplit(string[] tokens, out WmRequest? request, out string? error)
    {
        request = null;
        error = null;
        if (tokens.Length != 2)
        {
            error = $"usage: split <h|v|horizontal|vertical>";
            return false;
        }

        switch (tokens[1].ToLowerInvariant())
        {
            case "h":
            case "horizontal":
                request = new RunCommandRequest(
                    new SplitDirectionCommand(TilingDirection.Horizontal)
                );
                return true;
            case "v":
            case "vertical":
                request = new RunCommandRequest(
                    new SplitDirectionCommand(TilingDirection.Vertical)
                );
                return true;
            default:
                error = $"invalid split direction '{tokens[1]}' (expected h|v|horizontal|vertical)";
                return false;
        }
    }

    private static bool TryLayout(string[] tokens, out WmRequest? request, out string? error)
    {
        request = null;
        error = null;
        if (tokens.Length != 2)
        {
            error = $"usage: layout <stacked|tabbed|splith|splitv|toggle-split>";
            return false;
        }

        switch (tokens[1].ToLowerInvariant())
        {
            case "stacked":
                request = new RunCommandRequest(new SetLayoutCommand(Layout.Stacked));
                return true;
            case "tabbed":
                request = new RunCommandRequest(new SetLayoutCommand(Layout.Tabbed));
                return true;
            case "splith":
                request = new RunCommandRequest(new SetLayoutCommand(Layout.SplitHorizontal));
                return true;
            case "splitv":
                request = new RunCommandRequest(new SetLayoutCommand(Layout.SplitVertical));
                return true;
            case "toggle-split":
            case "toggle":
                request = new RunCommandRequest(new ToggleSplitDirectionCommand());
                return true;
            default:
                error =
                    $"invalid layout '{tokens[1]}' (expected stacked|tabbed|splith|splitv|toggle-split)";
                return false;
        }
    }

    private static bool TryWorkspace(
        string[] tokens,
        Func<string, ICommand> factory,
        out WmRequest? request,
        out string? error
    )
    {
        request = null;
        error = null;
        if (tokens.Length < 2)
        {
            error = $"usage: {tokens[0]} <name>";
            return false;
        }

        // Everything after the verb is the name (allows names with spaces);
        // collapes runs of whitespace
        string name = string.Join(' ', tokens.Skip(1));
        request = new RunCommandRequest(factory(name));
        return true;
    }

    private static bool TryNoArg(
        string[] tokens,
        WmRequest result,
        out WmRequest? request,
        out string? error
    )
    {
        request = null;
        error = null;
        if (tokens.Length != 1)
        {
            error = $"usage: '{tokens[0]}' takes no arguments";
            return false;
        }

        request = result;
        return true;
    }

    private static bool TryParseDirection(string token, out Direction direction)
    {
        switch (token.ToLowerInvariant())
        {
            case "left":
                direction = Direction.Left;
                return true;
            case "right":
                direction = Direction.Right;
                return true;
            case "up":
                direction = Direction.Up;
                return true;
            case "down":
                direction = Direction.Down;
                return true;
            default:
                direction = default;
                return false;
        }
    }
}
