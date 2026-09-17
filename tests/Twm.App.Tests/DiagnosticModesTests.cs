using Twm.Adapters.Windows.Diagnostics;
using Twm.Application.Config;
using Twm.Application.Coordination;
using Twm.Application.OutboundPorts;
using Twm.Domain.Geometry;
using Twm.Domain.Tree;

namespace Twm.App.Tests;

public sealed class DiagnosticModesTests
{
    [Fact]
    public void FormatWindowLine_NoOwnerNoDiagnostic_PrintsBasicFormatAndManageDecision()
    {
        NativeWindowInfo window = MakeWindow(
            new WindowId(0x1234),
            title: "hello",
            className: "Foo"
        );
        var filter = new WindowFilter(null); // default rules → MANAGE
        var idToWindow = new Dictionary<WindowId, NativeWindowInfo>();
        var idToDiagnostic = new Dictionary<WindowId, WindowDiagnostic>();

        string line = DiagnosticModes.FormatWindowLine(window, filter, idToWindow, idToDiagnostic);

        line.ShouldContain("[MANAGE]");
        line.ShouldContain("hello");
        line.ShouldContain("Foo");
        line.ShouldContain("0x1234");
        line.ShouldContain("owner="); // owner key always present, even when empty
    }

    [Fact]
    public void FormatWindowLine_WithDiagnostic_IncludesPidAndExe()
    {
        NativeWindowInfo window = MakeWindow(new WindowId(0x1234));
        var filter = new WindowFilter(null);
        var idToWindow = new Dictionary<WindowId, NativeWindowInfo>();
        var idToDiagnostic = new Dictionary<WindowId, WindowDiagnostic>
        {
            [window.Id] = new WindowDiagnostic(
                Id: window.Id,
                ProcessId: 4242,
                ProcessName: "notepad.exe",
                OwnerClass: null,
                OwnerProcessName: null
            ),
        };

        string line = DiagnosticModes.FormatWindowLine(window, filter, idToWindow, idToDiagnostic);

        line.ShouldContain("pid=4242");
        line.ShouldContain("exe=notepad.exe");
    }

    [Fact]
    public void FormatWindowLine_WithOwner_IncludesOwnerTitleAndHexId()
    {
        NativeWindowInfo window = MakeWindow(new WindowId(0x1234), owner: new WindowId(0xABCD));
        NativeWindowInfo ownerWindow = MakeWindow(new WindowId(0xABCD), title: "owner-title");
        var filter = new WindowFilter(null);
        var idToWindow = new Dictionary<WindowId, NativeWindowInfo>
        {
            [ownerWindow.Id] = ownerWindow,
        };
        var idToDiagnostic = new Dictionary<WindowId, WindowDiagnostic>();

        string line = DiagnosticModes.FormatWindowLine(window, filter, idToWindow, idToDiagnostic);

        line.ShouldContain("0xABCD");
        line.ShouldContain("\"owner-title\"");
    }

    [Fact]
    public void FormatWindowLine_UnmanageableWindow_ShowsIgnoreDecision()
    {
        // WindowRule.Ignore on a class name that matches this window forces
        // IsManageable to return false. Default rules (null) would MANAGE it.
        NativeWindowInfo window = MakeWindow(new WindowId(0x1234), className: "WillBeIgnored");
        var rules = new List<WindowRule>
        {
            new(ClassName: "WillBeIgnored", TitleSubstring: null, Action: WindowRuleAction.Ignore),
        };
        var filter = new WindowFilter(rules);
        var idToWindow = new Dictionary<WindowId, NativeWindowInfo>();
        var idToDiagnostic = new Dictionary<WindowId, WindowDiagnostic>();

        string line = DiagnosticModes.FormatWindowLine(window, filter, idToWindow, idToDiagnostic);

        line.ShouldContain("[ignore]");
    }

    private static NativeWindowInfo MakeWindow(
        WindowId id,
        string title = "test-window",
        string className = "TestClass",
        WindowId? owner = null
    ) =>
        new(
            Id: id,
            Title: title,
            ClassName: className,
            Bounds: new Rect(0, 0, 100, 100),
            IsVisible: true,
            IsCloaked: false,
            IsToolWindow: false,
            IsMinimized: false,
            IsChild: false,
            IsElevated: false,
            IsNoActivate: false,
            IsMenuPopup: false,
            IsLayered: false,
            HasCaption: true,
            HasWindowEdge: true,
            Owner: owner,
            IsDlgModalFrame: false
        );
}
