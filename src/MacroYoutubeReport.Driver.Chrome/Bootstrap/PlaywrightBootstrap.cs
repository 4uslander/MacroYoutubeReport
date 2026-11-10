using Microsoft.Playwright;

namespace MacroYoutubeReport.Driver.Chrome.Bootstrap;

/// <summary>
/// Quản lý khởi tạo Playwright và trình duyệt Google Chrome với cấu hình profile riêng biệt.
/// </summary>
public static class PlaywrightBootstrap
{
    public static async Task<(IPlaywright Playwright, IBrowserContext Context, IPage Page)> LaunchAsync(
        ChromeLaunchConfig config, 
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        var playwright = await Playwright.CreateAsync().ConfigureAwait(false);
        string profilePath = config.ResolveUserDataDir();

        var options = new BrowserTypeLaunchPersistentContextOptions
        {
            Headless = config.Headless,
            Channel = config.Channel,
            SlowMo = config.SlowMoMs,
            ViewportSize = new ViewportSize
            {
                Width = config.ViewportWidth,
                Height = config.ViewportHeight
            },
            Locale = config.PreferredLocale,
            Args = new[]
            {
                "--disable-blink-features=AutomationControlled",
                "--no-default-browser-check",
                "--no-first-run",
                "--mute-audio",
                "--disable-dev-shm-usage",
                "--disable-background-timer-throttling",
                "--disable-backgrounding-occluded-windows",
                "--disable-renderer-backgrounding",
                $"--lang={config.PreferredLocale},vi,en-US,en,ko-KR,ko"
            }
        };

        IBrowserContext context;
        try
        {
            context = await playwright.Chromium.LaunchPersistentContextAsync(profilePath, options).ConfigureAwait(false);
        }
        catch
        {
            // Fallback: khởi chạy engine Chromium nếu Chrome channel không có sẵn
            options.Channel = null;
            context = await playwright.Chromium.LaunchPersistentContextAsync(profilePath, options).ConfigureAwait(false);
        }

        var page = context.Pages.Count > 0 ? context.Pages[0] : await context.NewPageAsync().ConfigureAwait(false);
        return (playwright, context, page);
    }
}
