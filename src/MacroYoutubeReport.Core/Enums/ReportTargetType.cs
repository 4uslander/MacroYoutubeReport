namespace MacroYoutubeReport.Core.Enums;

/// <summary>
/// Loại đối tượng YouTube cần báo cáo.
/// </summary>
public enum ReportTargetType
{
    /// <summary>
    /// Báo cáo video cụ thể thông qua tìm kiếm từ khóa hoặc URL.
    /// </summary>
    Video = 1,

    /// <summary>
    /// Báo cáo kênh YouTube (người dùng) qua trang About của kênh.
    /// </summary>
    Channel = 2,

    /// <summary>
    /// Báo cáo video ngắn Shorts.
    /// </summary>
    Shorts = 3,

    /// <summary>
    /// Báo cáo luồng phát trực tiếp (Live Stream), tự động xử lý đóng/mở Live Chat overlay.
    /// </summary>
    LiveStream = 4
}
