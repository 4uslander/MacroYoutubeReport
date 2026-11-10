using MacroYoutubeReport.Core.Enums;
using MacroYoutubeReport.Core.Models;

namespace MacroYoutubeReport.Core.Workflows;

/// <summary>
/// Định nghĩa các kịch bản thực thi báo cáo YouTube chuẩn mực và toàn diện.
/// Toàn bộ các bước mô phỏng người dùng thật 100% từ khởi động, tìm kiếm, xem tự nhiên, xử lý quảng cáo đến nhấn báo cáo.
/// </summary>
public static class ScenarioDefinitions
{
    public const string ScenarioSearchAndReport = "1. Tìm kiếm từ khóa, xem tự nhiên và báo cáo video";
    public const string ScenarioDirectUrlAndReport = "2. Mở trực tiếp link URL, xem và báo cáo video";
    public const string ScenarioChannelReport = "3. Tìm kiếm và báo cáo toàn bộ kênh (User Channel)";
    public const string ScenarioShortsReport = "4. Mở video ngắn Shorts, xem tự nhiên và báo cáo";
    public const string ScenarioCommentReport = "5. Tìm và báo cáo bình luận vi phạm trong video";
    public const string ScenarioLiveStreamReport = "6. Báo cáo Livestream lừa đảo tài chính / crypto";

    /// <summary>
    /// Kịch bản 1: Mở app YouTube -> Gõ tìm kiếm -> Cuộn thông minh -> Xử lý quảng cáo -> Xem tự nhiên -> Nhấn Report -> Điền lý do -> Xác nhận.
    /// </summary>
    public static IReadOnlyList<IWorkflowStep> CreateSearchAndReportVideoScenario(
        ReportTargetItem target,
        ReportScenarioConfig config)
    {
        ArgumentNullException.ThrowIfNull(target);
        config ??= ReportScenarioConfig.CreateStandardSearchConfig();

        return new IWorkflowStep[]
        {
            new DelegateWorkflowStep("1. Khởi động app & chuẩn bị môi trường", async (driver, ctx, ct) =>
            {
                ctx.CurrentTarget = target;
                ctx.CurrentScenario = config;
                ctx.CurrentReason = config.GetNextReason();

                await driver.InitializeAsync(ct).ConfigureAwait(false);
                await driver.EnsureAppReadyAsync(ct).ConfigureAwait(false);
                await driver.NavigateHomeAsync(ct).ConfigureAwait(false);

                string shot = await driver.CaptureScreenshotAsync("01_HomeReady", ct).ConfigureAwait(false);
                return ReportStepResult.Success("Khởi động app", "Trang chủ YouTube đã mở và sẵn sàng.", shot);
            }, TimeSpan.FromSeconds(60)),

            new DelegateWorkflowStep("2. Gõ tìm kiếm từ khóa", async (driver, ctx, ct) =>
            {
                string searchKeyword = !string.IsNullOrWhiteSpace(target.Keyword)
                    ? target.Keyword
                    : target.VideoTitle ?? target.ChannelName ?? "YouTube";

                await driver.SearchAsync(searchKeyword, ct).ConfigureAwait(false);
                string shot = await driver.CaptureScreenshotAsync("02_SearchCompleted", ct).ConfigureAwait(false);
                return ReportStepResult.Success("Tìm kiếm từ khóa", $"Đã gõ tìm kiếm từ khóa: '{searchKeyword}'.", shot);
            }, TimeSpan.FromSeconds(60)),

            new DelegateWorkflowStep("3. Cuộn & chọn đúng video mục tiêu", async (driver, ctx, ct) =>
            {
                var filter = ctx.CurrentScenario?.SearchFilter ?? config.SearchFilter;
                if (filter != null && filter.HasActiveFilter())
                {
                    await driver.ApplySearchFilterAsync(filter, ct).ConfigureAwait(false);
                }

                bool found = await driver.SmartScrollAndSelectVideoAsync(target, ct).ConfigureAwait(false);
                if (!found && !string.IsNullOrWhiteSpace(target.TargetUrl))
                {
                    await driver.OpenVideoByUrlAsync(target.TargetUrl, ct).ConfigureAwait(false);
                    found = true;
                }

                if (!found)
                {
                    return ReportStepResult.Failure("Chọn video mục tiêu",
                        $"Không tìm thấy video phù hợp với tiêu chí: Tiêu đề='{target.VideoTitle}', Kênh='{target.ChannelName}'.");
                }

                await driver.EnsureNoAdPlayingAsync(ct).ConfigureAwait(false);
                string shot = await driver.CaptureScreenshotAsync("03_VideoOpened", ct).ConfigureAwait(false);
                return ReportStepResult.Success("Chọn video mục tiêu", "Đã tìm thấy và mở video mục tiêu thành công.", shot);
            }, TimeSpan.FromSeconds(180)),

            new DelegateWorkflowStep("4. Xem video tự nhiên", async (driver, ctx, ct) =>
            {
                var rnd = Random.Shared;
                int durationSeconds = rnd.Next(config.MinWatchDurationSeconds, Math.Max(config.MinWatchDurationSeconds + 1, config.MaxWatchDurationSeconds));
                var duration = TimeSpan.FromSeconds(durationSeconds);

                await driver.WatchVideoWithNaturalEngagementAsync(duration, config.EngagementOptions, ct).ConfigureAwait(false);
                string shot = await driver.CaptureScreenshotAsync("04_WatchedNatural", ct).ConfigureAwait(false);
                return ReportStepResult.Success("Xem video tự nhiên", $"Đã xem tự nhiên {durationSeconds}s kèm tương tác mô phỏng người thật.", shot);
            }, TimeSpan.FromSeconds(config.MaxWatchDurationSeconds + 60)),

            new DelegateWorkflowStep("5. Mở menu Báo cáo", async (driver, ctx, ct) =>
            {
                bool opened = await driver.OpenReportMenuAsync(ct).ConfigureAwait(false);
                if (!opened)
                {
                    return ReportStepResult.Failure("Mở menu Báo cáo", "Không thể định vị hoặc mở menu Báo cáo (Report) trên giao diện.");
                }

                string shot = await driver.CaptureScreenshotAsync("05_ReportMenuOpened", ct).ConfigureAwait(false);
                return ReportStepResult.Success("Mở menu Báo cáo", "Đã mở thành công bảng chọn lý do báo cáo.", shot);
            }, TimeSpan.FromSeconds(45)),

            new DelegateWorkflowStep("6. Chọn lý do & Gửi báo cáo", async (driver, ctx, ct) =>
            {
                var reason = ctx.CurrentReason ?? config.GetNextReason();
                bool submitted = await driver.SubmitVideoReportAsync(reason, ct).ConfigureAwait(false);
                if (!submitted)
                {
                    return ReportStepResult.Failure("Gửi báo cáo", $"Không thể gửi báo cáo cho danh mục: {reason.Category}.");
                }

                string shot = await driver.CaptureScreenshotAsync("06_ReportSubmitted", ct).ConfigureAwait(false);
                return ReportStepResult.Success("Gửi báo cáo", $"Đã chọn danh mục '{reason.Category}' và gửi báo cáo thành công.", shot);
            }, TimeSpan.FromSeconds(60)),

            new DelegateWorkflowStep("7. Xác thực kết quả & Lưu bằng chứng", async (driver, ctx, ct) =>
            {
                bool verified = await driver.VerifyReportSubmittedAsync(ct).ConfigureAwait(false);
                string proofShot = await driver.CaptureScreenshotAsync("07_ReportProof", ct).ConfigureAwait(false);

                target.ReportsCompleted++;
                target.LastReportedAt = DateTime.UtcNow;

                string note = verified
                    ? "Báo cáo đã được YouTube tiếp nhận (Hiển thị thông báo cảm ơn)."
                    : "Đã hoàn tất gửi báo cáo.";
                return ReportStepResult.Success("Xác thực kết quả", note, proofShot);
            }, TimeSpan.FromSeconds(30))
        };
    }

    /// <summary>
    /// Kịch bản 2: Mở trực tiếp theo URL -> Xử lý quảng cáo -> Xem tự nhiên -> Nhấn Report -> Điền lý do -> Xác nhận.
    /// </summary>
    public static IReadOnlyList<IWorkflowStep> CreateDirectUrlReportVideoScenario(
        ReportTargetItem target,
        ReportScenarioConfig config)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (string.IsNullOrWhiteSpace(target.TargetUrl))
        {
            throw new ArgumentException("Mục tiêu phải có TargetUrl để thực thi kịch bản mở link trực tiếp.", nameof(target));
        }

        config ??= ReportScenarioConfig.CreateDirectUrlConfig();

        return new IWorkflowStep[]
        {
            new DelegateWorkflowStep("1. Khởi động app & mở link video", async (driver, ctx, ct) =>
            {
                ctx.CurrentTarget = target;
                ctx.CurrentScenario = config;
                ctx.CurrentReason = config.GetNextReason();

                await driver.InitializeAsync(ct).ConfigureAwait(false);
                await driver.EnsureAppReadyAsync(ct).ConfigureAwait(false);
                await driver.OpenVideoByUrlAsync(target.TargetUrl, ct).ConfigureAwait(false);
                await driver.EnsureNoAdPlayingAsync(ct).ConfigureAwait(false);

                string shot = await driver.CaptureScreenshotAsync("01_UrlVideoOpened", ct).ConfigureAwait(false);
                return ReportStepResult.Success("Mở link video", $"Đã mở trực tiếp link: {target.TargetUrl}", shot);
            }, TimeSpan.FromSeconds(60)),

            new DelegateWorkflowStep("2. Xem video tự nhiên", async (driver, ctx, ct) =>
            {
                var rnd = Random.Shared;
                int durationSeconds = rnd.Next(config.MinWatchDurationSeconds, Math.Max(config.MinWatchDurationSeconds + 1, config.MaxWatchDurationSeconds));

                await driver.WatchVideoWithNaturalEngagementAsync(TimeSpan.FromSeconds(durationSeconds), config.EngagementOptions, ct).ConfigureAwait(false);
                string shot = await driver.CaptureScreenshotAsync("02_WatchedNatural", ct).ConfigureAwait(false);
                return ReportStepResult.Success("Xem video tự nhiên", $"Đã xem video {durationSeconds}s.", shot);
            }, TimeSpan.FromSeconds(config.MaxWatchDurationSeconds + 60)),

            new DelegateWorkflowStep("3. Mở menu Báo cáo", async (driver, ctx, ct) =>
            {
                bool opened = await driver.OpenReportMenuAsync(ct).ConfigureAwait(false);
                if (!opened)
                {
                    return ReportStepResult.Failure("Mở menu Báo cáo", "Không thể mở menu Báo cáo.");
                }
                string shot = await driver.CaptureScreenshotAsync("03_ReportMenuOpened", ct).ConfigureAwait(false);
                return ReportStepResult.Success("Mở menu Báo cáo", "Đã mở menu Báo cáo.", shot);
            }, TimeSpan.FromSeconds(45)),

            new DelegateWorkflowStep("4. Chọn lý do & Gửi báo cáo", async (driver, ctx, ct) =>
            {
                var reason = ctx.CurrentReason ?? config.GetNextReason();
                bool submitted = await driver.SubmitVideoReportAsync(reason, ct).ConfigureAwait(false);
                if (!submitted)
                {
                    return ReportStepResult.Failure("Gửi báo cáo", $"Không thể gửi báo cáo cho danh mục: {reason.Category}.");
                }
                string shot = await driver.CaptureScreenshotAsync("04_ReportSubmitted", ct).ConfigureAwait(false);
                return ReportStepResult.Success("Gửi báo cáo", $"Đã gửi báo cáo: {reason.Category}", shot);
            }, TimeSpan.FromSeconds(60)),

            new DelegateWorkflowStep("5. Xác thực kết quả & Lưu bằng chứng", async (driver, ctx, ct) =>
            {
                await driver.VerifyReportSubmittedAsync(ct).ConfigureAwait(false);
                string proofShot = await driver.CaptureScreenshotAsync("05_ReportProof", ct).ConfigureAwait(false);
                target.ReportsCompleted++;
                target.LastReportedAt = DateTime.UtcNow;
                return ReportStepResult.Success("Xác thực kết quả", "Báo cáo hoàn tất.", proofShot);
            }, TimeSpan.FromSeconds(30))
        };
    }

    /// <summary>
    /// Kịch bản 3: Mở trang giới thiệu Kênh (About tab) -> Báo cáo người dùng.
    /// </summary>
    public static IReadOnlyList<IWorkflowStep> CreateChannelReportScenario(
        ReportTargetItem target,
        ReportScenarioConfig config)
    {
        ArgumentNullException.ThrowIfNull(target);
        config ??= ReportScenarioConfig.CreateChannelReportConfig();

        return new IWorkflowStep[]
        {
            new DelegateWorkflowStep("1. Mở trang Kênh mục tiêu", async (driver, ctx, ct) =>
            {
                ctx.CurrentTarget = target;
                ctx.CurrentScenario = config;
                ctx.CurrentReason = config.GetNextReason();

                await driver.InitializeAsync(ct).ConfigureAwait(false);
                await driver.EnsureAppReadyAsync(ct).ConfigureAwait(false);

                string channelTarget = !string.IsNullOrWhiteSpace(target.TargetUrl)
                    ? target.TargetUrl
                    : target.ChannelName ?? target.Keyword;

                await driver.OpenChannelPageAsync(channelTarget, ct).ConfigureAwait(false);
                string shot = await driver.CaptureScreenshotAsync("01_ChannelPageOpened", ct).ConfigureAwait(false);
                return ReportStepResult.Success("Mở trang Kênh", $"Đã mở trang kênh: '{channelTarget}'.", shot);
            }, TimeSpan.FromSeconds(60)),

            new DelegateWorkflowStep("2. Gửi báo cáo Kênh", async (driver, ctx, ct) =>
            {
                var reason = ctx.CurrentReason ?? config.GetNextReason();
                bool submitted = await driver.SubmitChannelReportAsync(reason, ct).ConfigureAwait(false);
                if (!submitted)
                {
                    return ReportStepResult.Failure("Báo cáo kênh", "Không thể gửi báo cáo kênh.");
                }

                string shot = await driver.CaptureScreenshotAsync("02_ChannelReportSubmitted", ct).ConfigureAwait(false);
                return ReportStepResult.Success("Báo cáo kênh", $"Đã gửi báo cáo kênh với lý do: {reason.Category}", shot);
            }, TimeSpan.FromSeconds(60)),

            new DelegateWorkflowStep("3. Xác thực kết quả", async (driver, ctx, ct) =>
            {
                await driver.VerifyReportSubmittedAsync(ct).ConfigureAwait(false);
                string proofShot = await driver.CaptureScreenshotAsync("03_ChannelReportProof", ct).ConfigureAwait(false);
                target.ReportsCompleted++;
                target.LastReportedAt = DateTime.UtcNow;
                return ReportStepResult.Success("Xác thực kết quả", "Báo cáo kênh thành công.", proofShot);
            }, TimeSpan.FromSeconds(30))
        };
    }

    /// <summary>
    /// Kịch bản 4: Mở video ngắn Shorts -> Xem tự nhiên -> Báo cáo Shorts.
    /// </summary>
    public static IReadOnlyList<IWorkflowStep> CreateShortsReportScenario(
        ReportTargetItem target,
        ReportScenarioConfig config)
    {
        ArgumentNullException.ThrowIfNull(target);
        config ??= new ReportScenarioConfig
        {
            ScenarioName = "4. Báo cáo video ngắn Shorts",
            MinWatchDurationSeconds = 15,
            MaxWatchDurationSeconds = 45
        };

        return new IWorkflowStep[]
        {
            new DelegateWorkflowStep("1. Mở video Shorts", async (driver, ctx, ct) =>
            {
                ctx.CurrentTarget = target;
                ctx.CurrentScenario = config;
                ctx.CurrentReason = config.GetNextReason();

                await driver.InitializeAsync(ct).ConfigureAwait(false);
                await driver.EnsureAppReadyAsync(ct).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(target.TargetUrl))
                {
                    await driver.OpenVideoByUrlAsync(target.TargetUrl, ct).ConfigureAwait(false);
                }
                else
                {
                    await driver.SearchAsync(target.Keyword, ct).ConfigureAwait(false);
                    await driver.SmartScrollAndSelectVideoAsync(target, ct).ConfigureAwait(false);
                }

                string shot = await driver.CaptureScreenshotAsync("01_ShortsOpened", ct).ConfigureAwait(false);
                return ReportStepResult.Success("Mở Shorts", "Video Shorts đã mở.", shot);
            }, TimeSpan.FromSeconds(60)),

            new DelegateWorkflowStep("2. Xem Shorts tự nhiên", async (driver, ctx, ct) =>
            {
                int duration = Random.Shared.Next(config.MinWatchDurationSeconds, config.MaxWatchDurationSeconds);
                await Task.Delay(TimeSpan.FromSeconds(duration), ct).ConfigureAwait(false);
                string shot = await driver.CaptureScreenshotAsync("02_ShortsWatched", ct).ConfigureAwait(false);
                return ReportStepResult.Success("Xem Shorts", $"Đã xem Shorts {duration}s.", shot);
            }, TimeSpan.FromSeconds(config.MaxWatchDurationSeconds + 30)),

            new DelegateWorkflowStep("3. Gửi báo cáo Shorts", async (driver, ctx, ct) =>
            {
                var reason = ctx.CurrentReason ?? config.GetNextReason();
                bool submitted = await driver.SubmitShortsReportAsync(reason, ct).ConfigureAwait(false);
                if (!submitted)
                {
                    return ReportStepResult.Failure("Báo cáo Shorts", "Không thể mở menu hoặc gửi báo cáo Shorts.");
                }

                string shot = await driver.CaptureScreenshotAsync("03_ShortsReportSubmitted", ct).ConfigureAwait(false);
                return ReportStepResult.Success("Báo cáo Shorts", $"Đã gửi báo cáo Shorts: {reason.Category}", shot);
            }, TimeSpan.FromSeconds(60)),

            new DelegateWorkflowStep("4. Xác thực kết quả", async (driver, ctx, ct) =>
            {
                await driver.VerifyReportSubmittedAsync(ct).ConfigureAwait(false);
                string proofShot = await driver.CaptureScreenshotAsync("04_ShortsReportProof", ct).ConfigureAwait(false);
                target.ReportsCompleted++;
                target.LastReportedAt = DateTime.UtcNow;
                return ReportStepResult.Success("Xác thực kết quả", "Báo cáo Shorts hoàn tất.", proofShot);
            }, TimeSpan.FromSeconds(30))
        };
    }

    /// <summary>
    /// Kịch bản 5: Tìm và báo cáo một bình luận vi phạm cụ thể trong video.
    /// </summary>
    public static IReadOnlyList<IWorkflowStep> CreateCommentReportScenario(
        ReportTargetItem target,
        string commentAuthorOrKeyword,
        ReportScenarioConfig config)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(commentAuthorOrKeyword);

        config ??= ReportScenarioConfig.CreateStandardSearchConfig();

        return new IWorkflowStep[]
        {
            new DelegateWorkflowStep("1. Mở video chứa bình luận", async (driver, ctx, ct) =>
            {
                ctx.CurrentTarget = target;
                ctx.CurrentScenario = config;
                ctx.CurrentReason = config.GetNextReason();

                await driver.InitializeAsync(ct).ConfigureAwait(false);
                await driver.EnsureAppReadyAsync(ct).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(target.TargetUrl))
                {
                    await driver.OpenVideoByUrlAsync(target.TargetUrl, ct).ConfigureAwait(false);
                }
                else
                {
                    await driver.SearchAsync(target.Keyword, ct).ConfigureAwait(false);
                    await driver.SmartScrollAndSelectVideoAsync(target, ct).ConfigureAwait(false);
                }

                await driver.EnsureNoAdPlayingAsync(ct).ConfigureAwait(false);
                string shot = await driver.CaptureScreenshotAsync("01_CommentVideoOpened", ct).ConfigureAwait(false);
                return ReportStepResult.Success("Mở video", "Video chứa bình luận đã mở.", shot);
            }, TimeSpan.FromSeconds(60)),

            new DelegateWorkflowStep("2. Tìm và báo cáo bình luận", async (driver, ctx, ct) =>
            {
                var reason = ctx.CurrentReason ?? config.GetNextReason();
                bool submitted = await driver.SubmitCommentReportAsync(commentAuthorOrKeyword, reason, ct).ConfigureAwait(false);
                if (!submitted)
                {
                    return ReportStepResult.Failure("Báo cáo bình luận", $"Không tìm thấy bình luận có tác giả/nội dung chứa '{commentAuthorOrKeyword}'.");
                }

                string shot = await driver.CaptureScreenshotAsync("02_CommentReportSubmitted", ct).ConfigureAwait(false);
                return ReportStepResult.Success("Báo cáo bình luận", $"Đã báo cáo bình luận của '{commentAuthorOrKeyword}' với lý do: {reason.Category}", shot);
            }, TimeSpan.FromSeconds(60)),

            new DelegateWorkflowStep("3. Xác thực kết quả", async (driver, ctx, ct) =>
            {
                await driver.VerifyReportSubmittedAsync(ct).ConfigureAwait(false);
                string proofShot = await driver.CaptureScreenshotAsync("03_CommentReportProof", ct).ConfigureAwait(false);
                target.ReportsCompleted++;
                target.LastReportedAt = DateTime.UtcNow;
                return ReportStepResult.Success("Xác thực kết quả", "Báo cáo bình luận thành công.", proofShot);
            }, TimeSpan.FromSeconds(30))
        };
    }

    /// <summary>
    /// Kịch bản 6: Báo cáo Livestream lừa đảo tài chính và tiền ảo (Crypto/Financial Scam).
    /// Tự động đóng Live Chat overlay, mở menu cài đặt/player controls, chọn danh mục Gian lận/Lừa đảo và nộp báo cáo.
    /// </summary>
    public static IReadOnlyList<IWorkflowStep> CreateLiveStreamFinancialScamScenario(
        ReportTargetItem target,
        ReportScenarioConfig config)
    {
        ArgumentNullException.ThrowIfNull(target);
        config ??= ReportScenarioConfig.CreateLiveStreamFinancialScamConfig();

        return new IWorkflowStep[]
        {
            new DelegateWorkflowStep("1. Khởi động app & chuẩn bị môi trường", async (driver, ctx, ct) =>
            {
                ctx.CurrentTarget = target;
                ctx.CurrentScenario = config;
                ctx.CurrentReason = config.GetNextReason();

                await driver.InitializeAsync(ct).ConfigureAwait(false);
                await driver.EnsureAppReadyAsync(ct).ConfigureAwait(false);
                await driver.NavigateHomeAsync(ct).ConfigureAwait(false);

                string shot = await driver.CaptureScreenshotAsync("01_LiveReady", ct).ConfigureAwait(false);
                return ReportStepResult.Success("Khởi động app", "Trang chủ YouTube đã mở và sẵn sàng.", shot);
            }, TimeSpan.FromSeconds(60)),

            new DelegateWorkflowStep("2. Điều hướng vào Livestream", async (driver, ctx, ct) =>
            {
                if (!string.IsNullOrWhiteSpace(target.TargetUrl))
                {
                    await driver.OpenVideoByUrlAsync(target.TargetUrl, ct).ConfigureAwait(false);
                }
                else
                {
                    string searchKeyword = !string.IsNullOrWhiteSpace(target.Keyword)
                        ? target.Keyword
                        : target.VideoTitle ?? "crypto live stream";

                    await driver.SearchAsync(searchKeyword, ct).ConfigureAwait(false);

                    var filter = ctx.CurrentScenario?.SearchFilter ?? config.SearchFilter;
                    if (filter != null && filter.HasActiveFilter())
                    {
                        await driver.ApplySearchFilterAsync(filter, ct).ConfigureAwait(false);
                    }

                    bool found = await driver.SmartScrollAndSelectVideoAsync(target, ct).ConfigureAwait(false);
                    if (!found)
                    {
                        return ReportStepResult.Failure("Tìm kiếm livestream", $"Không tìm thấy livestream phù hợp với từ khóa '{searchKeyword}'.");
                    }
                }

                await driver.EnsureNoAdPlayingAsync(ct).ConfigureAwait(false);
                string shot = await driver.CaptureScreenshotAsync("02_LiveStreamOpened", ct).ConfigureAwait(false);
                return ReportStepResult.Success("Vào livestream", "Đã mở luồng phát trực tiếp thành công.", shot);
            }, TimeSpan.FromSeconds(180)),

            new DelegateWorkflowStep("3. Xem tự nhiên & tương tác Live Chat", async (driver, ctx, ct) =>
            {
                int duration = Random.Shared.Next(config.MinWatchDurationSeconds, Math.Max(config.MinWatchDurationSeconds + 1, config.MaxWatchDurationSeconds));
                var options = new NaturalEngagementOptions
                {
                    EnableLiveChatScroll = true,
                    EnableCommentReadingScroll = true,
                    MinActions = 1,
                    MaxActions = 2
                };

                await driver.WatchVideoWithNaturalEngagementAsync(TimeSpan.FromSeconds(duration), options, ct).ConfigureAwait(false);
                string shot = await driver.CaptureScreenshotAsync("03_WatchedLive", ct).ConfigureAwait(false);
                return ReportStepResult.Success("Xem tự nhiên", $"Đã xem livestream {duration}s và mô phỏng đọc chat.", shot);
            }, TimeSpan.FromSeconds(120)),

            new DelegateWorkflowStep("4. Đóng Live Chat, mở menu & nộp báo cáo lừa đảo", async (driver, ctx, ct) =>
            {
                var reason = ctx.CurrentReason ?? new ReportReasonConfig(ReportReasonCategory.FinancialOrCryptoScam, "Phát tán mô hình lừa đảo tài chính và tiền ảo giả mạo.");
                bool submitted = await driver.SubmitLiveStreamReportAsync(reason, ct).ConfigureAwait(false);

                if (!submitted)
                {
                    return ReportStepResult.Failure("Báo cáo livestream", "Không thể hoàn thành quy trình nộp báo cáo vi phạm livestream.");
                }

                string shot = await driver.CaptureScreenshotAsync("04_LiveReportSubmitted", ct).ConfigureAwait(false);
                return ReportStepResult.Success("Báo cáo vi phạm", $"Đã gửi báo cáo livestream vi phạm: [{reason.Category}] - {reason.CustomDetails}", shot);
            }, TimeSpan.FromSeconds(90)),

            new DelegateWorkflowStep("5. Xác thực phản hồi từ YouTube", async (driver, ctx, ct) =>
            {
                bool confirmed = await driver.VerifyReportSubmittedAsync(ct).ConfigureAwait(false);
                string shot = await driver.CaptureScreenshotAsync("05_LiveConfirmed", ct).ConfigureAwait(false);

                if (confirmed)
                {
                    target.ReportsCompleted++;
                    return ReportStepResult.Success("Xác nhận thành công", "YouTube đã xác nhận tiếp nhận báo cáo livestream vi phạm.", shot);
                }

                return ReportStepResult.Success("Hoàn thành báo cáo", "Đã gửi báo cáo (chờ YouTube duyệt tiếp nhận).", shot);
            }, TimeSpan.FromSeconds(45))
        };
    }
}
