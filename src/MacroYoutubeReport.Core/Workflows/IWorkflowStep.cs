using MacroYoutubeReport.Core.Drivers;
using MacroYoutubeReport.Core.Models;

namespace MacroYoutubeReport.Core.Workflows;

/// <summary>
/// Giao diện một bước thực thi đơn vị trong chuỗi kịch bản báo cáo.
/// </summary>
public interface IWorkflowStep
{
    string StepName { get; }
    TimeSpan Timeout { get; }
    Task<ReportStepResult> ExecuteAsync(IYouTubeReportDriver driver, WorkflowContext context, CancellationToken ct);
}
