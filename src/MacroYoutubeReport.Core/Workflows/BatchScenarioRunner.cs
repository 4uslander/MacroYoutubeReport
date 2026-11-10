using MacroYoutubeReport.Core.Drivers;
using MacroYoutubeReport.Core.Enums;
using MacroYoutubeReport.Core.Models;

namespace MacroYoutubeReport.Core.Workflows;

/// <summary>
/// Tổng kết kết quả sau khi chạy một đợt kịch bản báo cáo hàng loạt (Batch).
/// </summary>
public sealed record BatchExecutionSummary
{
    public int TotalTargets { get; init; }
    public int SuccessfulReports { get; init; }
    public int FailedReports { get; init; }
    public TimeSpan TotalDuration { get; init; }
    public IReadOnlyList<ReportTaskRecord> Records { get; init; } = Array.Empty<ReportTaskRecord>();
}

/// <summary>
/// Điều phối và thực thi hàng loạt các mục tiêu theo kịch bản định trước,
/// xử lý thời gian nghỉ cooldown, xoay vòng lý do và báo cáo tiến trình.
/// </summary>
public sealed class BatchScenarioRunner
{
    private readonly WorkflowEngine _engine = new();

    public event Action<ReportTargetItem>? OnTargetStarted;
    public event Action<ReportStepResult>? OnStepCompleted;
    public event Action<ReportTargetItem, bool, string?>? OnTargetFinished;
    public event Action<int>? OnCooldownTick;

    public BatchScenarioRunner()
    {
        _engine.OnStepCompleted += result => OnStepCompleted?.Invoke(result);
    }

    /// <summary>
    /// Chạy danh sách các mục tiêu tuần tự trên driver được chỉ định.
    /// </summary>
    public async Task<BatchExecutionSummary> RunBatchAsync(
        IYouTubeReportDriver driver,
        IReadOnlyList<ReportTargetItem> targets,
        ReportScenarioConfig config,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(targets);
        config ??= ReportScenarioConfig.CreateStandardSearchConfig();

        var startTime = DateTime.UtcNow;
        var records = new List<ReportTaskRecord>();
        int successCount = 0;
        int failCount = 0;

        for (int i = 0; i < targets.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var target = targets[i];
            if (target.IsCompleted) continue;

            OnTargetStarted?.Invoke(target);

            var record = new ReportTaskRecord
            {
                TargetId = target.Id,
                TargetNameOrUrl = !string.IsNullOrWhiteSpace(target.VideoTitle) ? target.VideoTitle : target.Keyword,
                DeviceSerialOrProfile = driver.TargetId,
                Platform = driver.Platform,
                Status = ExecutionStatus.Running,
                CreatedAt = DateTime.UtcNow
            };

            var reason = config.GetNextReason();
            record.Category = reason.Category;
            record.ReasonDetails = reason.CustomDetails;

            var steps = target.TargetType switch
            {
                ReportTargetType.Channel => ScenarioDefinitions.CreateChannelReportScenario(target, config),
                ReportTargetType.Shorts => ScenarioDefinitions.CreateShortsReportScenario(target, config),
                _ => !string.IsNullOrWhiteSpace(target.TargetUrl) && string.IsNullOrWhiteSpace(target.Keyword)
                    ? ScenarioDefinitions.CreateDirectUrlReportVideoScenario(target, config)
                    : ScenarioDefinitions.CreateSearchAndReportVideoScenario(target, config)
            };

            var context = new WorkflowContext
            {
                CurrentTarget = target,
                CurrentScenario = config,
                CurrentReason = reason
            };

            try
            {
                var stepResults = await _engine.ExecuteScenarioAsync(driver, steps, context, ct).ConfigureAwait(false);
                bool allOk = stepResults.Count > 0 && stepResults.All(r => r.IsSuccess);

                record.Status = allOk ? ExecutionStatus.Succeeded : ExecutionStatus.Failed;
                record.CompletedAt = DateTime.UtcNow;

                var lastStep = stepResults.LastOrDefault();
                if (lastStep != null)
                {
                    record.ScreenshotProofPath = lastStep.ScreenshotPath;
                    if (!allOk) record.ErrorMessage = lastStep.Message;
                }

                if (allOk) successCount++;
                else failCount++;

                OnTargetFinished?.Invoke(target, allOk, record.ErrorMessage);
            }
            catch (Exception ex)
            {
                record.Status = ExecutionStatus.Failed;
                record.ErrorMessage = ex.Message;
                record.CompletedAt = DateTime.UtcNow;
                failCount++;
                OnTargetFinished?.Invoke(target, false, ex.Message);
            }

            records.Add(record);

            // Thời gian nghỉ Cooldown giữa các mục tiêu (nếu còn mục tiêu tiếp theo)
            if (i < targets.Count - 1 && config.CooldownBetweenReportsSeconds > 0)
            {
                for (int c = config.CooldownBetweenReportsSeconds; c > 0; c--)
                {
                    ct.ThrowIfCancellationRequested();
                    OnCooldownTick?.Invoke(c);
                    await Task.Delay(1000, ct).ConfigureAwait(false);
                }
            }
        }

        return new BatchExecutionSummary
        {
            TotalTargets = targets.Count,
            SuccessfulReports = successCount,
            FailedReports = failCount,
            TotalDuration = DateTime.UtcNow - startTime,
            Records = records
        };
    }
}
