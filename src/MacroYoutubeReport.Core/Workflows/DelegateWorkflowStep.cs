using MacroYoutubeReport.Core.Drivers;
using MacroYoutubeReport.Core.Models;

namespace MacroYoutubeReport.Core.Workflows;

/// <summary>
/// Bước thực thi linh hoạt thông qua hàm ủy nhiệm (Delegate Function).
/// </summary>
public sealed class DelegateWorkflowStep : IWorkflowStep
{
    private readonly Func<IYouTubeReportDriver, WorkflowContext, CancellationToken, Task<ReportStepResult>> _action;

    public string StepName { get; }
    public TimeSpan Timeout { get; }

    public DelegateWorkflowStep(
        string stepName,
        Func<IYouTubeReportDriver, WorkflowContext, CancellationToken, Task<ReportStepResult>> action,
        TimeSpan? timeout = null)
    {
        StepName = string.IsNullOrWhiteSpace(stepName) ? "Step" : stepName.Trim();
        _action = action ?? throw new ArgumentNullException(nameof(action));
        Timeout = timeout ?? TimeSpan.FromSeconds(120);
    }

    public Task<ReportStepResult> ExecuteAsync(IYouTubeReportDriver driver, WorkflowContext context, CancellationToken ct) =>
        _action(driver, context, ct);
}
