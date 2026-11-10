using MacroYoutubeReport.Core.Enums;

namespace MacroYoutubeReport.Core.Localization;

/// <summary>
/// Quản lý từ điển đa ngôn ngữ cho toàn bộ các text, nhãn và locators nhận diện trên YouTube (VI, EN, KO).
/// Hỗ trợ tìm kiếm mờ (fuzzy match) và so khớp danh mục báo cáo chính xác.
/// </summary>
public static class UiLanguageManager
{
    public static AppLanguage CurrentLanguage { get; set; } = AppLanguage.Vietnamese;

    private static readonly Dictionary<ReportReasonCategory, Dictionary<AppLanguage, string[]>> CategoryKeywords = new()
    {
        [ReportReasonCategory.SpamOrMisleading] = new()
        {
            [AppLanguage.Vietnamese] = ["Spam hoặc gây hiểu nhầm", "Spam hoặc", "hiểu nhầm", "Spam"],
            [AppLanguage.English] = ["Spam or misleading", "Spam or misleading content", "Spam", "Misleading"],
            [AppLanguage.Korean] = ["스팸 또는 혼동을 야기하는 콘텐츠", "스팸 또는 혼동", "스팸", "혼동"]
        },
        [ReportReasonCategory.SexualContent] = new()
        {
            [AppLanguage.Vietnamese] = ["Nội dung khiêu dâm", "khiêu dâm", "Nội dung người lớn"],
            [AppLanguage.English] = ["Sexual content", "Sexually explicit content", "Adult content"],
            [AppLanguage.Korean] = ["성적인 콘텐츠", "성적인 내용", "음란물"]
        },
        [ReportReasonCategory.ViolentOrRepulsive] = new()
        {
            [AppLanguage.Vietnamese] = ["Nội dung bạo lực hoặc phản cảm", "bạo lực", "phản cảm"],
            [AppLanguage.English] = ["Violent or repulsive content", "Violent content", "Repulsive content"],
            [AppLanguage.Korean] = ["폭력적 또는 혐오스러운 콘텐츠", "폭력적", "혐오스러운 콘텐츠"]
        },
        [ReportReasonCategory.HatefulOrAbusive] = new()
        {
            [AppLanguage.Vietnamese] = ["Nội dung kích động thù địch hoặc lăng mạ", "thù địch", "lăng mạ", "kích động thù địch"],
            [AppLanguage.English] = ["Hateful or abusive content", "Hate speech", "Abusive content"],
            [AppLanguage.Korean] = ["증오 또는 악의적인 콘텐츠", "증오 발언", "악의적인 콘텐츠"]
        },
        [ReportReasonCategory.HarassmentOrBullying] = new()
        {
            [AppLanguage.Vietnamese] = ["Quấy rối hoặc bắt nạt", "Quấy rối", "bắt nạt"],
            [AppLanguage.English] = ["Harassment or bullying", "Harassment", "Bullying"],
            [AppLanguage.Korean] = ["괴롭힘 또는 폭력", "괴롭힘", "사이버 불링"]
        },
        [ReportReasonCategory.HarmfulDangerousActs] = new()
        {
            [AppLanguage.Vietnamese] = ["Hành vi có hại hoặc nguy hiểm", "nguy hiểm", "có hại"],
            [AppLanguage.English] = ["Harmful or dangerous acts", "Harmful acts", "Dangerous acts"],
            [AppLanguage.Korean] = ["유해하거나 위험한 행위", "유해한 행위", "위험한 행위"]
        },
        [ReportReasonCategory.Misinformation] = new()
        {
            [AppLanguage.Vietnamese] = ["Thông tin sai lệch", "sai lệch", "tin giả"],
            [AppLanguage.English] = ["Misinformation", "Misleading info", "False information"],
            [AppLanguage.Korean] = ["잘못된 정보", "허위 정보", "오해의 소지가 있는 정보"]
        },
        [ReportReasonCategory.ChildAbuse] = new()
        {
            [AppLanguage.Vietnamese] = ["Ngược đãi trẻ em", "trẻ em", "xâm hại trẻ em"],
            [AppLanguage.English] = ["Child abuse", "Child endangerment"],
            [AppLanguage.Korean] = ["아동 학대", "아동 보호 위반"]
        },
        [ReportReasonCategory.PromotesTerrorism] = new()
        {
            [AppLanguage.Vietnamese] = ["Cổ xúy chủ nghĩa khủng bố", "khủng bố", "Cổ xúy khủng bố"],
            [AppLanguage.English] = ["Promotes terrorism", "Terrorism", "Violent extremism"],
            [AppLanguage.Korean] = ["테러 조장", "테러리즘", "폭력적 극단주의"]
        },
        [ReportReasonCategory.InfringesMyRights] = new()
        {
            [AppLanguage.Vietnamese] = ["Vi phạm quyền của tôi", "Bản quyền", "Quyền riêng tư"],
            [AppLanguage.English] = ["Infringes my rights", "Copyright issue", "Privacy violation"],
            [AppLanguage.Korean] = ["권리 침해", "저작권 침해", "개인정보 침해"]
        },
        [ReportReasonCategory.CaptionsIssue] = new()
        {
            [AppLanguage.Vietnamese] = ["Vấn đề về phụ đề", "phụ đề"],
            [AppLanguage.English] = ["Captions issue", "Subtitle issue"],
            [AppLanguage.Korean] = ["자막 문제", "자막 관련 문제"]
        }
    };

    private static readonly Dictionary<AppLanguage, string[]> ReportMenuKeywords = new()
    {
        [AppLanguage.Vietnamese] = ["Báo cáo", "Báo cáo vi phạm", "Báo cáo người dùng"],
        [AppLanguage.English] = ["Report", "Report user", "Flag"],
        [AppLanguage.Korean] = ["신고", "사용자 신고", "신고하기"]
    };

    private static readonly Dictionary<AppLanguage, string[]> SettingsMenuKeywords = new()
    {
        [AppLanguage.Vietnamese] = ["Cài đặt", "Tùy chọn khác", "Khác", "Thêm tác vụ"],
        [AppLanguage.English] = ["Settings", "More options", "More actions", "Overflow"],
        [AppLanguage.Korean] = ["설정", "추가 옵션", "더보기", "추가 작업"]
    };

    private static readonly Dictionary<AppLanguage, string[]> SubmitButtonKeywords = new()
    {
        [AppLanguage.Vietnamese] = ["Báo cáo", "Tiếp theo", "Gửi", "Xong"],
        [AppLanguage.English] = ["Report", "Next", "Submit", "Done"],
        [AppLanguage.Korean] = ["신고", "다음", "제출", "완료"]
    };

    private static readonly Dictionary<AppLanguage, string[]> SuccessConfirmationKeywords = new()
    {
        [AppLanguage.Vietnamese] = ["Cảm ơn bạn đã báo cáo", "Đã gửi báo cáo", "Cảm ơn bạn đã phản hồi", "Báo cáo của bạn"],
        [AppLanguage.English] = ["Thanks for reporting", "Report submitted", "Thank you for reporting", "We received your report"],
        [AppLanguage.Korean] = ["신고해 주셔서 감사합니다", "신고가 접수되었습니다", "의견을 보내주셔서 감사합니다"]
    };

    /// <summary>
    /// Lấy danh sách từ khóa đại diện cho danh mục báo cáo theo ngôn ngữ cụ thể.
    /// </summary>
    public static IReadOnlyList<string> GetKeywordsForCategory(ReportReasonCategory category, AppLanguage language)
    {
        if (CategoryKeywords.TryGetValue(category, out var langDict) &&
            langDict.TryGetValue(language, out var keywords))
        {
            return keywords;
        }

        return Array.Empty<string>();
    }

    /// <summary>
    /// Lấy tất cả từ khóa cho danh mục báo cáo trên toàn bộ các ngôn ngữ (dùng khi chưa biết app đang ở ngôn ngữ nào).
    /// </summary>
    public static IReadOnlyList<string> GetAllKeywordsForCategory(ReportReasonCategory category)
    {
        if (!CategoryKeywords.TryGetValue(category, out var langDict))
        {
            return Array.Empty<string>();
        }

        var list = new List<string>();
        foreach (var arr in langDict.Values)
        {
            list.AddRange(arr);
        }
        return list;
    }

    /// <summary>
    /// Lấy danh sách từ khóa nút mở menu Báo cáo theo ngôn ngữ.
    /// </summary>
    public static IReadOnlyList<string> GetReportMenuKeywords(AppLanguage? language = null)
    {
        if (language.HasValue && ReportMenuKeywords.TryGetValue(language.Value, out var keywords))
        {
            return keywords;
        }

        var all = new List<string>();
        foreach (var arr in ReportMenuKeywords.Values)
        {
            all.AddRange(arr);
        }
        return all;
    }

    /// <summary>
    /// Lấy danh sách từ khóa nút Settings/Cài đặt/3-chấm.
    /// </summary>
    public static IReadOnlyList<string> GetSettingsMenuKeywords(AppLanguage? language = null)
    {
        if (language.HasValue && SettingsMenuKeywords.TryGetValue(language.Value, out var keywords))
        {
            return keywords;
        }

        var all = new List<string>();
        foreach (var arr in SettingsMenuKeywords.Values)
        {
            all.AddRange(arr);
        }
        return all;
    }

    /// <summary>
    /// Lấy danh sách từ khóa nút xác nhận Báo cáo / Tiếp theo.
    /// </summary>
    public static IReadOnlyList<string> GetSubmitButtonKeywords(AppLanguage? language = null)
    {
        if (language.HasValue && SubmitButtonKeywords.TryGetValue(language.Value, out var keywords))
        {
            return keywords;
        }

        var all = new List<string>();
        foreach (var arr in SubmitButtonKeywords.Values)
        {
            all.AddRange(arr);
        }
        return all;
    }

    /// <summary>
    /// Lấy các từ khóa báo hiệu gửi report thành công.
    /// </summary>
    public static IReadOnlyList<string> GetSuccessConfirmationKeywords(AppLanguage? language = null)
    {
        if (language.HasValue && SuccessConfirmationKeywords.TryGetValue(language.Value, out var keywords))
        {
            return keywords;
        }

        var all = new List<string>();
        foreach (var arr in SuccessConfirmationKeywords.Values)
        {
            all.AddRange(arr);
        }
        return all;
    }

    /// <summary>
    /// Suy luận danh mục ReportReasonCategory từ chuỗi văn bản lấy được từ UI.
    /// </summary>
    public static ReportReasonCategory? ResolveCategoryFromText(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return null;
        }

        foreach (var kvp in CategoryKeywords)
        {
            foreach (var langPair in kvp.Value)
            {
                foreach (var keyword in langPair.Value)
                {
                    if (rawText.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        return kvp.Key;
                    }
                }
            }
        }

        return null;
    }
}
