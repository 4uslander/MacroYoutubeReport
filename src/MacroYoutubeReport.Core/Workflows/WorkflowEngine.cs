using MacroYoutubeReport.Core.Drivers;
using MacroYoutubeReport.Core.Models;

namespace MacroYoutubeReport.Core.Workflows;

/// <summary>
/// Động cơ thực thi chuỗi kịch bản tuần tự, xử lý timeout, retry và ghi nhận tiến độ.
/// </summary>
public sealed class WorkflowEngine
{
    public event Action<ReportStepResult>? OnStepCompleted;

    /// <summary>
    /// Thực thi một danh sách các bước kịch bản theo thứ tự (tương thích ngược).
    /// </summary>
    public Task<IReadOnlyList<ReportStepResult>> ExecuteScenarioAsync(
        IYouTubeReportDriver driver,
        IReadOnlyList<IWorkflowStep> steps,
        WorkflowContext context,
        CancellationToken ct = default)
        => ExecuteScenarioAsync(driver, steps, context, onStepCompleted: null, ct);

    /// <summary>
    /// Thực thi một danh sách các bước kịch bản theo thứ tự với callback tiến độ riêng biệt.
    /// Nếu một bước thất bại, dừng chuỗi và trả về kết quả lỗi (trừ khi bước đó được cấu hình bỏ qua).
    /// </summary>
    public async Task<IReadOnlyList<ReportStepResult>> ExecuteScenarioAsync(
        IYouTubeReportDriver driver,
        IReadOnlyList<IWorkflowStep> steps,
        WorkflowContext context,
        Action<ReportStepResult>? onStepCompleted,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(steps);
        context ??= new WorkflowContext();

        var results = new List<ReportStepResult>();

        foreach (var step in steps)
        {
            ct.ThrowIfCancellationRequested();

            using var stepCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            stepCts.CancelAfter(step.Timeout);

            ReportStepResult result;
            try
            {
                result = await step.ExecuteAsync(driver, context, stepCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                result = ReportStepResult.Failure(step.StepName, $"Hết thời gian chờ (Timeout sau {step.Timeout.TotalSeconds:F0}s).");
            }
            catch (Exception ex)
            {
                result = ReportStepResult.Failure(step.StepName, $"Lỗi ngoại lệ: {ex.Message}", ex.ToString());
            }

            results.Add(result);
            onStepCompleted?.Invoke(result);
            OnStepCompleted?.Invoke(result);

            if (!result.IsSuccess)
            {
                // Thử chụp màn hình khi thất bại để lưu dấu vết phân tích
                try
                {
                    string errorShot = await driver.CaptureScreenshotAsync($"Error_{step.StepName.Replace(' ', '_')}", ct).ConfigureAwait(false);
                    context.Set("LastErrorScreenshot", errorShot);
                }
                catch
                {
                    // Bỏ qua nếu driver đã mất kết nối
                }

                break;
            }
        }

        return results;
    }
}
