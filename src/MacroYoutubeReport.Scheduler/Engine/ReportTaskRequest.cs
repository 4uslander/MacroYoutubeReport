using MacroYoutubeReport.Core.Enums;
using MacroYoutubeReport.Core.Models;

namespace MacroYoutubeReport.Scheduler.Engine;

/// <summary>
/// Yêu cầu thực thi một nhiệm vụ báo cáo trong hàng đợi Scheduler.
/// </summary>
public sealed class ReportTaskRequest
{
    public string TaskId { get; set; } = Guid.NewGuid().ToString("N");
    public ReportTargetItem Target { get; set; }
    public ReportScenarioConfig ScenarioConfig { get; set; }

    /// <summary>
    /// Nền tảng chỉ định hoặc ưu tiên (Android hoặc Chrome).
    /// </summary>
    public PlatformType PreferredPlatform { get; set; } = PlatformType.Android;

    /// <summary>
    /// Gán đích danh thiết bị/profile chạy (nếu null: Scheduler tự phân bổ thiết bị rảnh).
    /// </summary>
    public string? AssignedDeviceIdOrProfile { get; set; }

    /// <summary>
    /// Mức độ ưu tiên (số càng lớn độ ưu tiên càng cao).
    /// </summary>
    public int Priority { get; set; } = 1;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(8);
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;

    public ReportTaskRequest(ReportTargetItem target, ReportScenarioConfig? config = null)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        ScenarioConfig = config ?? ReportScenarioConfig.CreateStandardSearchConfig();
    }
}
