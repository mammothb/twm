using Twm.Application.InboundPorts;
using Twm.Domain.Tiling;

namespace Twm.Application.Config;

/// <summary>
/// The concrete inputs the WM consumes, resolved from the YAML by the config
/// adapter. Holds keymap, window rules (from which the composition root builds
/// the <c>WindowFilter</c>), gaps, workspace layout, and bar/border/tab
/// options, plus every non-fatal resolution error. Pure data, the adapter
/// constructs it; the application owns the type.
/// </summary>
public sealed record ResolvedConfig(
    IReadOnlyDictionary<KeyBinding, KeyEffect> Keymap,
    IReadOnlyList<WindowRule> WindowRules,
    Gaps Gaps,
    WorkspaceOptions? Workspaces,
    BarOptions Bar,
    BorderOptions Border,
    TabOptions Tabs,
    IReadOnlyList<string> Errors
);
