using System.Collections.Concurrent;
using MacroYoutubeReport.Core.Drivers;
using MacroYoutubeReport.Core.Enums;
using MacroYoutubeReport.Core.Models;
using MacroYoutubeReport.Core.Workflows;
using MacroYoutubeReport.Scheduler.Journal;

namespace MacroYoutubeReport.Scheduler.Coordination;

/// <summary>
/// Bộ điều phối thực thi nhiệm vụ báo cáo hàng đợi, kiểm soát luân chuyển thiết bị và ghi nhật ký.
/// </summary>
public sealed class ReportTaskCoordinator
{
    private readonly IExecutionJournal _journal;
    private readonly ConcurrentQueue<ReportTargetItem> _targetQueue = new();

    public ReportTaskCoordinator(IExecutionJournal journal)
    {
        _journal = journal ?? throw new ArgumentNullException(nameof(journal));
    }

    public void EnqueueTarget(ReportTargetItem target)
    {
        ArgumentNullException.ThrowIfNull(target);
        _targetQueue.Enqueue(target);
    }

    public void EnqueueTargets(IEnumerable<ReportTargetItem> targets)
    {
        ArgumentNullException.ThrowIfNull(targets);
        foreach (var t in targets)
        {
            _targetQueue.Enqueue(t);
        }
    }

    public bool TryDequeueTarget(out ReportTargetItem? target) =>
        _targetQueue.TryDequeue(out target);

    public int RemainingTargetsCount => _targetQueue.Count;

    /// <summary>
    /// Thực thi một mục tiêu báo cáo trên driver cụ thể.
    /// </summary>
    public async Task<ReportTaskRecord> ExecuteSingleReportAsync(
        IYouTubeReportDriver driver,
        ReportTargetItem target,
        ReportScenarioConfig config,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(target);
        config ??= ReportScenarioConfig.CreateStandardSearchConfig();

        var record = new ReportTaskRecord
        {
            TargetId = target.Id,
            TargetNameOrUrl = !string.IsNullOrWhiteSpace(target.VideoTitle) ? target.VideoTitle : target.Keyword,
            DeviceSerialOrProfile = driver.TargetId,
            Platform = driver.Platform,
            Category = config.ReasonPool.Count > 0 ? config.ReasonPool[0].Category : ReportReasonCategory.SpamOrMisleading,
            Status = ExecutionStatus.Running,
            CreatedAt = DateTime.UtcNow
        };

        var steps = target.TargetType switch
        {
            ReportTargetType.Channel => ScenarioDefinitions.CreateChannelReportScenario(target, config),
            _ => !string.IsNullOrWhiteSpace(target.TargetUrl) && string.IsNullOrWhiteSpace(target.Keyword)
                ? ScenarioDefinitions.CreateDirectUrlReportVideoScenario(target, config)
                : ScenarioDefinitions.CreateSearchAndReportVideoScenario(target, config)
        };

        var engine = new WorkflowEngine();
        var context = new WorkflowContext();

        try
        {
            var results = await engine.ExecuteScenarioAsync(driver, steps, context, ct).ConfigureAwait(false);
            bool allSuccess = results.All(r => r.IsSuccess);

            record.Status = allSuccess ? ExecutionStatus.Succeeded : ExecutionStatus.Failed;
            record.CompletedAt = DateTime.UtcNow;
            record.ReasonDetails = context.CurrentReason?.CustomDetails;
            record.Category = context.CurrentReason?.Category ?? record.Category;

            var lastResult = results.LastOrDefault();
            if (lastResult != null)
            {
                record.ScreenshotProofPath = lastResult.ScreenshotPath;
                if (!lastResult.IsSuccess)
                {
                    record.ErrorMessage = lastResult.Message;
                }
            }
        }
        catch (Exception ex)
        {
            record.Status = ExecutionStatus.Failed;
            record.ErrorMessage = ex.Message;
            record.CompletedAt = DateTime.UtcNow;
        }

        await _journal.RecordTaskAsync(record, ct).ConfigureAwait(false);
        return record;
    }
}
