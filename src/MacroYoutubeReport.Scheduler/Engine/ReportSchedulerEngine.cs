using System.Collections.Concurrent;
using System.Threading.Channels;
using MacroYoutubeReport.Core.Drivers;
using MacroYoutubeReport.Core.Enums;
using MacroYoutubeReport.Core.Models;
using MacroYoutubeReport.Core.Workflows;
using MacroYoutubeReport.Scheduler.Accounts;
using MacroYoutubeReport.Scheduler.Journal;
using MacroYoutubeReport.Scheduler.Proxy;

namespace MacroYoutubeReport.Scheduler.Engine;

/// <summary>
/// Động cơ điều phối và thực thi tác vụ báo cáo đa thiết bị, đa luồng:
/// - Quản lý hàng đợi bất đồng bộ (Channel)
/// - Giới hạn luồng chạy song song (Concurrency Gate)
/// - Đảm bảo tính độc quyền thiết bị (Exclusive Device Lock)
/// - Tự động liên kết tài khoản, proxy và ghi nhật ký SQLite
/// </summary>
public sealed class ReportSchedulerEngine : IAsyncDisposable
{
    private readonly IExecutionJournal _journal;
    private readonly IAccountManager _accountManager;
    private readonly IProxyRotator _proxyRotator;
    private readonly IDriverFactory _driverFactory;
    private readonly WorkflowEngine _workflowEngine = new();

    private readonly Channel<ReportTaskRequest> _channel;
    private readonly ConcurrentDictionary<string, ReportTaskRequest> _pendingTasks = new();
    private readonly ConcurrentDictionary<string, ReportTaskRequest> _runningTasks = new();
    private readonly ConcurrentDictionary<string, bool> _activeDeviceLocks = new(StringComparer.OrdinalIgnoreCase);

    private readonly SemaphoreSlim _concurrencyGate;
    private readonly int _maxConcurrency;
    private CancellationTokenSource? _engineCts;
    private Task? _dispatcherTask;
    private bool _isStarted;
    private readonly object _stateLock = new();

    public int MaxConcurrency => _maxConcurrency;
    public int PendingCount => _pendingTasks.Count;
    public int RunningCount => _runningTasks.Count;
    public bool IsStarted => _isStarted;

    public IExecutionJournal Journal => _journal;
    public IAccountManager AccountManager => _accountManager;
    public IProxyRotator ProxyRotator => _proxyRotator;

    public event Action<ReportTaskRequest>? OnTaskQueued;
    public event Action<ReportTaskRequest, string>? OnTaskStarted;
    public event Action<ReportTaskRequest, ReportStepResult>? OnStepProgress;
    public event Action<ReportTaskRecord>? OnTaskCompleted;
    public event Action<int, int>? OnQueueStatusChanged;

    public ReportSchedulerEngine(
        IExecutionJournal? journal = null,
        IAccountManager? accountManager = null,
        IProxyRotator? proxyRotator = null,
        IDriverFactory? driverFactory = null,
        int maxConcurrency = 4)
    {
        _maxConcurrency = Math.Max(1, maxConcurrency);
        _concurrencyGate = new SemaphoreSlim(_maxConcurrency, 64);
        _journal = journal ?? new SqliteExecutionJournal();
        _accountManager = accountManager ?? new AccountManager();
        _proxyRotator = proxyRotator ?? new ProxyRotator();
        _driverFactory = driverFactory ?? new DefaultDriverFactory();

        _channel = Channel.CreateUnbounded<ReportTaskRequest>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        lock (_stateLock)
        {
            if (_isStarted) return;
            _isStarted = true;
        }

        await _journal.InitializeAsync(ct).ConfigureAwait(false);
        await _accountManager.LoadAccountsAsync(ct: ct).ConfigureAwait(false);

        _engineCts = new CancellationTokenSource();
        _dispatcherTask = Task.Run(() => DispatcherLoopAsync(_engineCts.Token), CancellationToken.None);
    }

    public async Task StopAsync()
    {
        lock (_stateLock)
        {
            if (!_isStarted) return;
            _isStarted = false;
        }

        _engineCts?.Cancel();
        if (_dispatcherTask != null)
        {
            try
            {
                await _dispatcherTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Bỏ qua lỗi hủy luồng ngầm
            }
        }
    }

    public bool EnqueueTask(ReportTaskRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_pendingTasks.TryAdd(request.TaskId, request))
        {
            _channel.Writer.TryWrite(request);
            OnTaskQueued?.Invoke(request);
            NotifyQueueStatusChanged();
            return true;
        }

        return false;
    }

    public int EnqueueBatch(IEnumerable<ReportTargetItem> targets, ReportScenarioConfig? config = null, PlatformType platform = PlatformType.Android)
    {
        ArgumentNullException.ThrowIfNull(targets);
        int count = 0;
        foreach (var t in targets)
        {
            var req = new ReportTaskRequest(t, config)
            {
                PreferredPlatform = platform
            };
            if (EnqueueTask(req)) count++;
        }
        return count;
    }

    private async Task DispatcherLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var request = await _channel.Reader.ReadAsync(ct).ConfigureAwait(false);
                await _concurrencyGate.WaitAsync(ct).ConfigureAwait(false);

                // Chạy ngầm nhiệm vụ trong 1 task độc lập
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessRequestAsync(request, ct).ConfigureAwait(false);
                    }
                    finally
                    {
                        _concurrencyGate.Release();
                        NotifyQueueStatusChanged();
                    }
                }, CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                await Task.Delay(500, ct).ConfigureAwait(false);
            }
        }
    }

    private async Task ProcessRequestAsync(ReportTaskRequest request, CancellationToken ct)
    {
        _pendingTasks.TryRemove(request.TaskId, out _);
        _runningTasks[request.TaskId] = request;
        NotifyQueueStatusChanged();

        string deviceId = request.AssignedDeviceIdOrProfile ?? "DefaultDevice";

        // Khóa thiết bị độc quyền
        while (!_activeDeviceLocks.TryAdd(deviceId, true))
        {
            await Task.Delay(1000, ct).ConfigureAwait(false);
        }

        var record = new ReportTaskRecord
        {
            TaskId = request.TaskId,
            TargetId = request.Target.Id,
            TargetNameOrUrl = !string.IsNullOrWhiteSpace(request.Target.VideoTitle) ? request.Target.VideoTitle : request.Target.Keyword,
            DeviceSerialOrProfile = deviceId,
            Platform = request.PreferredPlatform,
            Status = ExecutionStatus.Running,
            CreatedAt = DateTime.UtcNow
        };

        var reason = request.ScenarioConfig.GetNextReason();
        record.Category = reason.Category;
        record.ReasonDetails = reason.CustomDetails;

        OnTaskStarted?.Invoke(request, deviceId);

        var proxy = _proxyRotator.GetNextProxy();
        await using var driver = _driverFactory.CreateDriver(request.PreferredPlatform, deviceId, proxy);

        var steps = (request.Target.TargetType, request.ScenarioConfig.ScenarioName.Contains("Livestream", StringComparison.OrdinalIgnoreCase)) switch
        {
            (ReportTargetType.LiveStream, _) => ScenarioDefinitions.CreateLiveStreamFinancialScamScenario(request.Target, request.ScenarioConfig),
            (_, true) => ScenarioDefinitions.CreateLiveStreamFinancialScamScenario(request.Target, request.ScenarioConfig),
            (ReportTargetType.Channel, _) => ScenarioDefinitions.CreateChannelReportScenario(request.Target, request.ScenarioConfig),
            (ReportTargetType.Shorts, _) => ScenarioDefinitions.CreateShortsReportScenario(request.Target, request.ScenarioConfig),
            _ => !string.IsNullOrWhiteSpace(request.Target.TargetUrl) && string.IsNullOrWhiteSpace(request.Target.Keyword)
                ? ScenarioDefinitions.CreateDirectUrlReportVideoScenario(request.Target, request.ScenarioConfig)
                : ScenarioDefinitions.CreateSearchAndReportVideoScenario(request.Target, request.ScenarioConfig)
        };

        var context = new WorkflowContext
        {
            CurrentTarget = request.Target,
            CurrentScenario = request.ScenarioConfig,
            CurrentReason = reason
        };

        using var taskCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        taskCts.CancelAfter(request.Timeout);

        try
        {
            // Lắng nghe tiến độ từng bước
            void StepHandler(ReportStepResult s) => OnStepProgress?.Invoke(request, s);
            _workflowEngine.OnStepCompleted += StepHandler;

            IReadOnlyList<ReportStepResult> results;
            try
            {
                results = await _workflowEngine.ExecuteScenarioAsync(driver, steps, context, taskCts.Token).ConfigureAwait(false);
            }
            finally
            {
                _workflowEngine.OnStepCompleted -= StepHandler;
            }

            bool allSuccess = results.Count > 0 && results.All(r => r.IsSuccess);
            record.Status = allSuccess ? ExecutionStatus.Succeeded : ExecutionStatus.Failed;
            record.CompletedAt = DateTime.UtcNow;

            var lastResult = results.LastOrDefault();
            if (lastResult != null)
            {
                record.ScreenshotProofPath = lastResult.ScreenshotPath;
                if (!allSuccess) record.ErrorMessage = lastResult.Message;
            }

            // Ghi nhận tài khoản sử dụng thành công và kích hoạt cooldown
            var acc = _accountManager.GetAvailableAccountForDevice(deviceId);
            if (acc != null)
            {
                _accountManager.RecordReportCompleted(acc.Id, request.ScenarioConfig.CooldownBetweenReportsSeconds);
            }
        }
        catch (Exception ex)
        {
            record.Status = ExecutionStatus.Failed;
            record.ErrorMessage = ex.Message;
            record.CompletedAt = DateTime.UtcNow;
        }
        finally
        {
            _activeDeviceLocks.TryRemove(deviceId, out _);
            _runningTasks.TryRemove(request.TaskId, out _);
            await _journal.RecordTaskAsync(record, CancellationToken.None).ConfigureAwait(false);
            OnTaskCompleted?.Invoke(record);
        }
    }

    private void NotifyQueueStatusChanged()
    {
        OnQueueStatusChanged?.Invoke(_pendingTasks.Count, _runningTasks.Count);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _concurrencyGate.Dispose();
        await _journal.DisposeAsync().ConfigureAwait(false);
    }
}
