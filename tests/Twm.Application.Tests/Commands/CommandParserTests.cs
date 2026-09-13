using Twm.Application.Commands;
using Twm.Application.Messaging;
using Twm.Domain.Geometry;
using Twm.Domain.Tree;

namespace Twm.Application.Tests.Commands;

public sealed class CommandParserTests
{
    [Theory]
    [InlineData("focus left", Direction.Left)]
    [InlineData("focus right", Direction.Right)]
    [InlineData("focus up", Direction.Up)]
    [InlineData("focus down", Direction.Down)]
    [InlineData("FOCUS Left", Direction.Left)]
    public void Parse_Focus(string line, Direction expected)
    {
        CommandParser.TryParse(line, out WmRequest? request, out _).ShouldBeTrue();
        Command<FocusInDirectionCommand>(request).Direction.ShouldBe(expected);
    }

    [Theory]
    [InlineData("move left", Direction.Left)]
    [InlineData("move down", Direction.Down)]
    public void Parse_Move(string line, Direction expected)
    {
        CommandParser.TryParse(line, out WmRequest? request, out _).ShouldBeTrue();
        Command<MoveInDirectionCommand>(request).Direction.ShouldBe(expected);
    }

    [Fact]
    public void Parse_Resize_DefaultAmountIs5Percent()
    {
        CommandParser.TryParse("resize right", out WmRequest? request, out _).ShouldBeTrue();
        ResizeInDirectionCommand command = Command<ResizeInDirectionCommand>(request);
        command.Direction.ShouldBe(Direction.Right);
        command.DeltaFraction.ShouldBe(0.05, 1e-10);
    }

    [Theory]
    [InlineData("split h", TilingDirection.Horizontal)]
    [InlineData("split horizontal", TilingDirection.Horizontal)]
    [InlineData("split v", TilingDirection.Vertical)]
    [InlineData("split vertical", TilingDirection.Vertical)]
    public void Parse_Split(string line, TilingDirection expected)
    {
        CommandParser.TryParse(line, out WmRequest? request, out _).ShouldBeTrue();
        Command<SplitDirectionCommand>(request).Direction.ShouldBe(expected);
    }

    [Theory]
    [InlineData("layout tabbed", Layout.Tabbed)]
    [InlineData("layout stacked", Layout.Stacked)]
    [InlineData("layout splitv", Layout.SplitVertical)]
    public void Parse_Layout(string line, Layout expected)
    {
        CommandParser.TryParse(line, out WmRequest? request, out _).ShouldBeTrue();
        Command<SetLayoutCommand>(request).Layout.ShouldBe(expected);
    }

    [Fact]
    public void Parse_LayoutToggleSplit_MapsToToggleCommand()
    {
        CommandParser.TryParse("layout toggle-split", out WmRequest? request, out _).ShouldBeTrue();
        Command<ToggleSplitDirectionCommand>(request);
    }

    [Fact]
    public void Parse_Workspace()
    {
        CommandParser.TryParse("workspace 3", out WmRequest? request, out _).ShouldBeTrue();
        Command<FocusWorkspaceCommand>(request).WorkspaceName.ShouldBe("3");
    }

    [Fact]
    public void Parse_MoveToWorkspace()
    {
        CommandParser.TryParse("move-to-workspace 3", out WmRequest? request, out _).ShouldBeTrue();
        Command<MoveWindowToWorkspaceCommand>(request).WorkspaceName.ShouldBe("3");
    }

    [Fact]
    public void Parse_AppLevelRequest()
    {
        CommandParser.TryParse("close", out WmRequest? closeRequest, out _).ShouldBeTrue();
        closeRequest.ShouldBeOfType<CloseRequest>();

        CommandParser.TryParse("exit", out WmRequest? exitRequest, out _).ShouldBeTrue();
        exitRequest.ShouldBeOfType<ExitRequest>();

        CommandParser
            .TryParse("reconcile-displays", out WmRequest? reconcileRequest, out _)
            .ShouldBeTrue();
        reconcileRequest.ShouldBeOfType<ReconcileRequest>();

        CommandParser.TryParse("get-tree", out WmRequest? getTreeRequest, out _).ShouldBeTrue();
        getTreeRequest.ShouldBeOfType<GetTreeRequest>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("invalid")]
    [InlineData("focus")]
    [InlineData("focus invalid")]
    [InlineData("move")]
    [InlineData("resize left abc")]
    [InlineData("resize left -5")]
    [InlineData("resize left 0")]
    [InlineData("split invalid")]
    [InlineData("toggle-split invalid")]
    [InlineData("workspace")]
    [InlineData("get-tree invalid")]
    [InlineData("reconcile-displays invalid")]
    public void Parse_Invalid_ReturnsError(string line)
    {
        CommandParser.TryParse(line, out WmRequest? request, out string? error).ShouldBeFalse();
        request.ShouldBeNull();
        error.ShouldNotBeNullOrWhiteSpace();
    }

    private static T Command<T>(WmRequest? request)
        where T : ICommand
    {
        RunCommandRequest runCommandRequest = request.ShouldBeOfType<RunCommandRequest>();
        return runCommandRequest.Command.ShouldBeOfType<T>();
    }
}
