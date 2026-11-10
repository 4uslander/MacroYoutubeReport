namespace MacroYoutubeReport.Driver.Chrome.Bootstrap;

/// <summary>
/// Cấu hình khởi chạy trình duyệt Google Chrome thông qua Playwright.
/// </summary>
public sealed record ChromeLaunchConfig
{
    public string ProfileName { get; init; } = "Default";
    public string? UserDataDir { get; init; }
    public bool Headless { get; init; } = false;
    public int ViewportWidth { get; init; } = 1280;
    public int ViewportHeight { get; init; } = 720;
    public string PreferredLocale { get; init; } = "vi-VN";
    public string Channel { get; init; } = "chrome";
    public int SlowMoMs { get; init; } = 50;

    public string ResolveUserDataDir()
    {
        if (!string.IsNullOrWhiteSpace(UserDataDir))
        {
            return UserDataDir;
        }

        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string profileDir = Path.Combine(localAppData, "MacroYoutubeReport", "ChromeProfiles", ProfileName);
        Directory.CreateDirectory(profileDir);
        return profileDir;
    }
}
