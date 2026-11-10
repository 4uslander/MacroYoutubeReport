using MacroYoutubeReport.Core.Enums;

namespace MacroYoutubeReport.Core.Models;

/// <summary>
/// Bản ghi nhật ký thực thi của một tác vụ báo cáo, dùng cho lưu trữ SQLite Journal và báo cáo thống kê.
/// </summary>
public sealed class ReportTaskRecord
{
    public long Id { get; set; }
    public string TaskId { get; set; } = Guid.NewGuid().ToString("N");
    public string TargetId { get; set; } = string.Empty;
    public string TargetNameOrUrl { get; set; } = string.Empty;
    public string DeviceSerialOrProfile { get; set; } = string.Empty;
    public PlatformType Platform { get; set; }
    public ReportReasonCategory Category { get; set; }
    public string? ReasonDetails { get; set; }
    public ExecutionStatus Status { get; set; } = ExecutionStatus.Pending;
    public int WatchDurationSeconds { get; set; }
    public string? ScreenshotProofPath { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public double DurationTotalSeconds => CompletedAt.HasValue
        ? (CompletedAt.Value - CreatedAt).TotalSeconds
        : 0;
}
