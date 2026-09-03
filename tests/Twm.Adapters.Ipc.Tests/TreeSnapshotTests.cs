using Twm.Domain.Geometry;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Adapters.Ipc.Tests;

public sealed class TreeSnapshotTests
{
    private static (RootContainer Root, TilingWindow First) BuildTwoWindowDesktop()
    {
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 100, 100));
        root.AppendChild(monitor);
        var workspace = new Workspace("1");
        monitor.AppendChild(workspace);
        var first = new TilingWindow(new WindowId(1));
        var second = new TilingWindow(new WindowId(2));
        workspace.AppendChild(first);
        workspace.AppendChild(second);
        new LayoutEngine(Gaps.None).Arrange(root);
        first.Focus();
        return (root, first);
    }

    [Fact]
    public void ToJson_MatchesGolden()
    {
        (RootContainer root, _) = BuildTwoWindowDesktop();
        string json = TreeSnapshotMapper.ToJson(root, id => $"win{id.Value}");

        json.ShouldBe(GoldenJson);
    }

    [Fact]
    public void ToJson_IsSingleLine()
    {
        (RootContainer root, _) = BuildTwoWindowDesktop();
        string json = TreeSnapshotMapper.ToJson(root, id => $"win{id.Value}");

        json.ShouldNotContain('\n');
        json.ShouldNotContain('\r');
    }

    [Fact]
    public void From_MarksFocusedWindowAndActiveWorkspace()
    {
        (RootContainer root, TilingWindow first) = BuildTwoWindowDesktop();
        TreeNode rootNode = TreeSnapshotMapper.From(root);

        TreeNode monitor = rootNode.Children.ShouldHaveSingleItem();
        TreeNode workspace = monitor.Children.ShouldHaveSingleItem();
        workspace.Active.ShouldBeTrue();
        workspace.Name.ShouldBe("1");
        workspace.Direction.ShouldBe("horizontal");
        workspace.Layout.ShouldBe("splith");

        TreeNode focused = workspace.Children!.Where(node => node.Focused).ShouldHaveSingleItem();
        focused.WindowId.ShouldBe((long)first.WindowId.Value);
        workspace.Children!.Count.ShouldBe(2);
    }

    private const string GoldenJson =
        """{"kind":"root","bounds":{"x":0,"y":0,"width":0,"height":0},"sizeFraction":1,"focused":false,"active":false,"children":[{"kind":"monitor","bounds":{"x":0,"y":0,"width":100,"height":100},"sizeFraction":1,"focused":false,"active":false,"children":[{"kind":"workspace","bounds":{"x":0,"y":0,"width":100,"height":100},"direction":"horizontal","layout":"splith","name":"1","sizeFraction":1,"focused":false,"active":true,"children":[{"kind":"window","bounds":{"x":0,"y":0,"width":50,"height":100},"windowId":1,"title":"win1","sizeFraction":1,"focused":true,"active":false},{"kind":"window","bounds":{"x":50,"y":0,"width":50,"height":100},"windowId":2,"title":"win2","sizeFraction":1,"focused":false,"active":false}]}]}]}""";
}
