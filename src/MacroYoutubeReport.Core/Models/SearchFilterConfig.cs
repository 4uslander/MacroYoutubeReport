namespace MacroYoutubeReport.Core.Models;

/// <summary>
/// Bộ lọc phân loại loại hình nội dung tìm kiếm (Phân loại).
/// </summary>
public enum SearchTypeFilter
{
    All = 0,         // Không dùng / Tất cả (전체)
    Video = 1,       // Video (동영상)
    Channel = 2,     // Kênh (채널)
    Playlist = 3,    // Danh sách phát (재생목록)
    Movie = 4        // Phim (영화)
}

/// <summary>
/// Bộ lọc độ dài thời lượng video (Độ dài).
/// </summary>
public enum SearchDurationFilter
{
    All = 0,         // Không dùng / Tất cả (모두)
    Short = 1,       // Dưới 4 phút (4분 미만)
    Medium = 2,      // 4 - 20 phút (4~20분)
    Long = 3         // Trên 20 phút (20분 초과)
}

/// <summary>
/// Bộ lọc thời điểm tải lên / phát sóng (Ngày tải lên).
/// </summary>
public enum SearchUploadDateFilter
{
    All = 0,         // Không dùng / Toàn thời gian (전체 기간)
    Today = 1,       // Hôm nay (오늘) - Khuyến nghị cho Livestream!
    LastHour = 2,    // Giờ qua (지난 1시간)
    ThisWeek = 3,    // Tuần này (이번 주)
    ThisMonth = 4,   // Tháng này (이번 달)
    ThisYear = 5     // Năm nay (올해)
}

/// <summary>
/// Bộ lọc sắp xếp ưu tiên kết quả tìm kiếm (Ưu tiên).
/// </summary>
public enum SearchSortByFilter
{
    Relevance = 0,   // Mức độ liên quan / Không dùng (관련성)
    UploadDate = 1,  // Ngày tải lên (업로드 날짜)
    ViewCount = 2,   // Số lượt xem (조회수)
    Rating = 3       // Xếp hạng (평점)
}

/// <summary>
/// Cấu hình bộ lọc tìm kiếm video chuẩn theo phong cách AutoYouTube.
/// Tự động thiết lập các tiêu chí lọc trên ứng dụng YouTube Android để tìm đúng video mục tiêu trong thời gian ngắn nhất.
/// </summary>
public sealed class SearchFilterConfig
{
    public SearchTypeFilter Type { get; set; } = SearchTypeFilter.All;
    public SearchDurationFilter Duration { get; set; } = SearchDurationFilter.All;
    public SearchUploadDateFilter UploadDate { get; set; } = SearchUploadDateFilter.Today;
    public SearchSortByFilter SortBy { get; set; } = SearchSortByFilter.Relevance;

    // Checkbox tính năng lọc theo đặc tính
    public bool LiveOnly { get; set; } = true;
    public bool CommonsOnly { get; set; } = false;
    public bool SubtitlesOnly { get; set; } = false;
    public bool FourKOnly { get; set; } = false;

    // Tùy chọn mở rộng vận hành
    public bool RandomWatch { get; set; } = false;
    public bool DirectUrlMode { get; set; } = false;
    public bool AutoAllocateEmail { get; set; } = true;
    public bool ReSearchIfNotFound { get; set; } = true;
    public bool CheckLaixiKeyboard { get; set; } = true;

    /// <summary>
    /// Số lần cuộn màn hình tối đa để tìm video mục tiêu (mặc định 50 lần, liên tục đến khi thấy).
    /// </summary>
    public int MaxScrollAttempts { get; set; } = 50;

    /// <summary>
    /// Kiểm tra xem có bất kỳ tiêu chí lọc nâng cao nào được kích hoạt hay không.
    /// </summary>
    public bool HasActiveFilter()
    {
        return LiveOnly ||
               UploadDate != SearchUploadDateFilter.All ||
               Type != SearchTypeFilter.All ||
               Duration != SearchDurationFilter.All ||
               SortBy != SearchSortByFilter.Relevance ||
               CommonsOnly ||
               SubtitlesOnly ||
               FourKOnly;
    }
}
