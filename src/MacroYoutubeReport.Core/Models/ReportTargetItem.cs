using MacroYoutubeReport.Core.Enums;

namespace MacroYoutubeReport.Core.Models;

/// <summary>
/// Đại diện cho một đối tượng mục tiêu cần báo cáo (Video hoặc Channel).
/// </summary>
public sealed class ReportTargetItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Loại mục tiêu (Video, Channel, Shorts).
    /// </summary>
    public ReportTargetType TargetType { get; set; } = ReportTargetType.Video;

    /// <summary>
    /// Từ khóa dùng để tìm kiếm trên thanh search của YouTube.
    /// </summary>
    public string Keyword { get; set; } = string.Empty;

    /// <summary>
    /// Tên kênh để nhận diện chính xác video trong danh sách kết quả tìm kiếm.
    /// </summary>
    public string? ChannelName { get; set; }

    /// <summary>
    /// Tiêu đề video (hoặc một phần tiêu đề) để so khớp.
    /// </summary>
    public string? VideoTitle { get; set; }

    /// <summary>
    /// Đường dẫn trực tiếp URL (nếu chạy kịch bản mở thẳng link hoặc dùng để đối chiếu).
    /// </summary>
    public string? TargetUrl { get; set; }

    /// <summary>
    /// Mức độ ưu tiên (số càng lớn ưu tiên càng cao).
    /// </summary>
    public int Priority { get; set; } = 1;

    /// <summary>
    /// Số lượt báo cáo thành công tối đa cần đạt cho mục tiêu này.
    /// </summary>
    public int MaxReportsRequired { get; set; } = 1;

    /// <summary>
    /// Số lượt báo cáo đã thực hiện thành công.
    /// </summary>
    public int ReportsCompleted { get; set; } = 0;

    /// <summary>
    /// Thời điểm báo cáo thành công gần nhất.
    /// </summary>
    public DateTime? LastReportedAt { get; set; }

    /// <summary>
    /// Đã hoàn thành đủ số lượt báo cáo yêu cầu.
    /// </summary>
    public bool IsCompleted => ReportsCompleted >= MaxReportsRequired;

    public ReportTargetItem() { }

    public ReportTargetItem(string keyword, string? channelName = null, string? videoTitle = null, string? targetUrl = null)
    {
        Keyword = keyword;
        ChannelName = channelName;
        VideoTitle = videoTitle;
        TargetUrl = targetUrl;
    }

    /// <summary>
    /// So khớp tiêu đề và kênh từ kết quả quét giao diện.
    /// </summary>
    public bool Matches(string? foundTitle, string? foundChannel, string? foundUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(TargetUrl) && !string.IsNullOrWhiteSpace(foundUrl))
        {
            return foundUrl.Contains(TargetUrl, StringComparison.OrdinalIgnoreCase) || 
                   TargetUrl.Contains(foundUrl, StringComparison.OrdinalIgnoreCase);
        }

        bool matchTitle = true;
        if (!string.IsNullOrWhiteSpace(VideoTitle))
        {
            matchTitle = !string.IsNullOrWhiteSpace(foundTitle) &&
                         foundTitle.Contains(VideoTitle.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        bool matchChannel = true;
        if (!string.IsNullOrWhiteSpace(ChannelName))
        {
            matchChannel = !string.IsNullOrWhiteSpace(foundChannel) &&
                          foundChannel.Contains(ChannelName.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        return matchTitle && matchChannel;
    }
}
