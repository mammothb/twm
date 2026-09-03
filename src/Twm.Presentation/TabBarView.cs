using Twm.Domain.Geometry;
using Twm.Domain.Tree;

namespace Twm.Presentation;

/// <summary>
/// One tab/stack entry: a child's representative title and whether it is the
/// focused one.
/// </summary>
public sealed record TabItem(string Title, bool Focused);

/// <summary>
/// The render model for one tabbed/stacked container's bar.
/// <see cref="ContainerId" /> keys the bar window across updates.
public sealed record TabBarView(
    Guid ContainerId,
    Rect Bounds,
    Layout Layout,
    IReadOnlyList<TabItem> Tabs
);
