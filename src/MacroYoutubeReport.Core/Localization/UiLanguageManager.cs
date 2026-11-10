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

    private static readonly Dictionary<string, (string Kr, string En)> UiTranslations = new(StringComparer.OrdinalIgnoreCase)
    {
        // Platform / Auth
        ["Đăng nhập & Nền tảng"] = ("로그인 & 플랫폼", "Login & Platform"),
        ["Android (ADB)"] = ("안드로이드 (ADB)", "Android (ADB)"),
        ["Chrome (Playwright)"] = ("크롬 (Playwright)", "Chrome (Playwright)"),
        ["Chọn file account.txt..."] = ("계정 파일 선택...", "Select account file..."),
        ["Chọn file proxy.txt..."] = ("프록시 파일 선택...", "Select proxy file..."),
        ["Chọn"] = ("선택", "Browse"),

        // Target Input
        ["Cài đặt Mục tiêu"] = ("목표 설정", "Target Settings"),
        ["Loại:"] = ("구분:", "Type:"),
        ["Từ khóa:"] = ("키워드:", "Keyword:"),
        ["Tên kênh:"] = ("채널명:", "Channel:"),
        ["Tiêu đề:"] = ("제목:", "Title:"),
        ["URL:"] = ("URL:", "URL:"),
        ["+ Thêm"] = ("+ 추가", "+ Add"),
        ["Nhập file"] = ("파일 불러오기", "Import File"),
        ["Xóa"] = ("삭제", "Delete"),

        // Report Settings
        ["Bộ lọc Tiêu chí Báo cáo"] = ("신고 기준 필터", "Report Criteria Filter"),
        ["Kịch bản:"] = ("시나리오:", "Scenario:"),
        ["Lý do:"] = ("사유:", "Reason:"),
        ["Xem (giây):"] = ("시청 (초):", "Watch (sec):"),
        ["đến"] = ("~", "to"),
        ["Nghỉ (s):"] = ("휴식 (초):", "Cooldown (s):"),
        ["Luồng:"] = ("스레드:", "Threads:"),
        ["Bỏ qua quảng cáo (Skip Ad)"] = ("광고 건너뛰기 (Skip Ad)", "Auto Skip Ads"),
        ["Mô phỏng người thật (cuộn đọc comment)"] = ("인간 시뮬레이션 (댓글 스크롤)", "Human Simulation (Scroll Comments)"),
        ["Xem ngẫu nhiên (Random watch)"] = ("랜덤 시청 (Random watch)", "Random Watch"),
        ["Tự động xoay vòng lý do vi phạm"] = ("위반 사유 자동 순환", "Auto Rotate Reasons"),
        ["Tự động phân bổ tài khoản Proxy"] = ("프록시 계정 자동 할당", "Auto Allocate Proxies"),

        // Left bottom action buttons
        ["Quét thiết bị"] = ("기기 검색", "Scan Devices"),
        ["Bắt đầu"] = ("시작", "Start"),
        ["Dừng"] = ("중지", "Stop"),
        ["Thoát"] = ("종료", "Exit"),

        // Top Action Bar
        ["Home"] = ("홈", "Home"),
        ["Đóng tất cả"] = ("모두 닫기", "Close All"),
        ["Toàn màn hình"] = ("전체 화면", "Full Screen"),
        ["Kiểm tra tài khoản"] = ("계정 확인", "Check Accounts"),
        ["Kiểm tra trên YouTube"] = ("YouTube 확인", "Check on YouTube"),
        ["Phân bổ lại tài khoản"] = ("계정 재할당", "Reallocate Accounts"),
        ["Xóa cache YouTube"] = ("YouTube 캐시 삭제", "Clear YT Cache"),
        ["Quét gom TCP"] = ("TCP 검색 및 통합", "Scan & Gather TCP"),
        ["Xuất báo cáo CSV"] = ("CSV 보고서 내보내기", "Export CSV Report"),
        ["Dọn tác vụ xong"] = ("완료 작업 정리", "Clear Completed"),

        // Filter Bar
        ["Tất cả"] = ("전체", "All"),
        ["Hoàn thành"] = ("완료", "Done"),
        ["Đang chạy"] = ("실행 중", "Running"),
        ["Thất bại"] = ("실패", "Failed"),
        ["Tìm"] = ("찾기", "Find"),
        ["Trùng"] = ("중복", "Duplicates"),
        ["Sao chép"] = ("복사", "Duplicate"),

        // Task Grid Headers
        ["Khởi động lại"] = ("재시작", "Restart"),
        ["Số thiết bị"] = ("기기수", "Devices"),
        ["Từ khóa"] = ("키워드", "Keyword"),
        ["Tên kênh"] = ("채널명", "Channel"),
        ["Tên tiêu đề"] = ("제목", "Title"),
        ["URL YouTube"] = ("YouTube URL", "YouTube URL"),
        ["Khoảng chạy"] = ("실행 간격", "Interval"),
        ["Bắt đầu ngẫu nhiên"] = ("랜덤 시작", "Random Start"),
        ["Trực tiếp"] = ("라이브", "Live"),
        ["Đã chạy"] = ("진행수", "Progress"),
        ["Trạng thái"] = ("상태", "Status"),

        // Device Grid Headers
        ["STT"] = ("번호", "No"),
        ["IP / Proxy"] = ("IP / 프록시", "IP / Proxy"),
        ["Serial thiết bị"] = ("기기 시리얼", "Device Serial"),
        ["Thiết bị"] = ("기기 모델", "Device Model"),
        ["Tài khoản"] = ("계정", "Account"),
        ["Mật khẩu"] = ("비밀번호", "Password"),
        ["Mục tiêu đang chạy"] = ("실행 중 목표", "Current Target"),

        // Menu Items
        ["Menu"] = ("메뉴", "Menu"),
        ["Mở thư mục Logs"] = ("로그 폴더 열기", "Open Logs Folder"),
        ["Nhập tài khoản Gmail..."] = ("지메일 계정 불러오기...", "Import Gmail Accounts..."),
        ["Nhập danh sách Proxy..."] = ("프록시 목록 불러오기...", "Import Proxies..."),
        ["Khác"] = ("기타", "Tools"),
        ["Xóa Cache ứng dụng YouTube"] = ("YouTube 앱 캐시 삭제", "Clear YouTube App Cache"),
        ["Khởi động lại ADB Server"] = ("ADB 서버 재시작", "Restart ADB Server"),
        ["Lệnh ADB Console..."] = ("ADB 콘솔 명령...", "ADB Console..."),
        ["Help"] = ("도움말", "Help"),
        ["Hướng dẫn sử dụng"] = ("사용 안내", "User Guide"),
        ["Thông tin phiên bản"] = ("버전 정보", "About"),

        // Headers
        ["Nhật Ký Thực Thi Thời Gian Thực (Live Log)"] = ("실시간 실행 로그 (Live Log)", "Real-time Execution Live Log")
    };

    /// <summary>
    /// Dịch chuỗi giao diện người dùng theo ngôn ngữ chỉ định hoặc ngôn ngữ hiện tại.
    /// </summary>
    public static string Translate(string text, AppLanguage? language = null)
    {
        var lang = language ?? CurrentLanguage;
        if (lang == AppLanguage.Vietnamese || string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        if (UiTranslations.TryGetValue(text, out var entry))
        {
            return lang == AppLanguage.Korean ? entry.Kr : entry.En;
        }

        string trimmed = text.Trim();
        if (UiTranslations.TryGetValue(trimmed, out entry))
        {
            string translated = lang == AppLanguage.Korean ? entry.Kr : entry.En;
            return text.Replace(trimmed, translated);
        }

        return text;
    }
}
