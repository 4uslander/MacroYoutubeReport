namespace MacroYoutubeReport.Core.Models;

/// <summary>
/// Các tùy chọn mô phỏng hành vi tự nhiên khi xem video trước khi thực hiện báo cáo.
/// Tránh thuật toán gắn cờ spam bot của YouTube.
/// </summary>
public sealed class NaturalEngagementOptions
{
    /// <summary>
    /// Cho phép cuộn nhẹ xuống khu vực bình luận rồi cuộn ngược lại.
    /// </summary>
    public bool EnableCommentReadingScroll { get; set; } = true;

    /// <summary>
    /// Bật/tắt pause rồi play lại video nhẹ 1-2 giây để tạo tín hiệu người thật.
    /// </summary>
    public bool EnablePlaybackToggle { get; set; } = true;

    /// <summary>
    /// Thao tác chạm nhẹ vào thanh âm lượng hoặc thanh tiến trình.
    /// </summary>
    public bool EnableVolumeOrProgressBarTouch { get; set; } = false;

    /// <summary>
    /// Số lượng vi hành vi tối thiểu trong lúc xem.
    /// </summary>
    public int MinActions { get; set; } = 1;

    /// <summary>
    /// Số lượng vi hành vi tối đa trong lúc xem.
    /// </summary>
    public int MaxActions { get; set; } = 3;

    public static NaturalEngagementOptions Default => new();
    public static NaturalEngagementOptions PassiveOnly => new()
    {
        EnableCommentReadingScroll = false,
        EnablePlaybackToggle = false,
        MinActions = 0,
        MaxActions = 0
    };
}
