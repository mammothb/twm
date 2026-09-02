using Twm.Application.Config;
using Twm.Domain.Tiling;

namespace Twm.Adapters.Config.Tests;

public class ConfigMappingTests
{
    [Fact]
    public void MapGaps_Null_IsNone()
    {
        ConfigMapping.MapGaps(null).ShouldBe(Gaps.None);
    }

    [Fact]
    public void MapGaps_Values_MapInnerAndOuter()
    {
        ConfigMapping.MapGaps(new GapsDto { Inner = 8, Outer = 12 }).ShouldBe(new Gaps(8, 12));
    }

    [Fact]
    public void MapGaps_MissingNumber_DefaultsToZero()
    {
        ConfigMapping.MapGaps(new GapsDto { Inner = 8 }).ShouldBe(new Gaps(8, 0));
    }

    [Fact]
    public void MapWorkspaces_Null_IsNone()
    {
        ConfigMapping.MapWorkspaces(null).ShouldBeNull();
    }

    [Fact]
    public void MapWorkspaces_CopiesCountAndNames()
    {
        WorkspaceOptions? mapped = ConfigMapping.MapWorkspaces(
            new WorkspacesDto { PerMonitor = 3, Names = ["a", "b"] }
        );

        mapped.ShouldNotBeNull();
        mapped.PerMonitor.ShouldBe(3);
        mapped.Names.ShouldBe(["a", "b"]);
    }

    [Fact]
    public void CompileRules_ValidRules_ParsesActionsAndCriteria()
    {
        WindowRuleCompileResult result = ConfigMapping.CompileRules([
            new WindowRuleDto { Class = "TaskManagerWindow", Action = "ignore" },
            new WindowRuleDto { Title = "Picture in picture", Action = "MANAGE" },
        ]);

        result.Errors.ShouldBeEmpty();
        result.Rules.Count.ShouldBe(2);
        result.Rules[0].Action.ShouldBe(WindowRuleAction.Ignore);
        result.Rules[1].Action.ShouldBe(WindowRuleAction.Manage);
    }

    [Fact]
    public void CompileRules_InvalidAction_IsRejectedWithError()
    {
        WindowRuleCompileResult result = ConfigMapping.CompileRules([
            new WindowRuleDto { Class = "TaskManagerWindow", Action = "float" },
        ]);

        result.Errors.ShouldNotBeEmpty();
        result.Rules.ShouldBeEmpty();
    }

    [Fact]
    public void CompileRules_NoCriteria_IsRejectedWithError()
    {
        WindowRuleCompileResult result = ConfigMapping.CompileRules([
            new WindowRuleDto { Action = "ignore" },
        ]);

        result.Errors.ShouldNotBeEmpty();
        result.Rules.ShouldBeEmpty();
    }
}
