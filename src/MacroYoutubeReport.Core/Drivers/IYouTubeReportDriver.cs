using MacroYoutubeReport.Core.Enums;
using MacroYoutubeReport.Core.Models;

namespace MacroYoutubeReport.Core.Drivers;

/// <summary>
/// Giao diện điều khiển tự động hóa YouTube chuyên sâu cho tác vụ Báo cáo (Report),
/// hỗ trợ đồng nhất cả 2 nền tảng: Android (máy thật/giả lập qua ADB) và Web Chrome (Playwright).
/// </summary>
public interface IYouTubeReportDriver : IAsyncDisposable
{
    /// <summary>
    /// Nền tảng điều khiển (Android hoặc Chrome).
    /// </summary>
    PlatformType Platform { get; }

    /// <summary>
    /// Định danh mục tiêu điều khiển (Serial thiết bị Android hoặc Tên Profile Chrome).
    /// </summary>
    string TargetId { get; }

    /// <summary>
    /// Khởi tạo phiên kết nối (ADB Client hoặc Browser Context).
    /// </summary>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>
    /// Đảm bảo ứng dụng YouTube đang chạy ở foreground, sạch sẽ và sẵn sàng tương tác.
    /// Tự động đóng popup (Premium, Update, Consent, Notifications) và giải phóng bẫy Shorts.
    /// </summary>
    Task EnsureAppReadyAsync(CancellationToken ct = default);

    /// <summary>
    /// Tự động phát hiện và xử lý/bỏ qua quảng cáo (Ad Skip) nếu đang xuất hiện trước video.
    /// </summary>
    Task EnsureNoAdPlayingAsync(CancellationToken ct = default);

    /// <summary>
    /// Điều hướng về trang chủ YouTube.
    /// </summary>
    Task NavigateHomeAsync(CancellationToken ct = default);

    /// <summary>
    /// Thực hiện tìm kiếm từ khóa với tốc độ gõ phím mô phỏng người thật.
    /// </summary>
    Task SearchAsync(string keyword, CancellationToken ct = default);

    /// <summary>
    /// Cuộn thông minh tìm kiếm video theo tiêu chí (Tiêu đề, Kênh, URL) và click mở khi phát hiện.
    /// Trả về true nếu tìm thấy và đã click mở thành công; false nếu hết số trang cuộn tối đa.
    /// </summary>
    Task<bool> SmartScrollAndSelectVideoAsync(ReportTargetItem target, CancellationToken ct = default);

    /// <summary>
    /// Mở trực tiếp video thông qua đường dẫn URL.
    /// </summary>
    Task OpenVideoByUrlAsync(string url, CancellationToken ct = default);

    /// <summary>
    /// Mở trang thông tin giới thiệu của Kênh (About tab) để chuẩn bị cho kịch bản báo cáo người dùng.
    /// </summary>
    Task OpenChannelPageAsync(string channelUrlOrName, CancellationToken ct = default);

    /// <summary>
    /// Xem video trong khoảng thời gian chỉ định kết hợp các vi hành vi tự nhiên (cuộn đọc bình luận, tạm dừng nhẹ, v.v.).
    /// </summary>
    Task WatchVideoWithNaturalEngagementAsync(TimeSpan duration, NaturalEngagementOptions options, CancellationToken ct = default);

    /// <summary>
    /// Kích hoạt menu chứa mục Báo cáo trên giao diện (nút Settings/3-dots trên player hoặc trang video).
    /// </summary>
    Task<bool> OpenReportMenuAsync(CancellationToken ct = default);

    /// <summary>
    /// Thực hiện toàn bộ quy trình chọn danh mục vi phạm, điền mô tả bổ sung và bấm nút gửi Báo cáo video.
    /// </summary>
    Task<bool> SubmitVideoReportAsync(ReportReasonConfig reasonConfig, CancellationToken ct = default);

    /// <summary>
    /// Thực hiện quy trình gửi Báo cáo toàn bộ kênh người dùng (Report user/channel).
    /// </summary>
    Task<bool> SubmitChannelReportAsync(ReportReasonConfig reasonConfig, CancellationToken ct = default);

    /// <summary>
    /// Thực hiện quy trình gửi Báo cáo video ngắn Shorts.
    /// </summary>
    Task<bool> SubmitShortsReportAsync(ReportReasonConfig reasonConfig, CancellationToken ct = default);

    /// <summary>
    /// Thực hiện quy trình gửi Báo cáo luồng phát trực tiếp (Live Stream), bao gồm xử lý đóng Live Chat overlay che khuất thanh công cụ.
    /// </summary>
    Task<bool> SubmitLiveStreamReportAsync(ReportReasonConfig reasonConfig, CancellationToken ct = default);

    /// <summary>
    /// Thực hiện quy trình tìm và gửi Báo cáo một bình luận vi phạm cụ thể.
    /// </summary>
    Task<bool> SubmitCommentReportAsync(string commentAuthorOrKeyword, ReportReasonConfig reasonConfig, CancellationToken ct = default);

    /// <summary>
    /// Kiểm tra xác thực thông báo báo cáo thành công xuất hiện trên màn hình (Toast / Snack-bar / Dialog confirmation).
    /// </summary>
    Task<bool> VerifyReportSubmittedAsync(CancellationToken ct = default);

    /// <summary>
    /// Chụp ảnh màn hình lưu lại bằng chứng thực thi.
    /// </summary>
    Task<string> CaptureScreenshotAsync(string stepPrefix, CancellationToken ct = default);
}
