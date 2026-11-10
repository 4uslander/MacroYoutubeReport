using MacroYoutubeReport.Core.Enums;
using MacroYoutubeReport.Core.Models;
using MacroYoutubeReport.Driver.Android.Adb;
using MacroYoutubeReport.Scheduler.Accounts;
using MacroYoutubeReport.Scheduler.Proxy;

namespace MacroYoutubeReport.UI.WinForms.Views;

/// <summary>
/// Giao diện View cho màn hình chính của ứng dụng Macro YouTube Report (MVP Pattern - Unified Single Dashboard).
/// </summary>
public interface IMainView
{
    // Cấu hình Nền tảng & Điều khiển chung
    PlatformType SelectedPlatform { get; }
    int ConcurrencyLimit { get; set; }
    bool IsRunning { get; }

    // Dữ liệu Mục tiêu & Kịch bản
    IReadOnlyList<ReportTargetItem> GetTargetItems();
    void SetTargetItems(IReadOnlyList<ReportTargetItem> targets);
    void AddTargetItem(ReportTargetItem target);
    void RemoveSelectedTarget();
    void ClearAllTargets();
    void RefreshTasksDisplay();

    // Danh sách thiết bị được chọn
    IReadOnlyList<string> GetSelectedDeviceSerials();

    ReportScenarioConfig GetScenarioConfig();
    void SetScenarioConfig(ReportScenarioConfig config);

    // Sự kiện người dùng
    event Func<Task>? ViewLoaded;
    event Func<Task>? ScanDevicesRequested;
    event Func<Task>? StartBatchRequested;
    event Func<ReportTargetItem, Task>? StartSingleTargetRequested;
    event Action? StopBatchRequested;
    event Func<Task>? RefreshHistoryRequested;
    event Action<string>? ExportHistoryCsvRequested;
    event Func<Task>? AddAccountRequested;
    event Func<Task>? AddProxyRequested;

    // Cập nhật giao diện từ Presenter
    void SetDevices(IReadOnlyList<ManagedAndroidDevice> devices);
    void UpdateDeviceRunningStatus(string deviceSerial, string targetName, string status);
    void SetChromeProfiles(IReadOnlyList<string> profiles);
    void SetAccounts(IReadOnlyList<AccountProfile> accounts);
    void SetProxies(IReadOnlyList<ProxyConfig> proxies);
    void SetHistoryRecords(IReadOnlyList<ReportTaskRecord> records);

    void UpdateQueueStatus(int pendingCount, int runningCount, int completedCount, int failedCount);
    void AppendLog(string message, string level = "INFO");
    void SetRunningState(bool isRunning);
    void ShowNotification(string message, bool isError = false);
}
