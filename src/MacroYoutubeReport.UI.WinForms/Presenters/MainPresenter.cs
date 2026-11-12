using MacroYoutubeReport.Core.Enums;
using MacroYoutubeReport.Core.Models;
using MacroYoutubeReport.Driver.Android.Adb;
using MacroYoutubeReport.Scheduler.Accounts;
using MacroYoutubeReport.Scheduler.Engine;
using MacroYoutubeReport.Scheduler.Journal;
using MacroYoutubeReport.Scheduler.Proxy;
using MacroYoutubeReport.UI.WinForms.Views;

namespace MacroYoutubeReport.UI.WinForms.Presenters;

/// <summary>
/// Presenter điều phối toàn bộ logic giữa giao diện người dùng (IMainView) và tầng Lập lịch (ReportSchedulerEngine).
/// </summary>
public sealed class MainPresenter
{
    private readonly IMainView _view;
    private readonly ReportSchedulerEngine _engine;
    private readonly IExecutionJournal _journal;
    private readonly IAccountManager _accountManager;
    private readonly IProxyRotator _proxyRotator;

    private int _completedCount = 0;
    private int _failedCount = 0;

    public MainPresenter(
        IMainView view,
        ReportSchedulerEngine engine,
        IExecutionJournal journal,
        IAccountManager accountManager,
        IProxyRotator proxyRotator)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _journal = journal ?? throw new ArgumentNullException(nameof(journal));
        _accountManager = accountManager ?? throw new ArgumentNullException(nameof(accountManager));
        _proxyRotator = proxyRotator ?? throw new ArgumentNullException(nameof(proxyRotator));

        BindEvents();
    }

    private void BindEvents()
    {
        _view.ViewLoaded += OnViewLoadedAsync;
        _view.ScanDevicesRequested += OnScanDevicesRequestedAsync;
        _view.StartBatchRequested += OnStartBatchRequestedAsync;
        _view.StartSingleTargetRequested += OnStartSingleTargetRequestedAsync;
        _view.StopBatchRequested += OnStopBatchRequested;
        _view.RefreshHistoryRequested += OnRefreshHistoryRequestedAsync;
        _view.ExportHistoryCsvRequested += OnExportHistoryCsvRequested;

        // Lắng nghe sự kiện từ Engine
        _engine.OnTaskQueued += req =>
        {
            UpdateStatusCounters();
        };

        _engine.OnTaskStarted += (req, deviceId) =>
        {
            string targetName = req.Target.VideoTitle ?? req.Target.Keyword;
            _view.AppendLog($"Bắt đầu xử lý: {targetName}", "INFO", deviceId);
            _view.UpdateDeviceRunningStatus(deviceId, targetName, "Đang chạy");
            UpdateStatusCounters();
        };

        _engine.OnStepProgress += (req, step) =>
        {
            string level = step.IsSuccess ? "INFO" : "WARN";
            _view.AppendLog($"[{step.StepName}] {step.Message}", level, req.AssignedDeviceIdOrProfile);
        };

        _engine.OnTaskCompleted += record =>
        {
            if (record.Status == ExecutionStatus.Succeeded)
            {
                Interlocked.Increment(ref _completedCount);
                _view.AppendLog($"Thành công: Đã báo cáo mục tiêu [{record.TargetNameOrUrl}]", "SUCCESS", record.DeviceSerialOrProfile);
            }
            else
            {
                Interlocked.Increment(ref _failedCount);
                _view.AppendLog($"Thất bại: [{record.TargetNameOrUrl}] - Lỗi: {record.ErrorMessage}", "ERROR", record.DeviceSerialOrProfile);
            }

            _view.UpdateDeviceRunningStatus(record.DeviceSerialOrProfile, "Đang rảnh", record.Status == ExecutionStatus.Succeeded ? "Hoàn thành" : "Thất bại");
            UpdateStatusCounters();
            ScheduleThrottledGridRefresh();
        };

        _engine.OnQueueStatusChanged += (pending, running) =>
        {
            UpdateStatusCounters();
            if (pending == 0 && running == 0 && _view.IsRunning)
            {
                _view.SetRunningState(false);
                _view.AppendLog("Đã hoàn tất tất cả mục tiêu trong hàng đợi.", "SUCCESS");
            }
        };
    }

    private async Task OnViewLoadedAsync()
    {
        try
        {
            _view.AppendLog("Đang khởi động hệ thống Macro YouTube Report...", "INFO");

            await _engine.StartAsync().ConfigureAwait(false);
            await ScanDevicesAsync().ConfigureAwait(false);
            LoadChromeProfiles();
            LoadAccountsAndProxies();
            await RefreshHistoryDataAsync().ConfigureAwait(false);

            _view.AppendLog("Hệ thống đã sẵn sàng.", "INFO");
        }
        catch (Exception ex)
        {
            _view.AppendLog($"Cảnh báo khởi tạo: {ex.Message}", "WARN");
        }
    }

    private async Task OnScanDevicesRequestedAsync()
    {
        _view.AppendLog("Đang quét danh sách thiết bị Android qua ADB...", "INFO");
        await ScanDevicesAsync().ConfigureAwait(false);
        _view.AppendLog("Quét thiết bị hoàn tất.", "INFO");
    }

    private async Task ScanDevicesAsync()
    {
        try
        {
            var devices = await AdbDeviceManager.Instance.ScanDevicesAsync().ConfigureAwait(false);
            _view.SetDevices(devices);
        }
        catch (Exception ex)
        {
            _view.AppendLog($"Lỗi khi quét thiết bị ADB: {ex.Message}", "WARN");
        }
    }

    private void LoadChromeProfiles()
    {
        var profiles = new List<string> { "Default", "Profile 1", "Profile 2", "Profile 3" };
        string baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MacroYoutubeReport", "ChromeProfiles");
        if (Directory.Exists(baseDir))
        {
            foreach (var dir in Directory.GetDirectories(baseDir))
            {
                string name = Path.GetFileName(dir);
                if (!profiles.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    profiles.Add(name);
                }
            }
        }
        _view.SetChromeProfiles(profiles);
    }

    private void LoadAccountsAndProxies()
    {
        var accounts = _accountManager.GetAllAccounts();
        _view.SetAccounts(accounts);

        var proxies = _proxyRotator.GetAllProxies();
        _view.SetProxies(proxies);
    }

    private async Task OnStartBatchRequestedAsync()
    {
        var targets = _view.GetTargetItems();
        if (targets.Count == 0)
        {
            _view.ShowNotification("Vui lòng thêm ít nhất một mục tiêu trước khi bắt đầu.", isError: true);
            return;
        }

        var config = _view.GetScenarioConfig();
        var platform = _view.SelectedPlatform;
        var selectedDevices = _view.GetSelectedDeviceSerials();

        if (platform == PlatformType.Android && selectedDevices.Count == 0)
        {
            _view.ShowNotification("Vui lòng tích chọn ít nhất một thiết bị Android để thực thi.", isError: true);
            return;
        }

        if (!_engine.IsStarted)
        {
            await _engine.StartAsync().ConfigureAwait(false);
        }

        _completedCount = 0;
        _failedCount = 0;

        foreach (var serial in selectedDevices)
        {
            _view.UpdateDeviceRunningStatus(serial, "Đang rảnh", "Online");
        }

        _view.SetRunningState(true);
        _view.AppendLog($"Bắt đầu thực thi hàng loạt {targets.Count} mục tiêu trên {(selectedDevices.Count > 0 ? selectedDevices.Count + " thiết bị" : platform.ToString())}...", "INFO");

        int queued = _engine.EnqueueBatchWithDevices(targets, selectedDevices, config, platform);
        _view.AppendLog($"Đã xếp {queued} tác vụ vào hàng đợi cho các thiết bị đã chọn.", "INFO");
    }

    private async Task OnStartSingleTargetRequestedAsync(ReportTargetItem target)
    {
        if (target == null) return;

        var config = _view.GetScenarioConfig();
        var platform = _view.SelectedPlatform;
        var selectedDevices = _view.GetSelectedDeviceSerials();

        if (platform == PlatformType.Android && selectedDevices.Count == 0)
        {
            _view.ShowNotification("Vui lòng tích chọn ít nhất một thiết bị Android để thực thi mục tiêu này.", isError: true);
            return;
        }

        if (!_engine.IsStarted)
        {
            await _engine.StartAsync().ConfigureAwait(false);
        }

        foreach (var serial in selectedDevices)
        {
            _view.UpdateDeviceRunningStatus(serial, "Đang rảnh", "Online");
        }

        _view.SetRunningState(true);
        string targetTitle = target.VideoTitle ?? target.Keyword;
        _view.AppendLog($"Bắt đầu mục tiêu [{targetTitle}] trên {(selectedDevices.Count > 0 ? selectedDevices.Count + " thiết bị" : platform.ToString())}...", "INFO");

        int queued = _engine.EnqueueBatchWithDevices(new[] { target }, selectedDevices, config, platform);
        _view.AppendLog($"Đã xếp {queued} tác vụ vào hàng đợi.", "INFO");
    }

    private void OnStopBatchRequested()
    {
        _view.AppendLog("Đang dừng toàn bộ tác vụ...", "WARN");
        _ = _engine.StopAsync();
        _view.SetRunningState(false);
        var selectedDevices = _view.GetSelectedDeviceSerials();
        foreach (var serial in selectedDevices)
        {
            _view.UpdateDeviceRunningStatus(serial, "Đang rảnh", "Đã dừng");
        }
        _view.AppendLog("Đã dừng hàng đợi tác vụ.", "WARN");
    }

    private async Task OnRefreshHistoryRequestedAsync()
    {
        await RefreshHistoryDataAsync().ConfigureAwait(false);
    }

    private async Task RefreshHistoryDataAsync()
    {
        try
        {
            var records = await _journal.GetRecentRecordsAsync(150).ConfigureAwait(false);
            _view.SetHistoryRecords(records);
        }
        catch (Exception ex)
        {
            _view.AppendLog($"Lỗi tải lịch sử: {ex.Message}", "WARN");
        }
    }

    private void OnExportHistoryCsvRequested(string filePath)
    {
        try
        {
            var records = _journal.GetRecentRecordsAsync(1000).GetAwaiter().GetResult();
            using var writer = new StreamWriter(filePath);
            writer.WriteLine("Id,TaskId,Target,Device,Platform,Category,Status,WatchSeconds,Screenshot,Error,CreatedAt");
            foreach (var r in records)
            {
                writer.WriteLine($"{r.Id},{r.TaskId},\"{r.TargetNameOrUrl}\",{r.DeviceSerialOrProfile},{r.Platform},{r.Category},{r.Status},{r.WatchDurationSeconds},\"{r.ScreenshotProofPath}\",\"{r.ErrorMessage}\",{r.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            }
            _view.ShowNotification("Xuất báo cáo CSV thành công!");
        }
        catch (Exception ex)
        {
            _view.ShowNotification($"Lỗi xuất file CSV: {ex.Message}", isError: true);
        }
    }

    private int _isRefreshPending = 0;
    private void ScheduleThrottledGridRefresh()
    {
        if (Interlocked.Exchange(ref _isRefreshPending, 1) == 0)
        {
            Task.Delay(1000).ContinueWith(_ =>
            {
                Interlocked.Exchange(ref _isRefreshPending, 0);
                _view.RefreshTasksDisplay();
                _ = RefreshHistoryDataAsync();
            });
        }
    }

    private void UpdateStatusCounters()
    {
        _view.UpdateQueueStatus(_engine.PendingCount, _engine.RunningCount, _completedCount, _failedCount);
    }
}
