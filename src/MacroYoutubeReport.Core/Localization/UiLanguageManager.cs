using System.Text.RegularExpressions;
using MacroYoutubeReport.Core.Enums;

namespace MacroYoutubeReport.Core.Localization;

/// <summary>
/// Quản lý từ điển đa ngôn ngữ cho toàn bộ các text, nhãn và locators nhận diện trên YouTube (VI, EN, KO).
/// Hỗ trợ tìm kiếm mờ (fuzzy match) và so khớp danh mục báo cáo chính xác.
/// </summary>
public static class UiLanguageManager
{
    public static AppLanguage CurrentLanguage { get; set; } = AppLanguage.Korean;

    private static readonly Dictionary<ReportReasonCategory, Dictionary<AppLanguage, string[]>> CategoryKeywords = new()
    {
        [ReportReasonCategory.SpamOrMisleading] = new()
        {
            [AppLanguage.Vietnamese] = ["Spam hoặc gây hiểu nhầm", "Spam hoặc", "hiểu nhầm", "Spam"],
            [AppLanguage.English] = ["Spam or misleading", "Spam or misleading content", "Spam", "Misleading"],
            [AppLanguage.Korean] = ["스팸 또는 혼동을 야기하는 콘텐츠", "스팸 또는 혼동", "스팸", "혼동"]
        },
        [ReportReasonCategory.FinancialOrCryptoScam] = new()
        {
            [AppLanguage.Vietnamese] = ["Gian lận hoặc lừa đảo", "lừa đảo tài chính", "tiền ảo", "lừa đảo", "Spam hoặc gây hiểu nhầm"],
            [AppLanguage.English] = ["Scams or fraud", "Financial scam", "Crypto scam", "Fraud", "Spam or misleading"],
            [AppLanguage.Korean] = ["사기 또는 혼동", "사기", "금융 사기", "스팸 또는 혼동을 야기하는 콘텐츠"]
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
            [AppLanguage.Korean] = ["증오 또는 악의적인 콘텐츠", "증오 발언", "악의적인 콘텐츠", "증오", "악의적"]
        },
        [ReportReasonCategory.HarassmentOrBullying] = new()
        {
            [AppLanguage.Vietnamese] = ["Quấy rối hoặc bắt nạt", "Quấy rối", "bắt nạt"],
            [AppLanguage.English] = ["Harassment or bullying", "Harassment", "Bullying"],
            [AppLanguage.Korean] = ["괴롭힘 또는 폭력", "괴롭힘", "사이버 불링", "폭력"]
        },
        [ReportReasonCategory.HarmfulDangerousActs] = new()
        {
            [AppLanguage.Vietnamese] = ["Hành vi có hại hoặc nguy hiểm", "nguy hiểm", "có hại"],
            [AppLanguage.English] = ["Harmful or dangerous acts", "Harmful acts", "Dangerous acts"],
            [AppLanguage.Korean] = ["유해하거나 위험한 행위", "유해한 행위", "위험한 행위", "유해", "위험"]
        },
        [ReportReasonCategory.Misinformation] = new()
        {
            [AppLanguage.Vietnamese] = ["Thông tin sai lệch", "sai lệch", "tin giả"],
            [AppLanguage.English] = ["Misinformation", "Misleading info", "False information"],
            [AppLanguage.Korean] = ["잘못된 정보", "허위 정보", "오해의 소지가 있는 정보", "오해", "허위"]
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
        [AppLanguage.Vietnamese] = ["Cảm ơn bạn đã báo cáo", "Đã gửi báo cáo", "Cảm ơn bạn đã phản hồi", "Báo cáo của bạn", "Cảm ơn"],
        [AppLanguage.English] = ["Thanks for reporting", "Report submitted", "Thank you for reporting", "We received your report", "Thanks", "Thank you"],
        [AppLanguage.Korean] = ["커뮤니티에 도움을 주셔서 감사합니다", "신고해 주셔서 감사합니다", "신고가 접수되었습니다", "의견을 보내주셔서 감사합니다", "감사합니다", "신고해 주신 내용"]
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
        ["LiveStream"] = ("실시간 스트림", "Live Stream"),
        ["Live Stream (Trực tiếp)"] = ("실시간 스트림", "Live Stream"),
        ["6. Báo cáo Livestream lừa đảo tài chính / crypto"] = ("6. 금융/가상화폐 사기 실시간 스트림 신고", "6. Report Financial/Crypto Scam Live Stream"),
        ["Lừa đảo tài chính / Tiền ảo (Scams & Fraud)"] = ("금융/가상화폐 사기 (Scams & Fraud)", "Financial / Crypto Scam (Scams & Fraud)"),
        ["FinancialOrCryptoScam"] = ("금융 사기", "Financial/Crypto Scam"),
        ["Từ khóa:"] = ("키워드:", "Keyword:"),
        ["Tên kênh:"] = ("채널명:", "Channel:"),
        ["Tiêu đề:"] = ("제목:", "Title:"),
        ["URL:"] = ("URL:", "URL:"),
        ["+ Thêm"] = ("+ 추가", "+ Add"),
        ["Nhập file"] = ("파일 불러오기", "Import File"),
        ["Xóa"] = ("삭제", "Delete"),

        // Search Filter (AutoYouTube Style)
        ["Bộ lọc tìm kiếm"] = ("검색 필터", "Search Filter"),
        ["Phân loại"] = ("구분", "Type"),
        ["Độ dài"] = ("길이", "Duration"),
        ["Ngày tải lên"] = ("업로드 날짜", "Upload Date"),
        ["Ưu tiên"] = ("우선순위", "Sort By"),
        ["Không dùng"] = ("사용 안함", "None"),
        ["Hôm nay"] = ("오늘", "Today"),
        ["Giờ qua"] = ("지난 1시간", "Last Hour"),
        ["Tuần này"] = ("이번 주", "This Week"),
        ["Tháng này"] = ("이번 달", "This Month"),
        ["Năm nay"] = ("올해", "This Year"),
        ["Dưới 4 phút"] = ("4분 미만", "Under 4 min"),
        ["4 - 20 phút"] = ("4~20분", "4 - 20 min"),
        ["Trên 20 phút"] = ("20분 초과", "Over 20 min"),
        ["Số lượt xem"] = ("조회수", "View Count"),
        ["Xếp hạng"] = ("평점", "Rating"),
        ["Live (Trực tiếp)"] = ("라이브 (실시간)", "Live Stream"),
        ["Commons"] = ("크리에이티브 커먼즈", "Commons"),
        ["Phụ đề"] = ("자막", "Subtitles"),
        ["4K"] = ("4K", "4K"),
        ["Xem ngẫu nhiên"] = ("랜덤 시청", "Random Watch"),
        ["URL"] = ("URL", "URL"),
        ["Tự động phân bổ email"] = ("이메일 자동 할당", "Auto Allocate Email"),
        ["Tìm kiếm lại"] = ("재검색", "Re-Search"),
        ["Check LaixiKeyBoard"] = ("LaixiKeyBoard 확인", "Check LaixiKeyBoard"),

        // Report Settings
        ["Bộ lọc Tiêu chí Báo cáo"] = ("신고 기준 필터", "Report Criteria Filter"),
        ["Kịch bản:"] = ("시나리오:", "Scenario:"),
        ["Lý do:"] = ("사유:", "Reason:"),
        ["Tệp lý do:"] = ("사유 파일:", "Reason File:"),
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
        ["Kết nối"] = ("기기 연결", "Connect"),
        ["⚙ Cài đặt nâng cao..."] = ("⚙ 고급 설정...", "⚙ Advanced Settings..."),
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

        // Task Grid Headers & Action Column
        ["Hành động"] = ("작업", "Actions"),
        ["Thao tác ▾"] = ("작업 ▾", "Actions ▾"),
        ["Thao tác"] = ("작업", "Actions"),
        ["Ngẫu nhiên"] = ("랜덤", "Random"),
        ["Bắt đầu ngẫu nhiên"] = ("랜덤 시작", "Random Start"),
        ["Ngày tải"] = ("업로드 날짜", "Upload Date"),
        ["Video 4K"] = ("4K 동영상", "4K Video"),
        ["Phụ đề"] = ("자막", "Subtitles"),
        ["Commons"] = ("크리에이티브 커먼즈", "Commons"),
        ["Live (Trực tiếp)"] = ("실시간 (라이브)", "Live"),
        ["Phân loại"] = ("분류", "Type"),
        ["Độ dài"] = ("길이", "Duration"),
        ["Ưu tiên"] = ("정렬", "Sort By"),
        ["Bộ lọc tìm kiếm"] = ("검색 필터", "Search Filters"),
        ["Bộ lọc Tiêu chí Báo cáo"] = ("신고 기준 필터", "Report Criteria Filters"),
        ["Kịch bản:"] = ("시나리오:", "Scenario:"),
        ["Lý do:"] = ("사유:", "Reason:"),
        ["Cài đặt nâng cao & Mô phỏng tương tác"] = ("고급 설정 및 상호작용 시뮬레이션", "Advanced Settings & Simulation"),
        ["Thời gian & Luồng chạy"] = ("시간 및 스레드 설정", "Timing & Threads"),
        ["Xem video (giây):"] = ("동영상 시청(초):", "Watch Duration (s):"),
        ["đến"] = ("~", "to"),
        ["Nghỉ giữa lượt (s):"] = ("대기 시간(초):", "Cooldown (s):"),
        ["Số luồng:"] = ("스레드 수:", "Threads:"),
        ["Tệp nội dung lý do báo cáo"] = ("신고 사유 내용 파일", "Report Reasons File"),
        ["Hành vi tương tác & Tự động hóa"] = ("상호작용 및 자동화 동작", "Engagement & Automation"),
        ["Lưu cài đặt"] = ("설정 저장", "Save Settings"),
        ["Hủy bỏ"] = ("취소", "Cancel"),
        ["▶ Bắt đầu mục tiêu này"] = ("▶ 시작 (개별 실행)", "▶ Start This Target"),
        ["🔄 Khởi động lại (Làm mới & Chạy)"] = ("🔄 재시작 (초기화 후 실행)", "🔄 Restart Target"),
        ["⏹ Dừng mục tiêu"] = ("⏹ 중지", "⏹ Stop Target"),
        ["❌ Xóa khỏi danh sách"] = ("❌ 목록에서 삭제", "❌ Delete From List"),
        ["Khởi động app"] = ("앱 시작", "Launch App"),
        ["Tìm kiếm từ khóa"] = ("키워드 검색", "Search Keyword"),
        ["Chọn video mục tiêu"] = ("대상 영상 선택", "Select Target Video"),
        ["Xem video tự nhiên"] = ("자연스러운 시청", "Watch Naturally"),
        ["Mở menu Báo cáo"] = ("신고 메뉴 열기", "Open Report Menu"),
        ["Gửi báo cáo"] = ("신고 제출", "Submit Report"),
        ["Xác thực kết quả"] = ("결과 확인", "Verify Result"),
        ["Vào livestream"] = ("라이브 스트림 입장", "Enter Live Stream"),
        ["Báo cáo vi phạm"] = ("위반 신고", "Report Violation"),
        ["Xác nhận thành công"] = ("접수 확인 성공", "Confirmed Success"),
        ["Hoàn thành báo cáo"] = ("신고 완료", "Report Completed"),

        // Headers & Live Log
        ["Nhật Ký Thực Thi Thời Gian Thực (Live Log)"] = ("실시간 실행 로그 (Live Log)", "Real-time Execution Live Log"),
        ["Nhật Ký:"] = ("실시간 로그:", "Live Log:"),
        ["Cuộn"] = ("자동 스크롤", "Auto Scroll"),
        ["Mở Logs"] = ("로그 열기", "Open Logs"),
        ["Tất cả thiết bị"] = ("모든 기기", "All devices"),

        // Scenarios
        ["1. Tìm kiếm, xem và báo cáo"] = ("1. 검색, 시청 및 신고", "1. Search, watch and report"),
        ["2. Truy cập URL trực tiếp và báo cáo"] = ("2. URL 직접 접속 및 신고", "2. Direct URL access and report"),
        ["3. Báo cáo kênh vi phạm bản quyền/spam"] = ("3. 저작권 침해/스팸 채널 신고", "3. Report copyright/spam channel"),
        ["4. Báo cáo video Shorts"] = ("4. Shorts 동영상 신고", "4. Report Shorts video"),
        ["5. Báo cáo bình luận độc hại"] = ("5. 악성 댓글 신고", "5. Report harmful comment"),

        // Reasons
        ["Spam hoặc gây hiểu lầm"] = ("스팸 또는 혼동을 야기하는 콘텐츠", "Spam or misleading"),
        ["Hành vi có hại hoặc nguy hiểm"] = ("유해하거나 위험한 행위", "Harmful or dangerous acts"),
        ["Thông tin sai lệch"] = ("잘못된 정보 (허위 정보)", "Misinformation"),
        ["Nội dung kích động thù địch"] = ("증오 또는 악의적인 콘텐츠", "Hateful or abusive content"),
        ["Quấy rối hoặc bắt nạt"] = ("괴롭힘 또는 사이버 폭력", "Harassment or bullying"),
        ["Bạo lực hoặc phản cảm"] = ("폭력적 또는 혐오스러운 콘텐츠", "Violent or repulsive content"),
        ["Nội dung khiêu dâm"] = ("성적인 콘텐츠", "Sexual content"),
        ["Lạm dụng trẻ em"] = ("아동 학대", "Child abuse"),
        ["Khủng bố"] = ("테러 조장", "Promotes terrorism"),
        ["Vi phạm bản quyền của tôi"] = ("내 권리 침해 (저작권/개인정보)", "Infringes my rights"),
        ["Tất cả danh mục (Xoay vòng ngẫu nhiên)"] = ("모든 카테고리 (무작위 순환)", "All categories (Random rotation)"),

        // Additional Filter Types
        ["Danh sách phát"] = ("재생목록", "Playlist"),
        ["Phim"] = ("영화", "Movie"),
        ["Shorts"] = ("Shorts", "Shorts")
    };

    private static readonly Dictionary<string, (string Kr, string En)> LogDirectTranslations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Hệ thống đã sẵn sàng."] = ("시스템이 준비되었습니다.", "System is ready."),
        ["Đang khởi động hệ thống Macro YouTube Report..."] = ("Macro YouTube Report 시스템을 시작하는 중...", "Starting Macro YouTube Report system..."),
        ["Đang quét danh sách thiết bị Android qua ADB..."] = ("ADB를 통해 Android 기기 목록을 검색 중...", "Scanning Android devices via ADB..."),
        ["Quét thiết bị hoàn tất."] = ("기기 검색이 완료되었습니다.", "Device scan completed."),
        ["Đang dừng toàn bộ tác vụ..."] = ("모든 작업을 중지하는 중...", "Stopping all tasks..."),
        ["Đã dừng hàng đợi tác vụ."] = ("작업 대기열이 중지되었습니다.", "Task queue stopped."),
        ["Đã hoàn tất tất cả mục tiêu trong hàng đợi."] = ("대기열의 모든 대상 처리가 완료되었습니다.", "All targets in queue completed."),
        ["Đang quét gom TCP các thiết bị ADB..."] = ("ADB 기기 TCP 검색 및 연결 중...", "Scanning & connecting TCP ADB devices..."),
        ["Đã dọn dẹp các tác vụ hoàn thành khỏi danh sách."] = ("완료된 작업을 목록에서 정리했습니다.", "Cleared completed tasks from list."),
        ["Không tìm thấy thiết bị nào để dọn cache."] = ("캐시를 삭제할 기기를 찾을 수 없습니다.", "No devices found to clear cache."),
        ["Không tìm thấy thiết bị nào khả dụng để gửi lệnh Home."] = ("홈 명령을 보낼 수 있는 기기를 찾을 수 없습니다.", "No available devices to send Home command."),
        ["Không tìm thấy thiết bị nào khả dụng để đóng ứng dụng."] = ("앱을 종료할 수 있는 기기를 찾을 수 없습니다.", "No available devices to close apps."),
        ["ADB Server đã được khởi động lại thành công."] = ("ADB 서버가 성공적으로 재시작되었습니다.", "ADB Server restarted successfully."),
        ["Đang khởi động lại ADB Server..."] = ("ADB 서버 재시작 중...", "Restarting ADB Server..."),
        ["Đã nhân bản dòng tác vụ đã chọn."] = ("선택한 작업 라인이 복제되었습니다.", "Duplicated selected task row."),
        ["Trang chủ YouTube đã mở và sẵn sàng."] = ("YouTube 홈 화면이 열렸으며 준비되었습니다.", "YouTube home screen opened and ready."),
        ["Đã tìm thấy và mở video mục tiêu thành công."] = ("대상 영상을 찾아 성공적으로 열었습니다.", "Found and opened target video successfully."),
        ["Đã mở thành công bảng chọn lý do báo cáo."] = ("신고 사유 선택 창을 성공적으로 열었습니다.", "Opened report reason dialog successfully."),
        ["Báo cáo hoàn tất."] = ("신고가 완료되었습니다.", "Report completed."),
        ["Báo cáo kênh thành công."] = ("채널 신고가 완료되었습니다.", "Channel reported successfully."),
        ["Video Shorts đã mở."] = ("Shorts 영상이 열렸습니다.", "Shorts video opened."),
        ["Báo cáo Shorts hoàn tất."] = ("Shorts 신고가 완료되었습니다.", "Shorts report completed."),
        ["Video chứa bình luận đã mở."] = ("댓글이 포함된 영상이 열렸습니다.", "Video containing comment opened."),
        ["Báo cáo bình luận thành công."] = ("댓글 신고가 완료되었습니다.", "Comment reported successfully."),
        ["Đã mở luồng phát trực tiếp thành công."] = ("라이브 스트림을 성공적으로 열었습니다.", "Opened live stream successfully."),
        ["YouTube đã xác nhận tiếp nhận báo cáo livestream vi phạm."] = ("YouTube에서 라이브 스트림 위반 신고 접수를 확인했습니다.", "YouTube confirmed receipt of live report."),
        ["Đã gửi báo cáo (chờ YouTube duyệt tiếp nhận)."] = ("신고를 전송했습니다 (접수 대기 중).", "Report submitted (awaiting review).")
    };

    private static readonly (Regex Regex, Func<Match, AppLanguage, string> Formatter)[] LogPatterns = new (Regex, Func<Match, AppLanguage, string>)[]
    {
        // [deviceId] [stepName] message
        (new Regex(@"^\[([^\]]+)\]\s*\[([^\]]+)\]\s*(.*)$"), (m, lang) =>
        {
            string dev = m.Groups[1].Value;
            string step = m.Groups[2].Value;
            string msg = m.Groups[3].Value;
            string transStep = Translate(step, lang);
            string transMsg = TranslateLog(msg, lang);
            return $"[{dev}] [{transStep}] {transMsg}";
        }),
        // Đã thêm mục tiêu vào hàng đợi: {title}
        (new Regex(@"^Đã thêm mục tiêu vào hàng đợi:\s*(.*)$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"대기열에 대상 추가됨: {m.Groups[1].Value}" : $"Target added to queue: {m.Groups[1].Value}"),
        // Bắt đầu xử lý: [{dev}] -> {target}
        (new Regex(@"^Bắt đầu xử lý:\s*\[([^\]]+)\]\s*->\s*(.*)$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"처리 시작: [{m.Groups[1].Value}] -> {m.Groups[2].Value}" : $"Start processing: [{m.Groups[1].Value}] -> {m.Groups[2].Value}"),
        // Thành công: Đã báo cáo mục tiêu [{target}] trên [{dev}]!
        (new Regex(@"^Thành công:\s*Đã báo cáo mục tiêu\s*\[([^\]]+)\]\s*trên\s*\[([^\]]+)\]!$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"성공: [{m.Groups[2].Value}]에서 [{m.Groups[1].Value}] 대상을 성공적으로 신고했습니다!" : $"Success: Reported [{m.Groups[1].Value}] on [{m.Groups[2].Value}]!"),
        // Thất bại: [{target}] trên [{dev}] - Lỗi: {err}
        (new Regex(@"^Thất bại:\s*\[([^\]]+)\]\s*trên\s*\[([^\]]+)\]\s*-\s*Lỗi:\s*(.*)$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"실패: [{m.Groups[2].Value}]에서 [{m.Groups[1].Value}] - 오류: {m.Groups[3].Value}" : $"Failed: [{m.Groups[1].Value}] on [{m.Groups[2].Value}] - Error: {m.Groups[3].Value}"),
        // Bắt đầu thực thi hàng loạt {N} mục tiêu trên {M}...
        (new Regex(@"^Bắt đầu thực thi hàng loạt\s*(\d+)\s*mục tiêu trên\s*(.*?)\.\.\.$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"{m.Groups[2].Value}에서 {m.Groups[1].Value}개 대상 일괄 실행 시작..." : $"Starting batch execution of {m.Groups[1].Value} targets on {m.Groups[2].Value}..."),
        // Đã xếp {N} tác vụ vào hàng đợi...
        (new Regex(@"^Đã xếp\s*(\d+)\s*tác vụ vào hàng đợi.*$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"{m.Groups[1].Value}개 작업이 대기열에 추가되었습니다." : $"{m.Groups[1].Value} tasks queued."),
        // Đang gửi lệnh Home tới {N} thiết bị...
        (new Regex(@"^Đang gửi lệnh Home tới\s*(\d+)\s*thiết bị\.\.\.$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"{m.Groups[1].Value}개 기기에 홈 화면 이동 명령 전송 중..." : $"Sending Home command to {m.Groups[1].Value} devices..."),
        // Thiết bị [{dev}]: Đã ra màn hình Home thành công.
        (new Regex(@"^Thiết bị\s*\[([^\]]+)\]:\s*Đã ra màn hình Home thành công\.$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"기기 [{m.Groups[1].Value}]: 홈 화면 이동 완료." : $"Device [{m.Groups[1].Value}]: Moved to Home successfully."),
        // Thiết bị [{dev}]: Không thể gửi phím Home.
        (new Regex(@"^Thiết bị\s*\[([^\]]+)\]:\s*Không thể gửi phím Home\.$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"기기 [{m.Groups[1].Value}]: 홈 키 전송 실패." : $"Device [{m.Groups[1].Value}]: Failed to send Home key."),
        // Thiết bị [{dev}]: Chưa sẵn sàng kết nối ADB.
        (new Regex(@"^Thiết bị\s*\[([^\]]+)\]:\s*Chưa sẵn sàng kết nối ADB\.$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"기기 [{m.Groups[1].Value}]: ADB 연결 준비되지 않음." : $"Device [{m.Groups[1].Value}]: ADB connection not ready."),
        // Hoàn tất: Đã đưa {N}/{M} thiết bị về màn hình Home.
        (new Regex(@"^Hoàn tất:\s*Đã đưa\s*(\d+)/(\d+)\s*thiết bị về màn hình Home\.$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"완료: {m.Groups[1].Value}/{m.Groups[2].Value}개 기기 홈 화면 이동 완료." : $"Completed: Moved {m.Groups[1].Value}/{m.Groups[2].Value} devices to Home."),
        // Đang đóng tất cả ứng dụng/tab trên {N} thiết bị...
        (new Regex(@"^Đang đóng tất cả ứng dụng/tab trên\s*(\d+)\s*thiết bị\.\.\.$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"{m.Groups[1].Value}개 기기의 모든 앱/탭 종료 중..." : $"Closing all apps/tabs on {m.Groups[1].Value} devices..."),
        // Thiết bị [{dev}]: Đã đóng tất cả ứng dụng và tab thành công.
        (new Regex(@"^Thiết bị\s*\[([^\]]+)\]:\s*Đã đóng tất cả ứng dụng và tab thành công\.$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"기기 [{m.Groups[1].Value}]: 모든 앱 및 탭 종료 완료." : $"Device [{m.Groups[1].Value}]: Closed all apps and tabs successfully."),
        // Hoàn tất: Đã đóng tất cả ứng dụng trên {N}/{M} thiết bị.
        (new Regex(@"^Hoàn tất:\s*Đã đóng tất cả ứng dụng trên\s*(\d+)/(\d+)\s*thiết bị\.$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"완료: {m.Groups[1].Value}/{m.Groups[2].Value}개 기기의 모든 앱 종료 완료." : $"Completed: Closed all apps on {m.Groups[1].Value}/{m.Groups[2].Value} devices."),
        // Đang gửi lệnh dọn bộ nhớ cache ứng dụng YouTube trên {N} thiết bị...
        (new Regex(@"^Đang gửi lệnh dọn bộ nhớ cache ứng dụng YouTube trên\s*(\d+)\s*thiết bị\.\.\.$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"{m.Groups[1].Value}개 기기의 YouTube 앱 캐시 삭제 명령 전송 중..." : $"Sending YouTube cache clear command to {m.Groups[1].Value} devices..."),
        // Đã dọn cache YouTube trên thiết bị [{dev}]
        (new Regex(@"^Đã dọn cache YouTube trên thiết bị\s*\[([^\]]+)\]$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"기기 [{m.Groups[1].Value}] YouTube 캐시 삭제 완료." : $"Cleared YouTube cache on device [{m.Groups[1].Value}]."),
        // Đã chọn file tài khoản Gmail: {0}
        (new Regex(@"^Đã chọn file tài khoản Gmail:\s*(.*)$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"Gmail 계정 파일 선택됨: {m.Groups[1].Value}" : $"Selected Gmail account file: {m.Groups[1].Value}"),
        // Đã chọn file Proxy: {0}
        (new Regex(@"^Đã chọn file Proxy:\s*(.*)$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"프록시 파일 선택됨: {m.Groups[1].Value}" : $"Selected Proxy file: {m.Groups[1].Value}"),
        // Đã chọn file nội dung lý do báo cáo: {0}
        (new Regex(@"^Đã chọn file nội dung lý do báo cáo:\s*(.*)$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"신고 사유 파일 선택됨: {m.Groups[1].Value}" : $"Selected report reason file: {m.Groups[1].Value}"),
        // Đã cập nhật cài đặt: Xem {0}-{1}s | Nghỉ {2}s | {3} luồng.
        (new Regex(@"^Đã cập nhật cài đặt:\s*Xem\s*(\d+)-(\d+)s\s*\|\s*Nghỉ\s*(\d+)s\s*\|\s*(\d+)\s*luồng\.$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"설정 업데이트 완료: 시청 {m.Groups[1].Value}-{m.Groups[2].Value}초 | 대기 {m.Groups[3].Value}초 | {m.Groups[4].Value} 스레드." : $"Settings updated: Watch {m.Groups[1].Value}-{m.Groups[2].Value}s | Cooldown {m.Groups[3].Value}s | {m.Groups[4].Value} threads."),
        // Đã nhập {N} mục tiêu từ file {file}
        (new Regex(@"^Đã nhập\s*(\d+)\s*mục tiêu từ file\s*(.*)$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"{m.Groups[2].Value} 파일에서 {m.Groups[1].Value}개 대상을 불러왔습니다." : $"Imported {m.Groups[1].Value} targets from file {m.Groups[2].Value}"),
        // Đã xóa mục tiêu tại dòng #{N}
        (new Regex(@"^Đã xóa mục tiêu tại dòng\s*#(\d+)$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"#{m.Groups[1].Value}번 라인의 대상이 삭제되었습니다." : $"Deleted target at line #{m.Groups[1].Value}."),
        // Đã loại bỏ {N} mục tiêu trùng lặp.
        (new Regex(@"^Đã loại bỏ\s*(\d+)\s*mục tiêu trùng lặp\.$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"{m.Groups[1].Value}개의 중복 대상이 제거되었습니다." : $"Removed {m.Groups[1].Value} duplicate targets."),
        // Đã gõ tìm kiếm từ khóa: '{0}'.
        (new Regex(@"^Đã gõ tìm kiếm từ khóa:\s*'(.*)'\.$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"'{m.Groups[1].Value}' 키워드 검색 입력 완료." : $"Searched keyword: '{m.Groups[1].Value}'."),
        // Đã xem tự nhiên {N}s...
        (new Regex(@"^Đã xem tự nhiên\s*(\d+)s\s*(.*)$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"실제 사용자 시뮬레이션과 함께 {m.Groups[1].Value}초 시청 완료." : $"Watched naturally for {m.Groups[1].Value}s with user simulation."),
        // Đã chọn danh mục '{0}' và gửi báo cáo thành công.
        (new Regex(@"^Đã chọn danh mục\s*'(.*)'\s*và gửi báo cáo thành công\.$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"'{m.Groups[1].Value}' 카테고리를 선택하고 성공적으로 신고를 제출했습니다." : $"Selected '{m.Groups[1].Value}' and submitted report successfully."),
        // Đã mở trang kênh: '{0}'.
        (new Regex(@"^Đã mở trang kênh:\s*'(.*)'\.$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"'{m.Groups[1].Value}' 채널 페이지를 열었습니다." : $"Opened channel page: '{m.Groups[1].Value}'."),
        // Đã gửi báo cáo kênh với lý do: {0}
        (new Regex(@"^Đã gửi báo cáo kênh với lý do:\s*(.*)$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"'{m.Groups[1].Value}' 사유로 채널 신고를 제출했습니다." : $"Submitted channel report with reason: {m.Groups[1].Value}"),
        // Đã xem livestream {N}s...
        (new Regex(@"^Đã xem livestream\s*(\d+)s\s*(.*)$"), (m, lang) =>
            lang == AppLanguage.Korean ? $"라이브 {m.Groups[1].Value}초 시청 및 실시간 채팅 읽기 시뮬레이션 완료." : $"Watched live for {m.Groups[1].Value}s with live chat simulation.")
    };

    /// <summary>
    /// Bản địa hóa nội dung nhật ký (Live Log) theo ngôn ngữ cài đặt của hệ thống.
    /// </summary>
    public static string TranslateLog(string text, AppLanguage? language = null)
    {
        var lang = language ?? CurrentLanguage;
        if (lang == AppLanguage.Vietnamese || string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        string trimmed = text.Trim();

        // 1. Tìm kiếm trong bảng dịch trực tiếp
        if (LogDirectTranslations.TryGetValue(trimmed, out var direct))
        {
            return lang == AppLanguage.Korean ? direct.Kr : direct.En;
        }

        if (UiTranslations.TryGetValue(trimmed, out var uiTrans))
        {
            return lang == AppLanguage.Korean ? uiTrans.Kr : uiTrans.En;
        }

        // 2. So khớp theo mẫu Regex
        foreach (var (regex, formatter) in LogPatterns)
        {
            var match = regex.Match(trimmed);
            if (match.Success)
            {
                return formatter(match, lang);
            }
        }

        return text;
    }

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

    /// <summary>
    /// Lấy thông tin hướng dẫn cấu trúc file (Tooltip ngắn và Nội dung chi tiết kèm tiêu đề) theo ngôn ngữ.
    /// fileType: "account", "proxy", "target", "reason"
    /// </summary>
    public static (string Tooltip, string DialogTitle, string DialogContent) GetFileFormatHelp(string fileType, AppLanguage? language = null)
    {
        var lang = language ?? CurrentLanguage;
        return fileType.ToLowerInvariant() switch
        {
            "account" => lang switch
            {
                AppLanguage.Korean => (
                    "형식: 이메일|비밀번호|복구이메일 (선택)\r\n예: user@gmail.com|pass123|recovery@gmail.com",
                    "계정 파일 (account.txt) 형식 안내",
                    "【계정 파일 (account.txt) 형식 안내】\r\n\r\n" +
                    "각 줄마다 하나의 Google/YouTube 계정을 입력합니다:\r\n" +
                    "• 형식 1: 이메일|비밀번호\r\n" +
                    "• 형식 2: 이메일|비밀번호|복구이메일 (선택)\r\n" +
                    "• 형식 3: 이메일:비밀번호:복구이메일\r\n\r\n" +
                    "예시:\r\n" +
                    "user1@gmail.com|Password123|recovery@gmail.com\r\n" +
                    "user2@gmail.com|MySecretPass"
                ),
                AppLanguage.English => (
                    "Format: Email|Password|RecoveryEmail (Optional)\r\nEx: user@gmail.com|pass123|recovery@gmail.com",
                    "Account File (account.txt) Format Guide",
                    "【Account File (account.txt) Format Guide】\r\n\r\n" +
                    "Each line represents one Google/YouTube account:\r\n" +
                    "• Format 1: Email|Password\r\n" +
                    "• Format 2: Email|Password|RecoveryEmail (Optional)\r\n" +
                    "• Format 3: Email:Password:RecoveryEmail\r\n\r\n" +
                    "Examples:\r\n" +
                    "user1@gmail.com|Password123|recovery@gmail.com\r\n" +
                    "user2@gmail.com|MySecretPass"
                ),
                _ => (
                    "Định dạng: Email|Mật khẩu|Email khôi phục (Tùy chọn)\r\nVí dụ: user@gmail.com|pass123|recovery@gmail.com",
                    "Hướng dẫn định dạng file tài khoản (account.txt)",
                    "【HƯỚNG DẪN ĐỊNH DẠNG FILE TÀI KHOẢN (account.txt)】\r\n\r\n" +
                    "Mỗi dòng đại diện cho một tài khoản Google/YouTube:\r\n" +
                    "• Định dạng 1: Email|Mật khẩu\r\n" +
                    "• Định dạng 2: Email|Mật khẩu|Email khôi phục (Tùy chọn)\r\n" +
                    "• Định dạng 3: Email:Mật khẩu:Email khôi phục\r\n\r\n" +
                    "Ví dụ:\r\n" +
                    "user1@gmail.com|Password123|recovery@gmail.com\r\n" +
                    "user2@gmail.com|MatkhauBaoMat456"
                )
            },
            "proxy" => lang switch
            {
                AppLanguage.Korean => (
                    "형식: IP:포트 또는 IP:포트:사용자명:비밀번호\r\n예: 103.77.243.55:10153:admin:secret",
                    "프록시 파일 (proxy.txt) 형식 안내",
                    "【프록시 파일 (proxy.txt) 형식 안내】\r\n\r\n" +
                    "각 줄마다 하나의 HTTP/SOCKS5 프록시 서버를 입력합니다:\r\n" +
                    "• 형식 1 (인증 없음): IP:Port\r\n" +
                    "• 형식 2 (인증 있음): IP:Port:Username:Password\r\n" +
                    "• 형식 3: Username:Password@IP:Port\r\n\r\n" +
                    "예시:\r\n" +
                    "103.77.243.55:10153\r\n" +
                    "103.77.243.55:10154:user123:pass456"
                ),
                AppLanguage.English => (
                    "Format: IP:Port or IP:Port:User:Pass\r\nEx: 103.77.243.55:10153:admin:secret",
                    "Proxy File (proxy.txt) Format Guide",
                    "【Proxy File (proxy.txt) Format Guide】\r\n\r\n" +
                    "Each line represents one HTTP/SOCKS5 proxy server:\r\n" +
                    "• Format 1 (No Auth): IP:Port\r\n" +
                    "• Format 2 (With Auth): IP:Port:Username:Password\r\n" +
                    "• Format 3: Username:Password@IP:Port\r\n\r\n" +
                    "Examples:\r\n" +
                    "103.77.243.55:10153\r\n" +
                    "103.77.243.55:10154:user123:pass456"
                ),
                _ => (
                    "Định dạng: IP:Port hoặc IP:Port:User:Pass\r\nVí dụ: 103.77.243.55:10153:admin:secret",
                    "Hướng dẫn định dạng file Proxy (proxy.txt)",
                    "【HƯỚNG DẪN ĐỊNH DẠNG FILE PROXY (proxy.txt)】\r\n\r\n" +
                    "Mỗi dòng đại diện cho một máy chủ Proxy (HTTP hoặc SOCKS5):\r\n" +
                    "• Định dạng 1 (Không user/pass): IP:Port\r\n" +
                    "• Định dạng 2 (Có user/pass): IP:Port:Username:Password\r\n" +
                    "• Định dạng 3: Username:Password@IP:Port\r\n\r\n" +
                    "Ví dụ:\r\n" +
                    "103.77.243.55:10153\r\n" +
                    "103.77.243.55:10154:proxyuser:proxypass123"
                )
            },
            "target" => lang switch
            {
                AppLanguage.Korean => (
                    "형식: 키워드 | 영상 제목 | 채널명 | YouTube URL\r\n예: 코인 사기 | 라이브 사기 | 사기 채널 | https://youtube.com/live/xxx",
                    "목표 목록 파일 (.txt / .csv) 형식 안내",
                    "【목표 목록 파일 (.txt / .csv) 형식 안내】\r\n\r\n" +
                    "각 줄마다 하나의 신고 대상 목표를 입력합니다:\r\n" +
                    "• 전체 형식 (| 기호 구분):\r\n" +
                    "  키워드 | 영상 제목 | 채널명 | YouTube URL\r\n\r\n" +
                    "• 간편 형식:\r\n" +
                    "  - URL만 입력: https://youtube.com/watch?v=... 또는 https://youtube.com/live/...\r\n" +
                    "  - 검색 키워드만 입력: 가상화폐 사기 라이브\r\n\r\n" +
                    "예시:\r\n" +
                    "가상화폐 사기 | 코인 무료 증정 | Crypto Official | https://youtube.com/live/abc123\r\n" +
                    "https://youtube.com/watch?v=dQw4w9WgXcQ"
                ),
                AppLanguage.English => (
                    "Format: Keyword | Video Title | Channel | YouTube URL\r\nEx: crypto scam | Giveaway Live | Scam Channel | https://youtube.com/live/xxx",
                    "Target List File (.txt / .csv) Format Guide",
                    "【Target List File (.txt / .csv) Format Guide】\r\n\r\n" +
                    "Each line represents one report target:\r\n" +
                    "• Full Format (separated by |):\r\n" +
                    "  Keyword | Video Title | Channel Name | YouTube URL\r\n\r\n" +
                    "• Simple Formats:\r\n" +
                    "  - Direct URL: https://youtube.com/watch?v=... or https://youtube.com/live/...\r\n" +
                    "  - Search Keyword only: crypto giveaway scam\r\n\r\n" +
                    "Examples:\r\n" +
                    "crypto scam | Fake BTC Event | Crypto Channel | https://youtube.com/live/abc123\r\n" +
                    "https://youtube.com/watch?v=dQw4w9WgXcQ"
                ),
                _ => (
                    "Định dạng: Từ khóa | Tiêu đề video | Tên kênh | URL YouTube\r\nVí dụ: tiền ảo lừa đảo | Tặng coin | Kênh lừa đảo | https://youtube.com/live/xxx",
                    "Hướng dẫn định dạng file danh sách mục tiêu (.txt / .csv)",
                    "【HƯỚNG DẪN ĐỊNH DẠNG FILE MỤC TIÊU (.txt / .csv)】\r\n\r\n" +
                    "Mỗi dòng đại diện cho một mục tiêu báo cáo:\r\n" +
                    "• Định dạng đầy đủ (phân tách bởi dấu |):\r\n" +
                    "  Từ khóa | Tiêu đề video | Tên kênh | URL YouTube\r\n\r\n" +
                    "• Định dạng rút gọn:\r\n" +
                    "  - Chỉ điền link URL: https://youtube.com/watch?v=... hoặc https://youtube.com/live/...\r\n" +
                    "  - Chỉ điền Từ khóa tìm kiếm: lừa đảo tiền ảo\r\n\r\n" +
                    "Ví dụ:\r\n" +
                    "tiền ảo lừa đảo | Livestream tặng BTC | Crypto News | https://youtube.com/live/abc123\r\n" +
                    "https://youtube.com/watch?v=dQw4w9WgXcQ"
                )
            },
            "reason" => lang switch
            {
                AppLanguage.Korean => (
                    "형식: 줄마다 신고 상세 사유 작성 또는 [카테고리] 그룹화\r\n예: [Financial]\r\n불법 가상화폐 입금을 유도하는 사기 영상입니다.",
                    "신고 사유 내용 파일 (report_reasons.txt) 형식 안내",
                    "【신고 사유 내용 파일 (report_reasons.txt) 형식 안내】\r\n\r\n" +
                    "신고 시 상세 설명란에 자동으로 입력할 문장을 설정합니다:\r\n" +
                    "1. 카테고리별 그룹화 형식 (권장):\r\n" +
                    "   [Financial]\r\n" +
                    "   이 영상은 시청자에게 암호화폐 입금을 유도하는 사기성 라이브입니다.\r\n" +
                    "   공식 채널을 사칭하여 금전적 피해를 입히고 있습니다.\r\n\r\n" +
                    "   [Spam]\r\n" +
                    "   반복적인 스팸 링크 및 오해를 유발하는 사기성 홍보입니다.\r\n\r\n" +
                    "2. 일반 형식:\r\n" +
                    "   줄마다 사유를 한 문장씩 작성하면 시스템이 랜덤 순환 입력합니다.\r\n" +
                    "   (# 또는 // 로 시작하는 줄은 주석 처리되어 제외됩니다)."
                ),
                AppLanguage.English => (
                    "Format: One reason per line or grouped by [Category]\r\nEx: [Financial]\r\nThis video is promoting crypto fraud soliciting funds.",
                    "Report Reasons File (report_reasons.txt) Format Guide",
                    "【Report Reasons File (report_reasons.txt) Format Guide】\r\n\r\n" +
                    "Defines text descriptions auto-filled when submitting reports:\r\n" +
                    "1. Category Grouped Format (Recommended):\r\n" +
                    "   [Financial]\r\n" +
                    "   This stream is impersonating an official entity and soliciting cryptocurrency.\r\n" +
                    "   Fraudulent giveaway scheme causing severe financial loss.\r\n\r\n" +
                    "   [Spam]\r\n" +
                    "   Mass deceptive spam links repeated continuously.\r\n\r\n" +
                    "2. Simple Format:\r\n" +
                    "   One reason per line, randomly rotated by the system.\r\n" +
                    "   (Lines starting with # or // are ignored as comments)."
                ),
                _ => (
                    "Định dạng: Mỗi dòng một câu lý do hoặc phân nhóm [Category]\r\nVí dụ: [Financial]\r\nVideo livestream phát tán hình ảnh giả mạo dụ dỗ nạp tiền ảo.",
                    "Hướng dẫn định dạng file nội dung báo cáo (report_reasons.txt)",
                    "【HƯỚNG DẪN ĐỊNH DẠNG FILE NỘI DUNG BÁO CÁO (report_reasons.txt)】\r\n\r\n" +
                    "Dùng để tự động điền phần giải trình chi tiết khi gửi báo cáo lên YouTube:\r\n" +
                    "1. Định dạng phân theo nhóm danh mục (Khuyên dùng):\r\n" +
                    "   [Financial]\r\n" +
                    "   Video livestream giả mạo dự án kêu gọi chuyển coin lừa đảo.\r\n" +
                    "   Nội dung hứa hẹn nhân đôi tiền ảo có dấu hiệu lừa đảo tài chính chiếm đoạt tài sản.\r\n\r\n" +
                    "   [Spam]\r\n" +
                    "   Nội dung spam lặp đi lặp lại nhiều lần gây hiểu lầm cho người xem.\r\n\r\n" +
                    "2. Định dạng thông thường:\r\n" +
                    "   Mỗi dòng là một nội dung giải trình, hệ thống sẽ tự động xoay vòng ngẫu nhiên.\r\n" +
                    "   (Các dòng bắt đầu bằng dấu # hoặc // sẽ được bỏ qua như ghi chú)."
                )
            },
            _ => (string.Empty, string.Empty, string.Empty)
        };
    }
}
