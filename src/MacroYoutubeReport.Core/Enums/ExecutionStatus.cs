namespace MacroYoutubeReport.Core.Enums;

/// <summary>
/// Trạng thái thực thi nhiệm vụ báo cáo.
/// </summary>
public enum ExecutionStatus
{
    Pending,
    Running,
    Succeeded,
    Failed,
    Cancelled,
    Skipped
}
