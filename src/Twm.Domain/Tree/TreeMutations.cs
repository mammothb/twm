namespace Twm.Domain.Tree;

public static class TreeMutations
{
    /// <summary>
    /// Walks up from <paramref name="start" /> removing empty splits and
    /// flattening single-child splits (the lone child takes the split's place
    /// and size). Never removes or flattens a workspace.
    /// </summary>
    public static void Cleanup(this Container? start)
    {
        Container? node = start;
        while (node is SplitContainer split and not Workspace && split.Parent is Container parent)
        {
            if (split.Children.Count == 0)
            {
                parent.RemoveChild(split);
                node = parent;
            }
            else if (split.Children.Count == 1)
            {
                Container onlyChild = split.Children[0];
                double fraction = split.SizeFraction;
                split.RemoveChild(onlyChild);
                onlyChild.SizeFraction = fraction;
                parent.ReplaceChild(split, onlyChild);
                node = parent;
            }
            else
            {
                return;
            }
        }
    }
}
