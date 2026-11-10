using Microsoft.Playwright;
using System.Text.RegularExpressions;
using MacroYoutubeReport.Core.Drivers;
using MacroYoutubeReport.Core.Enums;
using MacroYoutubeReport.Core.Localization;
using MacroYoutubeReport.Core.Models;
using MacroYoutubeReport.Driver.Chrome.Bootstrap;
using MacroYoutubeReport.Driver.Chrome.Locators;

namespace MacroYoutubeReport.Driver.Chrome.Driver;

/// <summary>
/// Triển khai IYouTubeReportDriver cho trình duyệt Google Chrome thông qua Microsoft Playwright.
/// Hỗ trợ tài khoản đăng nhập liên tục (Persistent Profile), tương tác tự nhiên, xử lý quảng cáo và báo cáo video/kênh/shorts/comment.
/// </summary>
public sealed class PlaywrightChromeReportDriver : IYouTubeReportDriver
{
    private readonly string _profileName;
    private readonly ChromeLaunchConfig _config;

    private IPlaywright? _playwright;
    private IBrowserContext? _context;
    private IPage? _page;

    public PlatformType Platform => PlatformType.Chrome;
    public string TargetId => _profileName;

    public PlaywrightChromeReportDriver(string profileName, ChromeLaunchConfig? config = null)
    {
        _profileName = string.IsNullOrWhiteSpace(profileName)
            ? throw new ArgumentNullException(nameof(profileName))
            : profileName.Trim();

        _config = config ?? new ChromeLaunchConfig { ProfileName = _profileName };
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_page != null) return;

        var launched = await PlaywrightBootstrap.LaunchAsync(_config, ct).ConfigureAwait(false);
        _playwright = launched.Playwright;
        _context = launched.Context;
        _page = launched.Page;
    }

    public async Task EnsureAppReadyAsync(CancellationToken ct = default)
    {
        var page = EnsurePage();

        if (string.IsNullOrWhiteSpace(page.Url) || page.Url.Equals("about:blank", StringComparison.OrdinalIgnoreCase))
        {
            await page.GotoAsync(ChromeLocators.YoutubeHomeUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }).ConfigureAwait(false);
        }

        try
        {
            var consentBtn = await page.QuerySelectorAsync("button[aria-label*='Accept'], button[aria-label*='đồng ý'], button[aria-label*='Agree'], tp-yt-paper-button:has-text('Reject all')").ConfigureAwait(false);
            if (consentBtn != null)
            {
                await consentBtn.ClickAsync().ConfigureAwait(false);
                await Task.Delay(1000, ct).ConfigureAwait(false);
            }
        }
        catch
        {
            // Bỏ qua nếu không có popup consent
        }
    }

    public async Task EnsureNoAdPlayingAsync(CancellationToken ct = default)
    {
        var page = EnsurePage();

        for (int retry = 0; retry < 5; retry++)
        {
            ct.ThrowIfCancellationRequested();

            var skipBtn = await page.QuerySelectorAsync(".ytp-skip-ad-button, .ytp-ad-skip-button-modern, button.ytp-ad-skip-button").ConfigureAwait(false);
            if (skipBtn != null && await skipBtn.IsVisibleAsync().ConfigureAwait(false))
            {
                await skipBtn.ClickAsync().ConfigureAwait(false);
                await Task.Delay(1500, ct).ConfigureAwait(false);
                return;
            }

            var adContainer = await page.QuerySelectorAsync(".ytp-ad-player-overlay, .ad-showing").ConfigureAwait(false);
            if (adContainer != null && await adContainer.IsVisibleAsync().ConfigureAwait(false))
            {
                await Task.Delay(2000, ct).ConfigureAwait(false);
            }
            else
            {
                break;
            }
        }
    }

    public async Task NavigateHomeAsync(CancellationToken ct = default)
    {
        var page = EnsurePage();
        await page.GotoAsync(ChromeLocators.YoutubeHomeUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }).ConfigureAwait(false);
        await Task.Delay(1500, ct).ConfigureAwait(false);
    }

    public async Task SearchAsync(string keyword, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyword);
        var page = EnsurePage();
        await EnsureAppReadyAsync(ct).ConfigureAwait(false);

        var searchInput = await page.WaitForSelectorAsync(ChromeLocators.SearchInput, new PageWaitForSelectorOptions { Timeout = 10000 }).ConfigureAwait(false);
        if (searchInput != null)
        {
            await searchInput.ClickAsync().ConfigureAwait(false);
            await searchInput.FillAsync(string.Empty).ConfigureAwait(false);

            var rnd = Random.Shared;
            foreach (char c in keyword)
            {
                ct.ThrowIfCancellationRequested();
                await page.Keyboard.TypeAsync(c.ToString()).ConfigureAwait(false);
                await Task.Delay(rnd.Next(60, 140), ct).ConfigureAwait(false);
            }

            await page.Keyboard.PressAsync("Enter").ConfigureAwait(false);
            await Task.Delay(2500, ct).ConfigureAwait(false);
        }
        else
        {
            string searchUrl = $"{ChromeLocators.YoutubeSearchUrl}{Uri.EscapeDataString(keyword)}";
            await page.GotoAsync(searchUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }).ConfigureAwait(false);
            await Task.Delay(2000, ct).ConfigureAwait(false);
        }
    }

    public async Task<bool> SmartScrollAndSelectVideoAsync(ReportTargetItem target, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        var page = EnsurePage();

        int maxScrollRounds = 8;
        for (int i = 0; i < maxScrollRounds; i++)
        {
            ct.ThrowIfCancellationRequested();

            var videoElements = await page.QuerySelectorAllAsync(ChromeLocators.VideoRow).ConfigureAwait(false);
            foreach (var videoEl in videoElements)
            {
                var titleEl = await videoEl.QuerySelectorAsync(ChromeLocators.VideoTitleLink).ConfigureAwait(false);
                if (titleEl == null) continue;

                string? title = await titleEl.InnerTextAsync().ConfigureAwait(false);
                string? channel = null;

                var channelEl = await videoEl.QuerySelectorAsync(ChromeLocators.ChannelNameLink).ConfigureAwait(false);
                if (channelEl != null)
                {
                    channel = await channelEl.InnerTextAsync().ConfigureAwait(false);
                }

                if (target.Matches(title, channel, null))
                {
                    await titleEl.ScrollIntoViewIfNeededAsync().ConfigureAwait(false);
                    await Task.Delay(1000, ct).ConfigureAwait(false);
                    await titleEl.ClickAsync().ConfigureAwait(false);
                    await Task.Delay(2500, ct).ConfigureAwait(false);
                    await EnsureNoAdPlayingAsync(ct).ConfigureAwait(false);
                    return true;
                }
            }

            await page.Mouse.WheelAsync(0, 600).ConfigureAwait(false);
            await Task.Delay(1500, ct).ConfigureAwait(false);
        }

        return false;
    }

    public async Task OpenVideoByUrlAsync(string url, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        var page = EnsurePage();
        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }).ConfigureAwait(false);
        await Task.Delay(2500, ct).ConfigureAwait(false);
        await EnsureNoAdPlayingAsync(ct).ConfigureAwait(false);
    }

    public async Task OpenChannelPageAsync(string channelUrlOrName, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelUrlOrName);
        var page = EnsurePage();

        if (channelUrlOrName.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            await page.GotoAsync(channelUrlOrName, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }).ConfigureAwait(false);
        }
        else
        {
            await SearchAsync(channelUrlOrName, ct).ConfigureAwait(false);
            var channelEl = await page.QuerySelectorAsync("#channel-title, ytd-channel-renderer #avatar").ConfigureAwait(false);
            if (channelEl != null)
            {
                await channelEl.ClickAsync().ConfigureAwait(false);
            }
        }
        await Task.Delay(2000, ct).ConfigureAwait(false);
    }

    public async Task WatchVideoWithNaturalEngagementAsync(TimeSpan duration, NaturalEngagementOptions options, CancellationToken ct = default)
    {
        var page = EnsurePage();
        options ??= NaturalEngagementOptions.Default;

        await EnsureNoAdPlayingAsync(ct).ConfigureAwait(false);

        DateTime endTime = DateTime.UtcNow + duration;
        var rnd = Random.Shared;
        bool didScrollComments = false;

        while (DateTime.UtcNow < endTime)
        {
            ct.ThrowIfCancellationRequested();

            var remaining = endTime - DateTime.UtcNow;
            if (remaining.TotalSeconds <= 0) break;

            if (options.EnableCommentReadingScroll && !didScrollComments && remaining.TotalSeconds > 15)
            {
                await Task.Delay(rnd.Next(4000, 8000), ct).ConfigureAwait(false);

                await page.Mouse.WheelAsync(0, 500).ConfigureAwait(false);
                await Task.Delay(rnd.Next(3000, 6000), ct).ConfigureAwait(false);

                await page.Mouse.WheelAsync(0, -500).ConfigureAwait(false);
                didScrollComments = true;
            }

            int sleepChunk = Math.Min(3000, (int)remaining.TotalMilliseconds);
            if (sleepChunk > 0)
            {
                await Task.Delay(sleepChunk, ct).ConfigureAwait(false);
            }
        }
    }

    public async Task<bool> OpenReportMenuAsync(CancellationToken ct = default)
    {
        var page = EnsurePage();

        var moreBtn = await page.QuerySelectorAsync(ChromeLocators.MoreActionsButton).ConfigureAwait(false);
        if (moreBtn == null)
        {
            moreBtn = await page.QuerySelectorAsync("ytd-menu-renderer tp-yt-paper-icon-button, button.yt-spec-button-shape-next[aria-label*='More'], button.yt-spec-button-shape-next[aria-label*='khác']").ConfigureAwait(false);
        }

        if (moreBtn != null)
        {
            await moreBtn.ClickAsync().ConfigureAwait(false);
            await Task.Delay(800, ct).ConfigureAwait(false);

            var reportKeywords = UiLanguageManager.GetReportMenuKeywords();
            var menuItems = await page.QuerySelectorAllAsync(ChromeLocators.MenuItemReport).ConfigureAwait(false);

            foreach (var item in menuItems)
            {
                string text = await item.InnerTextAsync().ConfigureAwait(false);
                foreach (var kw in reportKeywords)
                {
                    if (text.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    {
                        await item.ClickAsync().ConfigureAwait(false);
                        await Task.Delay(1200, ct).ConfigureAwait(false);
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public async Task<bool> SubmitVideoReportAsync(ReportReasonConfig reasonConfig, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(reasonConfig);
        var page = EnsurePage();

        await page.WaitForSelectorAsync(ChromeLocators.ReportModal, new PageWaitForSelectorOptions { Timeout = 8000 }).ConfigureAwait(false);

        var keywords = UiLanguageManager.GetAllKeywordsForCategory(reasonConfig.Category);
        var radioButtons = await page.QuerySelectorAllAsync(ChromeLocators.ReportRadioOption).ConfigureAwait(false);

        bool selected = false;
        foreach (var radio in radioButtons)
        {
            string text = await radio.InnerTextAsync().ConfigureAwait(false);
            foreach (var kw in keywords)
            {
                if (text.Contains(kw, StringComparison.OrdinalIgnoreCase))
                {
                    await radio.ClickAsync().ConfigureAwait(false);
                    selected = true;
                    break;
                }
            }
            if (selected) break;
        }

        if (!selected && radioButtons.Count > 0)
        {
            await radioButtons[0].ClickAsync().ConfigureAwait(false);
        }

        await Task.Delay(800, ct).ConfigureAwait(false);

        var nextBtn = await page.QuerySelectorAsync(ChromeLocators.ReportNextButton).ConfigureAwait(false);
        if (nextBtn != null && await nextBtn.IsVisibleAsync().ConfigureAwait(false))
        {
            await nextBtn.ClickAsync().ConfigureAwait(false);
            await Task.Delay(1000, ct).ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(reasonConfig.CustomDetails))
        {
            var textArea = await page.QuerySelectorAsync(ChromeLocators.ReportTextArea).ConfigureAwait(false);
            if (textArea != null && await textArea.IsVisibleAsync().ConfigureAwait(false))
            {
                await textArea.FillAsync(reasonConfig.CustomDetails).ConfigureAwait(false);
                await Task.Delay(600, ct).ConfigureAwait(false);
            }
        }

        var submitBtn = await page.QuerySelectorAsync(ChromeLocators.ReportSubmitButton).ConfigureAwait(false);
        if (submitBtn == null)
        {
            submitBtn = await page.QuerySelectorAsync("button:has-text('Report'), button:has-text('Báo cáo'), button:has-text('신고')").ConfigureAwait(false);
        }

        if (submitBtn != null)
        {
            await submitBtn.ClickAsync().ConfigureAwait(false);
            await Task.Delay(1500, ct).ConfigureAwait(false);
            return true;
        }

        return false;
    }

    public async Task<bool> SubmitChannelReportAsync(ReportReasonConfig reasonConfig, CancellationToken ct = default)
    {
        var page = EnsurePage();

        var flagBtn = await page.QuerySelectorAsync("button[aria-label*='Report'], ytd-channel-about-metadata-renderer button").ConfigureAwait(false);
        if (flagBtn != null)
        {
            await flagBtn.ClickAsync().ConfigureAwait(false);
            await Task.Delay(1000, ct).ConfigureAwait(false);
            return await SubmitVideoReportAsync(reasonConfig, ct).ConfigureAwait(false);
        }

        return false;
    }

    public async Task<bool> SubmitShortsReportAsync(ReportReasonConfig reasonConfig, CancellationToken ct = default)
    {
        var page = EnsurePage();

        var menuBtn = await page.QuerySelectorAsync("ytd-reel-video-renderer[is-active] ytd-menu-renderer button, ytd-shorts #menu button").ConfigureAwait(false);
        if (menuBtn != null)
        {
            await menuBtn.ClickAsync().ConfigureAwait(false);
            await Task.Delay(800, ct).ConfigureAwait(false);

            var reportItem = await page.QuerySelectorAsync("ytd-menu-service-item-renderer:has-text('Report'), ytd-menu-service-item-renderer:has-text('Báo cáo')").ConfigureAwait(false);
            if (reportItem != null)
            {
                await reportItem.ClickAsync().ConfigureAwait(false);
                await Task.Delay(1000, ct).ConfigureAwait(false);
                return await SubmitVideoReportAsync(reasonConfig, ct).ConfigureAwait(false);
            }
        }

        return false;
    }

    public async Task<bool> SubmitLiveStreamReportAsync(ReportReasonConfig reasonConfig, CancellationToken ct = default)
    {
        var page = EnsurePage();

        var hideChatBtn = await page.QuerySelectorAsync("#show-hide-button button, ytd-live-chat-frame #show-hide-button").ConfigureAwait(false);
        if (hideChatBtn != null)
        {
            try
            {
                await hideChatBtn.ClickAsync().ConfigureAwait(false);
                await Task.Delay(800, ct).ConfigureAwait(false);
            }
            catch
            {
                // Bỏ qua nếu không click được
            }
        }

        bool opened = await OpenReportMenuAsync(ct).ConfigureAwait(false);
        if (!opened)
        {
            var moreBtn = await page.QuerySelectorAsync("ytd-watch-metadata #actions ytd-menu-renderer button, button[aria-label*='More'], button[aria-label*='khác']").ConfigureAwait(false);
            if (moreBtn != null)
            {
                await moreBtn.ClickAsync().ConfigureAwait(false);
                await Task.Delay(800, ct).ConfigureAwait(false);

                var reportItem = await page.QuerySelectorAsync("ytd-menu-service-item-renderer:has-text('Report'), ytd-menu-service-item-renderer:has-text('Báo cáo'), ytd-menu-service-item-renderer:has-text('신고')").ConfigureAwait(false);
                if (reportItem != null)
                {
                    await reportItem.ClickAsync().ConfigureAwait(false);
                    await Task.Delay(1000, ct).ConfigureAwait(false);
                    opened = true;
                }
            }
        }

        return await SubmitVideoReportAsync(reasonConfig, ct).ConfigureAwait(false);
    }

    public async Task<bool> SubmitCommentReportAsync(string commentAuthorOrKeyword, ReportReasonConfig reasonConfig, CancellationToken ct = default)
    {
        var page = EnsurePage();

        // 1. Cuộn đến khu vực bình luận
        await page.Mouse.WheelAsync(0, 800).ConfigureAwait(false);
        await Task.Delay(1500, ct).ConfigureAwait(false);

        var commentThreads = await page.QuerySelectorAllAsync("ytd-comment-thread-renderer").ConfigureAwait(false);
        foreach (var thread in commentThreads)
        {
            string threadText = await thread.InnerTextAsync().ConfigureAwait(false);
            if (threadText.Contains(commentAuthorOrKeyword, StringComparison.OrdinalIgnoreCase))
            {
                var actionBtn = await thread.QuerySelectorAsync("#action-menu button").ConfigureAwait(false);
                if (actionBtn != null)
                {
                    await actionBtn.ClickAsync().ConfigureAwait(false);
                    await Task.Delay(600, ct).ConfigureAwait(false);

                    var reportItem = await page.QuerySelectorAsync("ytd-menu-service-item-renderer:has-text('Report'), ytd-menu-service-item-renderer:has-text('Báo cáo')").ConfigureAwait(false);
                    if (reportItem != null)
                    {
                        await reportItem.ClickAsync().ConfigureAwait(false);
                        await Task.Delay(1000, ct).ConfigureAwait(false);
                        return await SubmitVideoReportAsync(reasonConfig, ct).ConfigureAwait(false);
                    }
                }
            }
        }

        return false;
    }

    public async Task<bool> VerifyReportSubmittedAsync(CancellationToken ct = default)
    {
        var page = EnsurePage();

        try
        {
            var snackbar = await page.QuerySelectorAsync("ytd-notification-action-renderer, tp-yt-paper-toast").ConfigureAwait(false);
            if (snackbar != null)
            {
                string text = await snackbar.InnerTextAsync().ConfigureAwait(false);
                var successKeywords = UiLanguageManager.GetSuccessConfirmationKeywords();
                foreach (var kw in successKeywords)
                {
                    if (text.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }
        catch
        {
            // Bỏ qua lỗi truy vấn snackbar
        }

        return true;
    }

    public async Task<string> CaptureScreenshotAsync(string stepPrefix, CancellationToken ct = default)
    {
        var page = EnsurePage();
        string safePrefix = Regex.Replace(stepPrefix ?? "Step", @"[^\w\.-]", "_");
        string fileName = $"Chrome_{_profileName}_{safePrefix}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";
        string targetDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
        Directory.CreateDirectory(targetDir);

        string targetPath = Path.Combine(targetDir, fileName);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = targetPath, FullPage = false }).ConfigureAwait(false);
        return targetPath;
    }

    private IPage EnsurePage() =>
        _page ?? throw new InvalidOperationException($"Driver Chrome '{_profileName}' chưa được khởi tạo qua InitializeAsync.");

    public async ValueTask DisposeAsync()
    {
        if (_page != null)
        {
            await _page.CloseAsync().ConfigureAwait(false);
            _page = null;
        }

        if (_context != null)
        {
            await _context.CloseAsync().ConfigureAwait(false);
            _context = null;
        }

        _playwright?.Dispose();
        _playwright = null;
    }
}
