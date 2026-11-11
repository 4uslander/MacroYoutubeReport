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
    /// Cấu hình bộ lọc tìm kiếm video chuẩn theo phong cách AutoYouTube.
    /// </summary>
    public SearchFilterConfig SearchFilter { get; set; } = new();

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
    /// Đường dẫn file danh sách nội dung lý do báo cáo tùy chỉnh.
    /// </summary>
    public string? ReasonDetailsFilePath { get; set; }

    /// <summary>
    /// Bảng ánh xạ danh mục lý do với danh sách các câu giải trình tương ứng.
    /// </summary>
    public Dictionary<ReportReasonCategory, List<string>> ReasonCategoryTemplates { get; set; } = new();

    /// <summary>
    /// Số lượt report tối đa cho phép trên 1 thiết bị/tài khoản mỗi ngày.
    /// </summary>
    public int MaxReportsPerDevicePerDay { get; set; } = 8;

    /// <summary>
    /// Tải danh sách nội dung lý do chi tiết từ file văn bản.
    /// Hỗ trợ cả định dạng phân nhóm [Category] hoặc mỗi dòng một lý do xoay vòng.
    /// </summary>
    public void LoadReasonTemplatesFromFile(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;

        try
        {
            ReasonDetailsFilePath = filePath;
            ReasonCategoryTemplates.Clear();
            var lines = File.ReadAllLines(filePath);
            ReportReasonCategory currentCategory = ReportReasonCategory.FinancialOrCryptoScam;
            var allTemplates = new List<string>();

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith("//"))
                {
                    continue;
                }

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    string sec = line.Substring(1, line.Length - 2).Trim();
                    currentCategory = sec.ToLowerInvariant() switch
                    {
                        var s when s.Contains("financial") || s.Contains("crypto") || s.Contains("lừa đảo") || s.Contains("사기") => ReportReasonCategory.FinancialOrCryptoScam,
                        var s when s.Contains("spam") || s.Contains("misleading") || s.Contains("hiểu nhầm") || s.Contains("스팸") => ReportReasonCategory.SpamOrMisleading,
                        var s when s.Contains("harmful") || s.Contains("nguy hiểm") || s.Contains("위험") => ReportReasonCategory.HarmfulDangerousActs,
                        var s when s.Contains("misinformation") || s.Contains("sai lệch") || s.Contains("잘못된") => ReportReasonCategory.Misinformation,
                        var s when s.Contains("hateful") || s.Contains("thù địch") || s.Contains("증오") => ReportReasonCategory.HatefulOrAbusive,
                        var s when s.Contains("harassment") || s.Contains("quấy rối") || s.Contains("괴롭힘") => ReportReasonCategory.HarassmentOrBullying,
                        var s when s.Contains("violent") || s.Contains("bạo lực") || s.Contains("폭력") => ReportReasonCategory.ViolentOrRepulsive,
                        var s when s.Contains("sexual") || s.Contains("khiêu dâm") || s.Contains("성적") => ReportReasonCategory.SexualContent,
                        _ => ReportReasonCategory.FinancialOrCryptoScam
                    };

                    if (!ReasonCategoryTemplates.ContainsKey(currentCategory))
                    {
                        ReasonCategoryTemplates[currentCategory] = new List<string>();
                    }
                    continue;
                }

                allTemplates.Add(line);
                if (!ReasonCategoryTemplates.TryGetValue(currentCategory, out var catList))
                {
                    catList = new List<string>();
                    ReasonCategoryTemplates[currentCategory] = catList;
                }
                catList.Add(line);
            }

            if (allTemplates.Count > 0)
            {
                DetailTextTemplates = allTemplates;
            }
        }
        catch
        {
            // Bỏ qua lỗi đọc file, dùng template có sẵn
        }
    }

    /// <summary>
    /// Lấy câu giải trình chi tiết cho một danh mục lý do cụ thể, ưu tiên từ file cấu hình.
    /// </summary>
    public string GetDetailTextForCategory(ReportReasonCategory category, Random? random = null)
    {
        random ??= Random.Shared;
        if (ReasonCategoryTemplates.TryGetValue(category, out var catTemplates) && catTemplates.Count > 0)
        {
            return catTemplates[random.Next(catTemplates.Count)];
        }

        if (DetailTextTemplates.Count > 0)
        {
            return DetailTextTemplates[random.Next(DetailTextTemplates.Count)];
        }

        return category switch
        {
            ReportReasonCategory.FinancialOrCryptoScam => "이 방송은 불법 해외선물 대여계좌 및 사기성 리딩방을 홍보하고 있습니다.",
            ReportReasonCategory.SpamOrMisleading => "스팸성 링크 및 불법 투자 유도성 콘텐츠입니다.",
            ReportReasonCategory.Misinformation => "시청자를 기만하는 조작된 허위 정보 방송입니다.",
            ReportReasonCategory.HarmfulDangerousActs => "시청자에게 위험하거나 불법적인 금융 거래를 유도하고 있습니다.",
            _ => "YouTube 커뮤니티 가이드라인을 심각하게 위반하는 콘텐츠입니다."
        };
    }

    /// <summary>
    /// Chọn lý do tiếp theo (ngẫu nhiên hoặc tuần tự).
    /// </summary>
    public ReportReasonConfig GetNextReason(Random? random = null)
    {
        if (ReasonPool.Count == 0)
        {
            return new ReportReasonConfig(ReportReasonCategory.SpamOrMisleading);
        }

        random ??= Random.Shared;
        var chosen = RotateReasons ? ReasonPool[random.Next(ReasonPool.Count)] : ReasonPool[0];

        // Lấy câu giải trình chi tiết: ƯU TIÊN CAO NHẤT từ kho templates của danh mục (ReasonCategoryTemplates từ file cấu hình)
        string? detail = null;
        if (ReasonCategoryTemplates.TryGetValue(chosen.Category, out var catTemplates) && catTemplates.Count > 0)
        {
            detail = catTemplates[random.Next(catTemplates.Count)];
        }
        else if (!string.IsNullOrWhiteSpace(chosen.CustomDetails))
        {
            detail = chosen.CustomDetails;
        }
        else if (DetailTextTemplates.Count > 0)
        {
            detail = DetailTextTemplates[random.Next(DetailTextTemplates.Count)];
        }
        else
        {
            detail = GetDetailTextForCategory(chosen.Category, random);
        }

        return new ReportReasonConfig(chosen.Category, detail, chosen.SubCategoryKey, chosen.TargetTimestamp)
        {
            SpinTextEnabled = chosen.SpinTextEnabled
        };
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

    /// <summary>
    /// Tạo cấu hình chuyên sâu cho luồng báo cáo Livestream lừa đảo tài chính và tiền ảo (Crypto/Financial Scam).
    /// </summary>
    public static ReportScenarioConfig CreateLiveStreamFinancialScamConfig() => new()
    {
        ScenarioName = "6. Báo cáo Livestream lừa đảo tài chính / crypto",
        MinWatchDurationSeconds = 20,
        MaxWatchDurationSeconds = 45,
        EnableNaturalEngagement = true,
        RotateReasons = true,
        SearchFilter = new SearchFilterConfig
        {
            LiveOnly = true,
            UploadDate = SearchUploadDateFilter.All,
            ReSearchIfNotFound = true,
            CheckLaixiKeyboard = true,
            MaxScrollAttempts = 60
        },
        ReasonPool = new()
        {
            new ReportReasonConfig(ReportReasonCategory.FinancialOrCryptoScam),
            new ReportReasonConfig(ReportReasonCategory.SpamOrMisleading)
        },
        DetailTextTemplates = new()
        {
            "Livestream này đang phát sóng mô hình lừa đảo tài chính và tiền mã hóa (crypto giveaway scam, doubling crypto), dụ dỗ người xem chuyển tiền vào ví giả mạo.",
            "Video phát trực tiếp dụ dỗ người xem tham gia sàn giao dịch tài chính nhị phân/đa cấp bất hợp pháp, cam kết lợi nhuận bất thường và chiếm đoạt tiền gửi.",
            "Dấu hiệu lừa đảo tài chính nghiêm trọng: live chat được bot spam link độc hại đánh cắp khóa ví tiền ảo và tài khoản ngân hàng.",
            "This live stream promotes a fraudulent cryptocurrency/financial scam (fake giveaways, phishing QR code). Please terminate it under the Financial Scams policy.",
            "이 실시간 스트리밍은 가상자산 사기 및 허위 에어드롭 이벤트를 홍보하여 사용자의 자산을 탈취하고 있습니다."
        }
    };
}
