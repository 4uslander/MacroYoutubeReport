using MacroYoutubeReport.Core.Drivers;
using MacroYoutubeReport.Core.Enums;
using MacroYoutubeReport.Driver.Android.Driver;
using MacroYoutubeReport.Driver.Chrome.Bootstrap;
using MacroYoutubeReport.Driver.Chrome.Driver;
using MacroYoutubeReport.Scheduler.Proxy;

namespace MacroYoutubeReport.Scheduler.Engine;

/// <summary>
/// Giao diện khởi tạo IYouTubeReportDriver theo nền tảng và thiết bị.
/// </summary>
public interface IDriverFactory
{
    IYouTubeReportDriver CreateDriver(PlatformType platform, string targetId, ProxyConfig? proxy = null);
}

/// <summary>
/// Triển khai mặc định tạo driver Android (ADB) hoặc Chrome (Playwright).
/// </summary>
public sealed class DefaultDriverFactory : IDriverFactory
{
    public IYouTubeReportDriver CreateDriver(PlatformType platform, string targetId, ProxyConfig? proxy = null)
    {
        return platform switch
        {
            PlatformType.Chrome => new PlaywrightChromeReportDriver(targetId, new ChromeLaunchConfig
            {
                ProfileName = targetId
            }),
            _ => new AdbAndroidReportDriver(targetId)
        };
    }
}
