using MacroYoutubeReport.Core.Enums;
using MacroYoutubeReport.Core.Models;
using MacroYoutubeReport.Driver.Android.Adb;
using MacroYoutubeReport.Scheduler.Accounts;
using MacroYoutubeReport.Scheduler.Engine;
using MacroYoutubeReport.Scheduler.Journal;
using MacroYoutubeReport.Scheduler.Proxy;
using MacroYoutubeReport.UI.WinForms.Presenters;
using MacroYoutubeReport.UI.WinForms.Views;

namespace MacroYoutubeReport.Tests;

public class MainPresenterTests
{
    private sealed class MockMainView : IMainView
    {
        public PlatformType SelectedPlatform { get; set; } = PlatformType.Android;
        public int ConcurrencyLimit { get; set; } = 3;
        public bool IsRunning { get; private set; }

        public List<ReportTargetItem> Targets { get; } = new();
        public ReportScenarioConfig ScenarioConfig { get; set; } = ReportScenarioConfig.CreateStandardSearchConfig();

        private readonly object _logLock = new();
        private readonly List<string> _logs = new();
        public IReadOnlyList<string> Logs
        {
            get
            {
                lock (_logLock)
                {
                    return _logs.ToList();
                }
            }
        }
        public (int Pending, int Running, int Completed, int Failed) LastCounters { get; private set; }
        public IReadOnlyList<ReportTaskRecord> HistoryRecords { get; private set; } = Array.Empty<ReportTaskRecord>();

        public event Func<Task>? ViewLoaded;
#pragma warning disable CS0067
        public event Func<Task>? ScanDevicesRequested;
        public event Func<Task>? StartBatchRequested;
        public event Func<ReportTargetItem, Task>? StartSingleTargetRequested;
        public event Action? StopBatchRequested;
        public event Func<Task>? RefreshHistoryRequested;
        public event Action<string>? ExportHistoryCsvRequested;
        public event Func<Task>? AddAccountRequested;
        public event Func<Task>? AddProxyRequested;
#pragma warning restore CS0067

        public IReadOnlyList<ReportTargetItem> GetTargetItems() => Targets;
        public void SetTargetItems(IReadOnlyList<ReportTargetItem> targets)
        {
            Targets.Clear();
            Targets.AddRange(targets);
        }
        public void AddTargetItem(ReportTargetItem target) => Targets.Add(target);
        public void RemoveSelectedTarget() { if (Targets.Count > 0) Targets.RemoveAt(0); }
        public void ClearAllTargets() => Targets.Clear();
        public void RefreshTasksDisplay() { }

        public List<string> SelectedDevices { get; } = new() { "mock-device-01" };
        public IReadOnlyList<string> GetSelectedDeviceSerials() => SelectedDevices;

        public ReportScenarioConfig GetScenarioConfig() => ScenarioConfig;
        public void SetScenarioConfig(ReportScenarioConfig config) => ScenarioConfig = config;

        public void SetDevices(IReadOnlyList<ManagedAndroidDevice> devices) { }
        public void UpdateDeviceRunningStatus(string deviceSerial, string targetName, string status) { }
        public void SetChromeProfiles(IReadOnlyList<string> profiles) { }
        public void SetAccounts(IReadOnlyList<AccountProfile> accounts) { }
        public void SetProxies(IReadOnlyList<ProxyConfig> proxies) { }
        public void SetHistoryRecords(IReadOnlyList<ReportTaskRecord> records) => HistoryRecords = records;

        public void UpdateQueueStatus(int pendingCount, int runningCount, int completedCount, int failedCount)
        {
            LastCounters = (pendingCount, runningCount, completedCount, failedCount);
        }

        public void AppendLog(string message, string level = "INFO", string? deviceSerial = null)
        {
            lock (_logLock)
            {
                string dev = string.IsNullOrWhiteSpace(deviceSerial) ? "" : $"[{deviceSerial}] ";
                _logs.Add($"[{level}] {dev}{message}");
            }
        }
        public void SetRunningState(bool isRunning) => IsRunning = isRunning;
        public void ShowNotification(string message, bool isError = false) { }

        public async Task TriggerViewLoadedAsync() { if (ViewLoaded != null) await ViewLoaded(); }
        public async Task TriggerStartBatchAsync() { if (StartBatchRequested != null) await StartBatchRequested(); }
        public void TriggerStopBatch() => StopBatchRequested?.Invoke();
    }

    [Fact]
    public async Task MainPresenter_StartsAndEnqueuesBatchSuccessfully()
    {
        string tempDb = Path.Combine(Path.GetTempPath(), $"ui_test_{Guid.NewGuid():N}.db");
        try
        {
            var journal = new SqliteExecutionJournal(tempDb);
            var accountManager = new AccountManager();
            var proxyRotator = new ProxyRotator();
            var engine = new ReportSchedulerEngine(journal, accountManager, proxyRotator, maxConcurrency: 2);

            var view = new MockMainView();
            var presenter = new MainPresenter(view, engine, journal, accountManager, proxyRotator);

            await view.TriggerViewLoadedAsync();
            Assert.Contains(view.Logs, l => l.Contains("Hệ thống đã sẵn sàng"));

            view.AddTargetItem(new ReportTargetItem("cat videos", "FunnyCats", "Cute cats 2026"));
            await view.TriggerStartBatchAsync();

            Assert.True(view.IsRunning);
            Assert.Contains(view.Logs, l => l.Contains("Đã xếp 1 tác vụ vào hàng đợi"));

            view.TriggerStopBatch();
            Assert.False(view.IsRunning);
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
