using MacroYoutubeReport.Core.Drivers;
using MacroYoutubeReport.Core.Enums;
using MacroYoutubeReport.Core.Models;
using MacroYoutubeReport.Scheduler.Accounts;
using MacroYoutubeReport.Scheduler.Engine;
using MacroYoutubeReport.Scheduler.Journal;
using MacroYoutubeReport.Scheduler.Proxy;

namespace MacroYoutubeReport.Tests;

public class ReportSchedulerEngineTests
{
    private sealed class MockFastDriver : IYouTubeReportDriver
    {
        public PlatformType Platform { get; } = PlatformType.Android;
        public string TargetId { get; }

        public MockFastDriver(string targetId) => TargetId = targetId;

        public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task EnsureAppReadyAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task EnsureNoAdPlayingAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task NavigateHomeAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task SearchAsync(string keyword, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> SmartScrollAndSelectVideoAsync(ReportTargetItem target, CancellationToken ct = default) => Task.FromResult(true);
        public Task OpenVideoByUrlAsync(string url, CancellationToken ct = default) => Task.CompletedTask;
        public Task OpenChannelPageAsync(string channelUrlOrName, CancellationToken ct = default) => Task.CompletedTask;
        public Task WatchVideoWithNaturalEngagementAsync(TimeSpan duration, NaturalEngagementOptions options, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> OpenReportMenuAsync(CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> SubmitVideoReportAsync(ReportReasonConfig reasonConfig, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> SubmitChannelReportAsync(ReportReasonConfig reasonConfig, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> SubmitShortsReportAsync(ReportReasonConfig reasonConfig, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> SubmitLiveStreamReportAsync(ReportReasonConfig reasonConfig, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> SubmitCommentReportAsync(string commentAuthorOrKeyword, ReportReasonConfig reasonConfig, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> VerifyReportSubmittedAsync(CancellationToken ct = default) => Task.FromResult(true);
        public Task<string> CaptureScreenshotAsync(string stepPrefix, CancellationToken ct = default) => Task.FromResult("shot.png");
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class MockDriverFactory : IDriverFactory
    {
        public IYouTubeReportDriver CreateDriver(PlatformType platform, string targetId, ProxyConfig? proxy = null)
        {
            return new MockFastDriver(targetId);
        }
    }

    [Fact]
    public async Task ReportSchedulerEngine_ExecutesQueuedTaskSuccessfully()
    {
        string tempDb = Path.Combine(Path.GetTempPath(), $"engine_test_{Guid.NewGuid():N}.db");
        try
        {
            var journal = new SqliteExecutionJournal(tempDb);
            var accountManager = new AccountManager();
            var proxyRotator = new ProxyRotator();
            var driverFactory = new MockDriverFactory();

            await using var engine = new ReportSchedulerEngine(journal, accountManager, proxyRotator, driverFactory, maxConcurrency: 2);
            await engine.StartAsync();

            var target = new ReportTargetItem("game live", "GameChannel", "Live Gameplay 2026");
            var config = ReportScenarioConfig.CreateStandardSearchConfig();
            config.MinWatchDurationSeconds = 1;
            config.MaxWatchDurationSeconds = 1;
            config.CooldownBetweenReportsSeconds = 0;

            var request = new ReportTaskRequest(target, config)
            {
                AssignedDeviceIdOrProfile = "mock-device-A"
            };

            var completedTcs = new TaskCompletionSource<ReportTaskRecord>();
            engine.OnTaskCompleted += record =>
            {
                if (record.TaskId == request.TaskId)
                {
                    completedTcs.TrySetResult(record);
                }
            };

            engine.EnqueueTask(request);

            var completedRecord = await Task.WhenAny(completedTcs.Task, Task.Delay(10000));
            Assert.Same(completedTcs.Task, completedRecord);

            var record = await completedTcs.Task;
            Assert.Equal(ExecutionStatus.Succeeded, record.Status);
            Assert.Equal("mock-device-A", record.DeviceSerialOrProfile);

            var recent = await journal.GetRecentRecordsAsync(5);
            Assert.NotEmpty(recent);
        }
        finally
        {
            if (File.Exists(tempDb))
            {
                try { File.Delete(tempDb); } catch { }
            }
        }
    }
}
