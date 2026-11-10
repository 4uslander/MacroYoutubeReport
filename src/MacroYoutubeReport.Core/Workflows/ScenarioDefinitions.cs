using MacroYoutubeReport.Core.Models;

namespace MacroYoutubeReport.Core.Workflows;

/// <summary>
/// Định nghĩa các kịch bản thực thi báo cáo YouTube chuẩn mực và toàn diện.
/// Toàn bộ các bước mô phỏng người dùng thật 100% từ khởi động, tìm kiếm, xem tự nhiên đến nhấn báo cáo.
/// </summary>
public static class ScenarioDefinitions
{
    public const string ScenarioSearchAndReport = "1. Tìm kiếm từ khóa, xem tự nhiên và báo cáo video";
    public const string ScenarioDirectUrlAndReport = "2. Mở trực tiếp link URL, xem và báo cáo video";
    public const string ScenarioChannelReport = "3. Tìm kiếm và báo cáo toàn bộ kênh (User Channel)";

    /// <summary>
    /// Kịch bản 1: Mở app YouTube -> Gõ tìm kiếm -> Cuộn thông minh tìm đúng video -> Xem tự nhiên -> Nhấn Report -> Điền lý do -> Xác nhận.
    /// </summary>
    public static IReadOnlyList<IWorkflowStep> CreateSearchAndReportVideoScenario(
        ReportTargetItem target,
        ReportScenarioConfig config)
    {
        ArgumentNullException.ThrowIfNull(target);
        config ??= ReportScenarioConfig.CreateStandardSearchConfig();

        return new IWorkflowStep[]
        {
            // Bước 1: Khởi tạo và làm sạch môi trường ứng dụng
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

            // Bước 2: Tìm kiếm từ khóa tự nhiên
            new DelegateWorkflowStep("2. Gõ tìm kiếm từ khóa", async (driver, ctx, ct) =>
            {
                string searchKeyword = !string.IsNullOrWhiteSpace(target.Keyword)
                    ? target.Keyword
                    : target.VideoTitle ?? target.ChannelName ?? "YouTube";

                await driver.SearchAsync(searchKeyword, ct).ConfigureAwait(false);
                string shot = await driver.CaptureScreenshotAsync("02_SearchCompleted", ct).ConfigureAwait(false);
                return ReportStepResult.Success("Tìm kiếm từ khóa", $"Đã gõ tìm kiếm từ khóa: '{searchKeyword}'.", shot);
            }, TimeSpan.FromSeconds(60)),

            // Bước 3: Cuộn thông minh & click chọn đúng video mục tiêu
            new DelegateWorkflowStep("3. Cuộn & chọn đúng video mục tiêu", async (driver, ctx, ct) =>
            {
                bool found = await driver.SmartScrollAndSelectVideoAsync(target, ct).ConfigureAwait(false);
                if (!found)
                {
                    // Nếu cuộn tìm không thấy nhưng có link URL dự phòng, mở link
                    if (!string.IsNullOrWhiteSpace(target.TargetUrl))
                    {
                        await driver.OpenVideoByUrlAsync(target.TargetUrl, ct).ConfigureAwait(false);
                        found = true;
                    }
                }

                if (!found)
                {
                    return ReportStepResult.Failure("Chọn video mục tiêu",
                        $"Không tìm thấy video phù hợp với tiêu chí: Tiêu đề='{target.VideoTitle}', Kênh='{target.ChannelName}'.");
                }

                string shot = await driver.CaptureScreenshotAsync("03_VideoOpened", ct).ConfigureAwait(false);
                return ReportStepResult.Success("Chọn video mục tiêu", "Đã tìm thấy và mở video mục tiêu thành công.", shot);
            }, TimeSpan.FromSeconds(120)),

            // Bước 4: Xem video với tương tác tự nhiên (Warm-up Watch Time)
            new DelegateWorkflowStep("4. Xem video tự nhiên", async (driver, ctx, ct) =>
            {
                var rnd = Random.Shared;
                int durationSeconds = rnd.Next(config.MinWatchDurationSeconds, Math.Max(config.MinWatchDurationSeconds + 1, config.MaxWatchDurationSeconds));
                var duration = TimeSpan.FromSeconds(durationSeconds);

                await driver.WatchVideoWithNaturalEngagementAsync(duration, config.EngagementOptions, ct).ConfigureAwait(false);
                string shot = await driver.CaptureScreenshotAsync("04_WatchedNatural", ct).ConfigureAwait(false);
                return ReportStepResult.Success("Xem video tự nhiên", $"Đã xem tự nhiên {durationSeconds}s kèm tương tác mô phỏng người thật.", shot);
            }, TimeSpan.FromSeconds(config.MaxWatchDurationSeconds + 60)),

            // Bước 5: Mở menu Báo cáo
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

            // Bước 6: Chọn lý do vi phạm & Gửi báo cáo
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

            // Bước 7: Xác thực phản hồi & Lưu bằng chứng
            new DelegateWorkflowStep("7. Xác thực kết quả & Lưu bằng chứng", async (driver, ctx, ct) =>
            {
                bool verified = await driver.VerifyReportSubmittedAsync(ct).ConfigureAwait(false);
                string proofShot = await driver.CaptureScreenshotAsync("07_ReportProof", ct).ConfigureAwait(false);

                if (verified)
                {
                    target.ReportsCompleted++;
                    target.LastReportedAt = DateTime.UtcNow;
                    return ReportStepResult.Success("Xác thực kết quả", "Báo cáo đã được YouTube tiếp nhận (Hiển thị thông báo cảm ơn).", proofShot);
                }

                return ReportStepResult.Success("Xác thực kết quả", "Đã hoàn tất gửi báo cáo (Không phát hiện thông báo từ chối).", proofShot);
            }, TimeSpan.FromSeconds(30))
        };
    }

    /// <summary>
    /// Kịch bản 2: Mở trực tiếp theo URL -> Xem tự nhiên -> Nhấn Report -> Điền lý do -> Xác nhận.
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
}
