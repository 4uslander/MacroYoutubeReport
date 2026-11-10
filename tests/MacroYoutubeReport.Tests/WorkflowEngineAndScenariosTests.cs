using MacroYoutubeReport.Core.Drivers;
using MacroYoutubeReport.Core.Enums;
using MacroYoutubeReport.Core.Models;
using MacroYoutubeReport.Core.Workflows;

namespace MacroYoutubeReport.Tests;

public class WorkflowEngineAndScenariosTests
{
    private sealed class MockYouTubeReportDriver : IYouTubeReportDriver
    {
        public PlatformType Platform => PlatformType.Android;
        public string TargetId => "mock-device-01";
        public List<string> ActionLogs { get; } = new();

        public Task InitializeAsync(CancellationToken ct = default)
        {
            ActionLogs.Add("Initialize");
            return Task.CompletedTask;
        }

        public Task EnsureAppReadyAsync(CancellationToken ct = default)
        {
            ActionLogs.Add("EnsureAppReady");
            return Task.CompletedTask;
        }

        public Task NavigateHomeAsync(CancellationToken ct = default)
        {
            ActionLogs.Add("NavigateHome");
            return Task.CompletedTask;
        }

        public Task SearchAsync(string keyword, CancellationToken ct = default)
        {
            ActionLogs.Add($"Search:{keyword}");
            return Task.CompletedTask;
        }

        public Task<bool> SmartScrollAndSelectVideoAsync(ReportTargetItem target, CancellationToken ct = default)
        {
            ActionLogs.Add($"SelectVideo:{target.Keyword}");
            return Task.FromResult(true);
        }

        public Task OpenVideoByUrlAsync(string url, CancellationToken ct = default)
        {
            ActionLogs.Add($"OpenUrl:{url}");
            return Task.CompletedTask;
        }

        public Task OpenChannelPageAsync(string channelUrlOrName, CancellationToken ct = default)
        {
            ActionLogs.Add($"OpenChannel:{channelUrlOrName}");
            return Task.CompletedTask;
        }

        public Task WatchVideoWithNaturalEngagementAsync(TimeSpan duration, NaturalEngagementOptions options, CancellationToken ct = default)
        {
            ActionLogs.Add($"Watch:{duration.TotalSeconds}s");
            return Task.CompletedTask;
        }

        public Task<bool> OpenReportMenuAsync(CancellationToken ct = default)
        {
            ActionLogs.Add("OpenReportMenu");
            return Task.FromResult(true);
        }

        public Task<bool> SubmitVideoReportAsync(ReportReasonConfig reasonConfig, CancellationToken ct = default)
        {
            ActionLogs.Add($"SubmitVideoReport:{reasonConfig.Category}");
            return Task.FromResult(true);
        }

        public Task<bool> SubmitChannelReportAsync(ReportReasonConfig reasonConfig, CancellationToken ct = default)
        {
            ActionLogs.Add($"SubmitChannelReport:{reasonConfig.Category}");
            return Task.FromResult(true);
        }

        public Task<bool> VerifyReportSubmittedAsync(CancellationToken ct = default)
        {
            ActionLogs.Add("VerifyReport");
            return Task.FromResult(true);
        }

        public Task<string> CaptureScreenshotAsync(string stepPrefix, CancellationToken ct = default)
        {
            ActionLogs.Add($"Screenshot:{stepPrefix}");
            return Task.FromResult($"proof_{stepPrefix}.png");
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact]
    public async Task SearchAndReportScenario_ExecutesAllStepsSuccessfully()
    {
        var driver = new MockYouTubeReportDriver();
        var target = new ReportTargetItem("funny cats", "Cats Channel", "Funny Cat Fails 2026");
        var config = ReportScenarioConfig.CreateStandardSearchConfig();
        config.MinWatchDurationSeconds = 1;
        config.MaxWatchDurationSeconds = 2;

        var steps = ScenarioDefinitions.CreateSearchAndReportVideoScenario(target, config);
        var engine = new WorkflowEngine();
        var context = new WorkflowContext();

        var results = await engine.ExecuteScenarioAsync(driver, steps, context);

        Assert.Equal(steps.Count, results.Count);
        Assert.All(results, r => Assert.True(r.IsSuccess));
        Assert.Equal(1, target.ReportsCompleted);
        Assert.NotNull(target.LastReportedAt);
        Assert.Contains(driver.ActionLogs, a => a.StartsWith("Search:funny cats"));
        Assert.Contains(driver.ActionLogs, a => a == "OpenReportMenu");
        Assert.Contains(driver.ActionLogs, a => a.StartsWith("SubmitVideoReport:"));
    }

    [Fact]
    public async Task WorkflowEngine_StopsOnFailure()
    {
        var driver = new MockYouTubeReportDriver();
        var steps = new IWorkflowStep[]
        {
            new DelegateWorkflowStep("Step 1", (d, ctx, ct) => Task.FromResult(ReportStepResult.Success("Step 1", "OK"))),
            new DelegateWorkflowStep("Step 2", (d, ctx, ct) => Task.FromResult(ReportStepResult.Failure("Step 2", "Failed!"))),
            new DelegateWorkflowStep("Step 3", (d, ctx, ct) => Task.FromResult(ReportStepResult.Success("Step 3", "Should not run")))
        };

        var engine = new WorkflowEngine();
        var results = await engine.ExecuteScenarioAsync(driver, steps, new WorkflowContext());

        Assert.Equal(2, results.Count);
        Assert.True(results[0].IsSuccess);
        Assert.False(results[1].IsSuccess);
    }
}
