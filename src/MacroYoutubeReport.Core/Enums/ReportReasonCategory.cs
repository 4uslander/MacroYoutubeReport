namespace MacroYoutubeReport.Core.Enums;

/// <summary>
/// Các danh mục lý do báo cáo vi phạm chính trên YouTube (Video & Channel).
/// Tương thích toàn diện với giao diện YouTube trên Android App và Web Chrome.
/// </summary>
public enum ReportReasonCategory
{
    /// <summary>
    /// Spam hoặc gây hiểu lầm (Spam or misleading)
    /// </summary>
    SpamOrMisleading = 1,

    /// <summary>
    /// Nội dung khiêu dâm (Sexual content)
    /// </summary>
    SexualContent = 2,

    /// <summary>
    /// Nội dung bạo lực hoặc phản cảm (Violent or repulsive content)
    /// </summary>
    ViolentOrRepulsive = 3,

    /// <summary>
    /// Kích động thù địch hoặc lăng mạ (Hateful or abusive content)
    /// </summary>
    HatefulOrAbusive = 4,

    /// <summary>
    /// Quấy rối hoặc bắt nạt (Harassment or bullying)
    /// </summary>
    HarassmentOrBullying = 5,

    /// <summary>
    /// Hành vi có hại hoặc nguy hiểm (Harmful or dangerous acts)
    /// </summary>
    HarmfulDangerousActs = 6,

    /// <summary>
    /// Thông tin sai lệch (Misinformation)
    /// </summary>
    Misinformation = 7,

    /// <summary>
    /// Ngược đãi trẻ em (Child abuse)
    /// </summary>
    ChildAbuse = 8,

    /// <summary>
    /// Cổ xúy chủ nghĩa khủng bố (Promotes terrorism)
    /// </summary>
    PromotesTerrorism = 9,

    /// <summary>
    /// Vi phạm quyền của tôi / Bản quyền (Infringes my rights)
    /// </summary>
    InfringesMyRights = 10,

    /// <summary>
    /// Vấn đề về phụ đề (Captions issue)
    /// </summary>
    CaptionsIssue = 11
}
