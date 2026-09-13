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
        // Arrange / Act
        bool parsed = CommandParser.TryParse(line, out WmRequest? request, out _);

        // Assert
        parsed.ShouldBeTrue();
        Command<FocusInDirectionCommand>(request).Direction.ShouldBe(expected);
    }

    [Theory]
    [InlineData("move left", Direction.Left)]
    [InlineData("move down", Direction.Down)]
    public void Parse_Move(string line, Direction expected)
    {
        // Arrange / Act
        bool parsed = CommandParser.TryParse(line, out WmRequest? request, out _);

        // Assert
        parsed.ShouldBeTrue();
        Command<MoveInDirectionCommand>(request).Direction.ShouldBe(expected);
    }

    [Fact]
    public void Parse_Resize_DefaultAmountIs5Percent()
    {
        // Arrange / Act
        bool parsed = CommandParser.TryParse("resize right", out WmRequest? request, out _);

        // Assert
        parsed.ShouldBeTrue();
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
        // Arrange / Act
        bool parsed = CommandParser.TryParse(line, out WmRequest? request, out _);

        // Assert
        parsed.ShouldBeTrue();
        Command<SplitInDirectionCommand>(request).Direction.ShouldBe(expected);
    }

    [Theory]
    [InlineData("layout tabbed", Layout.Tabbed)]
    [InlineData("layout stacked", Layout.Stacked)]
    [InlineData("layout splitv", Layout.SplitVertical)]
    public void Parse_Layout(string line, Layout expected)
    {
        // Arrange / Act
        bool parsed = CommandParser.TryParse(line, out WmRequest? request, out _);

        // Assert
        parsed.ShouldBeTrue();
        Command<SetLayoutCommand>(request).Layout.ShouldBe(expected);
    }

    [Fact]
    public void Parse_LayoutToggleSplit_MapsToToggleCommand()
    {
        // Arrange / Act
        bool parsed = CommandParser.TryParse("layout toggle-split", out WmRequest? request, out _);

        // Assert
        parsed.ShouldBeTrue();
        Command<ToggleSplitDirectionCommand>(request);
    }

    [Fact]
    public void Parse_Workspace_ReturnsFocusWorkspaceCommand()
    {
        // Arrange / Act
        bool parsed = CommandParser.TryParse("workspace 3", out WmRequest? request, out _);

        // Assert
        parsed.ShouldBeTrue();
        Command<FocusWorkspaceCommand>(request).WorkspaceName.ShouldBe("3");
    }

    [Fact]
    public void Parse_MoveToWorkspace_ReturnsMoveWindowToWorkspaceCommand()
    {
        // Arrange / Act
        bool parsed = CommandParser.TryParse("move-to-workspace 3", out WmRequest? request, out _);

        // Assert
        parsed.ShouldBeTrue();
        Command<MoveWindowToWorkspaceCommand>(request).WorkspaceName.ShouldBe("3");
    }

    [Fact]
    public void Parse_AppLevelVerbs_MapToRequests()
    {
        // Arrange / Act / Assert
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
    [InlineData("resize")]
    [InlineData("resize left right")]
    [InlineData("resize diagonal")]
    [InlineData("resize left abc")]
    [InlineData("resize left -5")]
    [InlineData("resize left 0")]
    [InlineData("split invalid")]
    [InlineData("split h extra")]
    [InlineData("layout invalid")]
    [InlineData("layout tabbed extra")]
    [InlineData("workspace")]
    [InlineData("move-to-workspace")]
    [InlineData("get-tree invalid")]
    [InlineData("reconcile-displays invalid")]
    public void Parse_Invalid_ReturnsError(string line)
    {
        // Arrange / Act
        bool parsed = CommandParser.TryParse(line, out WmRequest? request, out string? error);

        // Assert
        parsed.ShouldBeFalse();
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
