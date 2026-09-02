using Twm.Application.Config;
using Twm.Application.OutboundPorts;
using Twm.Domain.Geometry;
using Twm.Domain.Tree;

namespace Twm.Application.Tests;

public class WindowRuleTests
{
    private static NativeWindowInfo Window(string className, string title) =>
        new(
            Id: new WindowId(1),
            Title: title,
            ClassName: className,
            Bounds: new Rect(0, 0, 800, 600),
            IsVisible: true,
            IsCloaked: false,
            IsToolWindow: false,
            IsMinimized: false
        );

    [Fact]
    public void Matches_ClassExact_TitleSubstringCaseInsensitive()
    {
        var classRule = new WindowRule("Notepad", null, WindowRuleAction.Ignore);

        classRule.Matches(Window("Notepad", "anything")).ShouldBeTrue();
        classRule.Matches(Window("notepad", "anything")).ShouldBeFalse();

        var titleRule = new WindowRule(
            null,
            "picture in picture - YouTube",
            WindowRuleAction.Ignore
        );

        titleRule.Matches(Window("X", "Picture In Picture - YouTube")).ShouldBeTrue();
        titleRule.Matches(Window("X", "some other window")).ShouldBeFalse();
    }

    [Fact]
    public void Matches_BothCriteria_RequiresAllToMatch()
    {
        var rule = new WindowRule("Chrome", "YouTube", WindowRuleAction.Ignore);

        rule.Matches(Window("Chrome", "Cats - YouTube")).ShouldBeTrue();
        rule.Matches(Window("Chrome", "Docs")).ShouldBeFalse();
        rule.Matches(Window("Edge", "Cats - YouTube")).ShouldBeFalse();
    }
}
