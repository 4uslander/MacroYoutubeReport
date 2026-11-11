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
    /// Hỗ trợ cả khớp chính xác tuyệt đối lẫn khớp thông minh theo cụm từ khóa (Fuzzy / Token Matching).
    /// </summary>
    public bool Matches(string? foundTitle, string? foundChannel, string? foundUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(TargetUrl) && !string.IsNullOrWhiteSpace(foundUrl))
        {
            return foundUrl.Contains(TargetUrl, StringComparison.OrdinalIgnoreCase) || 
                   TargetUrl.Contains(foundUrl, StringComparison.OrdinalIgnoreCase);
        }

        string combined = $"{foundTitle} {foundChannel}".Trim();
        if (string.IsNullOrWhiteSpace(combined)) return false;

        string cleanTitle = VideoTitle?.TrimEnd('.', '…', ' ') ?? string.Empty;
        string cleanChannel = ChannelName?.TrimEnd('.', '…', ' ') ?? string.Empty;

        // 1. Trường hợp có cả Title lẫn Channel
        if (!string.IsNullOrWhiteSpace(cleanTitle) && !string.IsNullOrWhiteSpace(cleanChannel))
        {
            // Bắt buộc phải đúng Kênh trước
            if (!combined.Contains(cleanChannel, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // 1.1. Khớp chính xác cả 2 chuỗi
            if (combined.Contains(cleanTitle, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // 1.2. Khớp tiêu đề bị cắt ngắn (do YouTube thêm dấu ... ở cuối)
            string trimmedTitle = cleanTitle.TrimEnd('.', '…', ' ');
            if (trimmedTitle.Length >= 8 && combined.Contains(trimmedTitle, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // 1.3. Khớp thông minh: Đúng KÊNH và tiêu đề video chứa các phân đoạn đặc trưng chính
            var segments = cleanTitle.Split(new[] { '|', '-', ':', '[', ']', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(s => s.Trim())
                                     .Where(s => s.Length >= 5 && 
                                                 !s.Equals("실시간", StringComparison.OrdinalIgnoreCase) && 
                                                 !s.Equals("live", StringComparison.OrdinalIgnoreCase) &&
                                                 !s.Equals("stream", StringComparison.OrdinalIgnoreCase))
                                     .ToList();

            if (segments.Count > 0 && segments.Any(seg => combined.Contains(seg, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // 1.4. Khớp từ khóa tiêu đề có tỷ lệ cao
            if (IsTitleKeywordMatch(cleanTitle, combined))
            {
                return true;
            }

            return false; // Đã có cả Title và Channel thì không được fall-through sang chỉ check Channel!
        }

        // 2. Chỉ có Title
        if (!string.IsNullOrWhiteSpace(cleanTitle))
        {
            return combined.Contains(cleanTitle, StringComparison.OrdinalIgnoreCase) || IsTitleKeywordMatch(cleanTitle, combined);
        }

        // 3. Chỉ có Channel
        if (!string.IsNullOrWhiteSpace(cleanChannel))
        {
            return combined.Contains(cleanChannel, StringComparison.OrdinalIgnoreCase);
        }

        // 4. Chỉ có Keyword
        if (!string.IsNullOrWhiteSpace(Keyword))
        {
            return combined.Contains(Keyword.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool IsTitleKeywordMatch(string titlePattern, string candidateText)
    {
        var tokens = titlePattern.Split(new[] { ' ', '|', '-', ':', '[', ']', '(', ')', '#', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(t => t.Trim())
                                 .Where(t => t.Length >= 3 && 
                                             !t.Equals("실시간", StringComparison.OrdinalIgnoreCase) && 
                                             !t.Equals("live", StringComparison.OrdinalIgnoreCase) &&
                                             !t.Equals("stream", StringComparison.OrdinalIgnoreCase))
                                 .Distinct(StringComparer.OrdinalIgnoreCase)
                                 .ToList();

        if (tokens.Count == 0) return false;

        int matchedCount = tokens.Count(t => candidateText.Contains(t, StringComparison.OrdinalIgnoreCase));
        double ratio = (double)matchedCount / tokens.Count;

        // Khớp >= 60% từ khóa và tối thiểu 2 từ khóa đặc trưng (hoặc 1 từ khóa dài >= 8 ký tự)
        return (ratio >= 0.60 && matchedCount >= 2) || (matchedCount >= 1 && tokens.Any(t => t.Length >= 8 && candidateText.Contains(t, StringComparison.OrdinalIgnoreCase)));
    }
}
