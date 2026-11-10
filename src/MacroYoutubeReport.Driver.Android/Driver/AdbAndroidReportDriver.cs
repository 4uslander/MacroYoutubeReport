using MacroYoutubeReport.Core.Drivers;
using MacroYoutubeReport.Core.Enums;
using MacroYoutubeReport.Core.Models;
using MacroYoutubeReport.Driver.Android.Locators;

namespace MacroYoutubeReport.Driver.Android.Driver;

/// <summary>
/// Triển khai IYouTubeReportDriver cho thiết bị thật hoặc giả lập Android qua kết nối ADB.
/// </summary>
public sealed class AdbAndroidReportDriver : IYouTubeReportDriver
{
    private readonly string _serial;

    public PlatformType Platform => PlatformType.Android;
    public string TargetId => _serial;

    public AdbAndroidReportDriver(string serial)
    {
        _serial = string.IsNullOrWhiteSpace(serial)
            ? throw new ArgumentNullException(nameof(serial))
            : serial.Trim();
    }

    public Task InitializeAsync(CancellationToken ct = default)
    {
        // Phase 1: Khởi tạo kết nối cấu trúc
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
