using MacroYoutubeReport.Core.Drivers;
using MacroYoutubeReport.Core.Enums;
using MacroYoutubeReport.Core.Models;
using MacroYoutubeReport.Driver.Chrome.Locators;

namespace MacroYoutubeReport.Driver.Chrome.Driver;

/// <summary>
/// Triển khai IYouTubeReportDriver cho trình duyệt Chrome thông qua Playwright.
/// </summary>
public sealed class PlaywrightChromeReportDriver : IYouTubeReportDriver
{
    private readonly string _profileName;

    public PlatformType Platform => PlatformType.Chrome;
    public string TargetId => _profileName;

    public PlaywrightChromeReportDriver(string profileName)
    {
        _profileName = string.IsNullOrWhiteSpace(profileName)
            ? throw new ArgumentNullException(nameof(profileName))
            : profileName.Trim();
    }

    public Task InitializeAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task EnsureAppReadyAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task NavigateHomeAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task SearchAsync(string keyword, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task<bool> SmartScrollAndSelectVideoAsync(ReportTargetItem target, CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }

    public Task OpenVideoByUrlAsync(string url, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task OpenChannelPageAsync(string channelUrlOrName, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task WatchVideoWithNaturalEngagementAsync(TimeSpan duration, NaturalEngagementOptions options, CancellationToken ct = default)
    {
        return Task.Delay(100, ct);
    }

    public Task<bool> OpenReportMenuAsync(CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> SubmitVideoReportAsync(ReportReasonConfig reasonConfig, CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> SubmitChannelReportAsync(ReportReasonConfig reasonConfig, CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }

    public Task<bool> VerifyReportSubmittedAsync(CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }

    public Task<string> CaptureScreenshotAsync(string stepPrefix, CancellationToken ct = default)
    {
        return Task.FromResult(string.Empty);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
