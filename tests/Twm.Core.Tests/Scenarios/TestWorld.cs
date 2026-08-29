using Twm.Core.Bussing;
using Twm.Core.Commands;
using Twm.Core.Geometry;
using Twm.Core.Layout;
using Twm.Core.Tree;

namespace Twm.Core.Tests.Scenarios;

/// <summary>
/// A two-monitor desktop wired through the <see cref="Bussing.Bus" /> with
/// every handler registered, for end-to-end scenario tests. Primary is 1920x1080
/// at the origin; secondary is 1280x1024 to its right.
/// </summary>
public sealed class TestWorld
{
    public RootContainer Root { get; }
    public Monitor Primary { get; }
    public Monitor Secondary { get; }
    public LayoutEngine Layout { get; }
    public Bus Bus { get; }

    public TestWorld(
        IReadOnlyList<string>? primaryWorkspaces = null,
        IReadOnlyList<string>? secondaryWorkspaces = null
    )
    {
        primaryWorkspaces ??= ["1"];
        secondaryWorkspaces ??= ["2"];

        Root = new RootContainer();
        Primary = new Monitor(new Rect(0, 0, 1920, 1080));
        Secondary = new Monitor(new Rect(1920, 0, 1280, 1024));
        Root.AppendChild(Primary);
        Root.AppendChild(Secondary);

        foreach (string name in primaryWorkspaces)
        {
            Primary.AppendChild(new Workspace(name));
        }

        foreach (string name in secondaryWorkspaces)
        {
            Secondary.AppendChild(new Workspace(name));
        }

        Layout = new LayoutEngine();
        Bus = new Bus();
        Bus.Register(new AdoptWindowHandler(Root, Layout));
        Bus.Register(new FocusInDirectionHandler(Root, Layout));
        Bus.Register(new FocusWorkspaceHandler(Root, Layout));
        Bus.Register(new MoveInDirectionHandler(Root, Layout));
        Bus.Register(new MoveWindowToWorkspaceHandler(Root, Layout));
        Bus.Register(new RemoveWindowHandler(Root, Layout));
        Bus.Register(new ResizeContainerHandler(Root, Layout));
        Bus.Register(new ResizeInDirectionHandler(Root, Layout));
        Bus.Register(new SetLayoutHandler(Root, Layout));
        Bus.Register(new SplitDirectionHandler(Root, Layout));
        Bus.Register(new ToggleSplitDirectionHandler(Root, Layout));

        Layout.Arrange(Root);
    }

    public CommandResult Invoke(ICommand command) => Bus.Invoke(command);

    /// <summary>
    /// Opens a new window (via the adopt command) on the given monitor.
    /// </summary>
    public void Adopt(int id, Monitor monitor) =>
        Bus.Invoke(new AdoptWindowCommand(new WindowId(id), monitor));

    public TilingWindow Window(int id) => Root.FindWindow(new WindowId(id))!;

    public static Workspace WorkspaceOf(Monitor monitor, int index) =>
        (Workspace)monitor.Children[index];
}
