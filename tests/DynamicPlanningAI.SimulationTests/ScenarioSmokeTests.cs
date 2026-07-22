using System.IO;
using DynamicPlanningAI.Configuration;
using DynamicPlanningAI.Sample.Simulation;
using Xunit;

namespace DynamicPlanningAI.SimulationTests;

public sealed class ScenarioSmokeTests
{
    public static TheoryData<string> AllScenarios => new()
    {
        "BasicAttack",
        "TakeCover",
        "AdvanceUnderSuppression",
        "EmergentSideAttack",
        "GrenadeEscape",
        "BlockedDoorReplanning",
        "WindowTraversalFallback",
        "LostTargetSearch",
        "BlindFireUnderPressure",
        "SquadSearch",
        "OrderOverriddenByDanger",
        "ZeroAllocationSteadyState",
    };

    [Theory]
    [MemberData(nameof(AllScenarios))]
    public void Scenario_CompletesSuccessfully_WithinTickBudget(string scenarioName)
    {
        ScenarioRunner runner = new(ConfigurationBundle.CreateDefault());
        ScenarioResult result = runner.Run(
            scenarioName,
            seedOverride: null,
            traceDirectory: Path.Combine(Path.GetTempPath(), "tg-sim"));
        Assert.True(result.Success, scenarioName + ": " + result.Summary);
        Assert.True(result.TicksExecuted <= result.MaxTicks, scenarioName + " exceeded tick budget");
    }
}
