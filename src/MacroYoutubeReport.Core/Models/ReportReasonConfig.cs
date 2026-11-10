using MacroYoutubeReport.Core.Enums;

namespace MacroYoutubeReport.Core.Models;

/// <summary>
/// Cấu hình cụ thể cho một hành động báo cáo vi phạm.
/// </summary>
public sealed class ReportReasonConfig
{
    /// <summary>
    /// Danh mục vi phạm chính trên YouTube.
    /// </summary>
    public ReportReasonCategory Category { get; set; } = ReportReasonCategory.SpamOrMisleading;

    /// <summary>
    /// Khóa danh mục con (Sub-category) nếu YouTube yêu cầu chọn chi tiết hơn.
    /// </summary>
    public string? SubCategoryKey { get; set; }

    /// <summary>
    /// Nội dung diễn giải chi tiết tùy chọn nhập vào ô ghi chú thêm của YouTube.
    /// </summary>
    public string? CustomDetails { get; set; }

    /// <summary>
    /// Mốc thời gian (timestamp trong video, ví dụ "01:23") nếu áp dụng.
    /// </summary>
    public string? TargetTimestamp { get; set; }

    /// <summary>
    /// Kích hoạt hoán đổi nội dung giải trình tự động (Spin-text) để tránh spam pattern.
    /// </summary>
    public bool SpinTextEnabled { get; set; } = true;

    public ReportReasonConfig() { }

    public ReportReasonConfig(
        ReportReasonCategory category, 
        string? customDetails = null, 
        string? subCategoryKey = null, 
        string? targetTimestamp = null)
    {
        Category = category;
        CustomDetails = customDetails;
        SubCategoryKey = subCategoryKey;
        TargetTimestamp = targetTimestamp;
    }

    public override string ToString() =>
        $"{Category} - {CustomDetails ?? "(Không kèm văn bản mô tả)"}";
}
