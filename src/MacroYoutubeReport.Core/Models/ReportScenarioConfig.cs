using MacroYoutubeReport.Core.Enums;

namespace MacroYoutubeReport.Core.Models;

/// <summary>
/// Cấu hình kịch bản báo cáo định trước, thiết lập các tham số thời gian, lý do và chống phát hiện bot.
/// </summary>
public sealed class ReportScenarioConfig
{
    public string ScenarioName { get; set; } = "Kịch bản mặc định: Tìm kiếm, xem và báo cáo video";

    /// <summary>
    /// Thời gian xem tối thiểu (giây) trước khi nhấn báo cáo.
    /// Khuyến nghị: tối thiểu 30-45 giây để YouTube ghi nhận phiên xem hợp lệ.
    /// </summary>
    public int MinWatchDurationSeconds { get; set; } = 35;

    /// <summary>
    /// Thời gian xem tối đa (giây).
    /// </summary>
    public int MaxWatchDurationSeconds { get; set; } = 90;

    /// <summary>
    /// Bật tương tác tự nhiên trong lúc xem (cuộn bình luận, tạm dừng).
    /// </summary>
    public bool EnableNaturalEngagement { get; set; } = true;

    /// <summary>
    /// Cấu hình các tùy chọn tương tác tự nhiên.
    /// </summary>
    public NaturalEngagementOptions EngagementOptions { get; set; } = new();

    /// <summary>
    /// Cho phép luân phiên ngẫu nhiên giữa danh sách các lý do báo cáo trong ReasonPool.
    /// </summary>
    public bool RotateReasons { get; set; } = true;

    /// <summary>
    /// Danh sách các lý do báo cáo để luân chuyển.
    /// </summary>
    public List<ReportReasonConfig> ReasonPool { get; set; } = new()
    {
        new ReportReasonConfig(ReportReasonCategory.SpamOrMisleading, "Spam liên tục, gây hiểu nhầm người xem."),
        new ReportReasonConfig(ReportReasonCategory.HarmfulDangerousActs, "Nội dung kích động hành vi nguy hiểm."),
        new ReportReasonConfig(ReportReasonCategory.Misinformation, "Lan truyền thông tin giả mạo sai lệch."),
        new ReportReasonConfig(ReportReasonCategory.HatefulOrAbusive, "Nội dung xúc phạm, vi phạm tiêu chuẩn cộng đồng.")
    };

    /// <summary>
    /// Danh sách mẫu văn bản giải trình dùng để spin-text khi điền vào ô ghi chú thêm của YouTube.
    /// </summary>
    public List<string> DetailTextTemplates { get; set; } = new()
    {
        "Nội dung vi phạm nghiêm trọng tiêu chuẩn cộng đồng YouTube.",
        "Video chứa thông tin sai sự thật và mang tính chất lừa dối người xem.",
        "Hành vi phát tán spam và gây hiểu lầm trên diện rộng, đề nghị kiểm tra xử lý.",
        "Kênh liên tục đăng tải các nội dung nguy hại, vi phạm chính sách nội dung của nền tảng."
    };

    /// <summary>
    /// Thời gian nghỉ giải lao (giây) sau khi hoàn thành 1 lượt report trước khi nhận nhiệm vụ mới.
    /// </summary>
    public int CooldownBetweenReportsSeconds { get; set; } = 120;

    /// <summary>
    /// Số lượt report tối đa cho phép trên 1 thiết bị/tài khoản mỗi ngày.
    /// </summary>
    public int MaxReportsPerDevicePerDay { get; set; } = 8;

    /// <summary>
    /// Chọn lý do tiếp theo (ngẫu nhiên hoặc tuần tự).
    /// </summary>
    public ReportReasonConfig GetNextReason(Random? random = null)
    {
        if (ReasonPool.Count == 0)
        {
            return new ReportReasonConfig(ReportReasonCategory.SpamOrMisleading);
        }

        if (!RotateReasons)
        {
            return ReasonPool[0];
        }

        random ??= Random.Shared;
        int idx = random.Next(ReasonPool.Count);
        var chosen = ReasonPool[idx];

        // Nếu có kích hoạt SpinText và chưa có text cụ thể, lấy từ kho templates
        if (chosen.SpinTextEnabled && string.IsNullOrWhiteSpace(chosen.CustomDetails) && DetailTextTemplates.Count > 0)
        {
            int textIdx = random.Next(DetailTextTemplates.Count);
            return new ReportReasonConfig(chosen.Category, DetailTextTemplates[textIdx], chosen.SubCategoryKey, chosen.TargetTimestamp);
        }

        return chosen;
    }

    /// <summary>
    /// Tạo cấu hình chuẩn cho luồng tìm kiếm và xem tự nhiên.
    /// </summary>
    public static ReportScenarioConfig CreateStandardSearchConfig() => new()
    {
        ScenarioName = "1. Tìm kiếm, xem 45s-120s và báo cáo vi phạm",
        MinWatchDurationSeconds = 45,
        MaxWatchDurationSeconds = 120,
        EnableNaturalEngagement = true,
        RotateReasons = true
    };

    /// <summary>
    /// Tạo cấu hình chuẩn cho luồng mở link trực tiếp (URL).
    /// </summary>
    public static ReportScenarioConfig CreateDirectUrlConfig() => new()
    {
        ScenarioName = "2. Mở trực tiếp theo URL, xem và báo cáo",
        MinWatchDurationSeconds = 30,
        MaxWatchDurationSeconds = 75,
        EnableNaturalEngagement = true,
        RotateReasons = true
    };

    /// <summary>
    /// Tạo cấu hình cho luồng báo cáo toàn bộ Kênh người dùng.
    /// </summary>
    public static ReportScenarioConfig CreateChannelReportConfig() => new()
    {
        ScenarioName = "3. Báo cáo kênh người dùng (User Channel)",
        MinWatchDurationSeconds = 15,
        MaxWatchDurationSeconds = 40,
        EnableNaturalEngagement = false,
        RotateReasons = false,
        ReasonPool = new()
        {
            new ReportReasonConfig(ReportReasonCategory.SpamOrMisleading, "Kênh hoạt động theo mạng lưới spam có tổ chức.")
        }
    };
}
