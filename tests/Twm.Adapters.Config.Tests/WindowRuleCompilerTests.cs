using Twm.Application.Config;

namespace Twm.Adapters.Config.Tests;

public class WindowRuleCompilerTests
{
    [Fact]
    public void Compile_ValidRules_ParsesActionsAndCriteria()
    {
        WindowRuleCompileResult result = WindowRuleCompiler.Compile([
            new WindowRuleDto { Class = "TaskManagerWindow", Action = "ignore" },
            new WindowRuleDto { Title = "Picture in picture", Action = "MANAGE" },
        ]);

        result.Errors.ShouldBeEmpty();
        result.Rules.Count.ShouldBe(2);
        result.Rules[0].Action.ShouldBe(WindowRuleAction.Ignore);
        result.Rules[1].Action.ShouldBe(WindowRuleAction.Manage);
    }

    [Fact]
    public void Compile_InvalidAction_IsRejectedWithError()
    {
        WindowRuleCompileResult result = WindowRuleCompiler.Compile([
            new WindowRuleDto { Class = "TaskManagerWindow", Action = "float" },
        ]);

        result.Errors.ShouldNotBeEmpty();
        result.Rules.ShouldBeEmpty();
    }

    [Fact]
    public void Compile_NoCriteria_IsRejectedWithError()
    {
        WindowRuleCompileResult result = WindowRuleCompiler.Compile([
            new WindowRuleDto { Action = "ignore" },
        ]);

        result.Errors.ShouldNotBeEmpty();
        result.Rules.ShouldBeEmpty();
    }
}
