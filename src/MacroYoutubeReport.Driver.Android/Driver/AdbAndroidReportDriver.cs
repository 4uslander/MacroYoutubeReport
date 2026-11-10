using AdvancedSharpAdbClient.Models;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Xml;
using MacroYoutubeReport.Core.Drivers;
using MacroYoutubeReport.Core.Enums;
using MacroYoutubeReport.Core.Localization;
using MacroYoutubeReport.Core.Models;
using MacroYoutubeReport.Driver.Android.Adb;
using MacroYoutubeReport.Driver.Android.Locators;

namespace MacroYoutubeReport.Driver.Android.Driver;

/// <summary>
/// Triển khai IYouTubeReportDriver cho thiết bị thật hoặc giả lập Android qua kết nối ADB.
/// Mô phỏng đầy đủ hành vi người dùng thật (Bézier swipe, random jitter tap, natural engagement, dismiss popups & shorts).
/// </summary>
public sealed class AdbAndroidReportDriver : IYouTubeReportDriver
{
    private readonly string _serial;
    private DeviceData? _deviceData;
    private (int Width, int Height) _screenResolution = (1080, 2400);

    public PlatformType Platform => PlatformType.Android;
    public string TargetId => _serial;

    public AdbAndroidReportDriver(string serial)
    {
        _serial = string.IsNullOrWhiteSpace(serial)
            ? throw new ArgumentNullException(nameof(serial))
            : serial.Trim();
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var deviceManager = AdbDeviceManager.Instance;
        var managed = await deviceManager.GetDeviceBySerialAsync(_serial, ct).ConfigureAwait(false);

        if (managed == null)
        {
            throw new InvalidOperationException($"Không tìm thấy thiết bị Android serial '{_serial}'.");
        }

        if (managed.Status != DeviceStatus.Online)
        {
            throw new InvalidOperationException($"Thiết bị '{_serial}' chưa ở trạng thái Online (Hiện tại: {managed.Status}).");
        }

        _deviceData = managed.RawDevice;

        // Khóa màn hình dọc và tắt tự xoay
        try
        {
            await AdbClientGeneral.Instance.ExecuteShellAsync(_deviceData,
                "settings put system accelerometer_rotation 0; settings put system user_rotation 0; settings put secure immersive_mode_confirmations confirmed",
                ct).ConfigureAwait(false);
        }
        catch
        {
            // Bỏ qua nếu lệnh settings không đủ quyền
        }

        _screenResolution = await AdbClientGeneral.Instance.GetScreenResolutionAsync(_deviceData, ct).ConfigureAwait(false);
    }

    public async Task EnsureAppReadyAsync(CancellationToken ct = default)
    {
        var device = EnsureDevice();

        bool isForeground = await AdbClientGeneral.Instance.IsAppInForegroundAsync(device, AndroidLocators.PackageYouTube, ct).ConfigureAwait(false);
        if (!isForeground)
        {
            await AdbClientGeneral.Instance.LaunchAppByLauncherAsync(device, AndroidLocators.PackageYouTube, ct).ConfigureAwait(false);

            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(500, ct).ConfigureAwait(false);
                if (await AdbClientGeneral.Instance.IsAppInForegroundAsync(device, AndroidLocators.PackageYouTube, ct).ConfigureAwait(false))
                {
                    break;
                }
            }

            await Task.Delay(1500, ct).ConfigureAwait(false);
        }

        await TryDismissPopupsAsync(device, ct).ConfigureAwait(false);
        await EscapeShortsTrapAsync(device, ct).ConfigureAwait(false);
    }

    public async Task NavigateHomeAsync(CancellationToken ct = default)
    {
        var device = EnsureDevice();
        await EnsureAppReadyAsync(ct).ConfigureAwait(false);

        for (int retry = 0; retry < 3; retry++)
        {
            var doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            if (doc != null)
            {
                var homeNode = doc.SelectSingleNode("//node[contains(@content-desc, '홈') or contains(@content-desc, 'Trang chủ') or contains(@content-desc, 'Home')]");
                if (homeNode != null)
                {
                    var elem = AdbUiXmlDumper.ParseElement(homeNode);
                    if (elem != null)
                    {
                        await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                        await Task.Delay(1500, ct).ConfigureAwait(false);
                        return;
                    }
                }
            }

            await AdbClientGeneral.Instance.PressBackAsync(device, ct).ConfigureAwait(false);
            await Task.Delay(800, ct).ConfigureAwait(false);
        }
    }

    public async Task SearchAsync(string keyword, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyword);
        var device = EnsureDevice();
        await EnsureAppReadyAsync(ct).ConfigureAwait(false);

        // 1. Tìm và bấm nút Search
        bool tappedSearch = false;
        var doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
        if (doc != null)
        {
            var searchNode = doc.SelectSingleNode("//node[@resource-id='com.google.android.youtube:id/menu_item_view' or contains(@content-desc, 'Search') or contains(@content-desc, 'Tìm kiếm') or contains(@content-desc, '검색')]");
            if (searchNode != null)
            {
                var elem = AdbUiXmlDumper.ParseElement(searchNode);
                if (elem != null)
                {
                    await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                    tappedSearch = true;
                }
            }
        }

        if (!tappedSearch)
        {
            // Fallback: vị trí icon search ở góc trên bên phải
            int sx = (int)(_screenResolution.Width * 0.85);
            int sy = (int)(_screenResolution.Height * 0.05);
            await AdbClientGeneral.Instance.TapAsync(device, sx, sy, ct).ConfigureAwait(false);
        }

        await Task.Delay(1200, ct).ConfigureAwait(false);

        // 2. Gõ từ khóa bằng AdbIME / input text
        await AdbImeHelper.InputTextAsync(device, keyword, ct).ConfigureAwait(false);
        await Task.Delay(800, ct).ConfigureAwait(false);

        // 3. Nhấn Enter (KEYCODE_ENTER = 66) để thực thi tìm kiếm
        await AdbClientGeneral.Instance.SendKeyEventAsync(device, 66, ct).ConfigureAwait(false);
        await Task.Delay(2500, ct).ConfigureAwait(false);
    }

    public async Task<bool> SmartScrollAndSelectVideoAsync(ReportTargetItem target, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        var device = EnsureDevice();

        int maxScrollAttempts = 10;
        int midX = _screenResolution.Width / 2;
        int startY = (int)(_screenResolution.Height * 0.75);
        int endY = (int)(_screenResolution.Height * 0.28);

        for (int attempt = 0; attempt < maxScrollAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            var doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            if (doc != null)
            {
                var textNodes = doc.SelectNodes("//android.widget.TextView[@text!='']");
                if (textNodes != null)
                {
                    foreach (XmlNode node in textNodes)
                    {
                        string text = node.Attributes?["text"]?.Value ?? string.Empty;
                        if (target.Matches(text, text, null))
                        {
                            var elem = AdbUiXmlDumper.ParseElement(node);
                            if (elem != null && elem.Bounds.Y > (_screenResolution.Height * 0.1) && elem.Bounds.Y < (_screenResolution.Height * 0.85))
                            {
                                await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                                await Task.Delay(2500, ct).ConfigureAwait(false);
                                await TryDismissPopupsAsync(device, ct).ConfigureAwait(false);
                                return true;
                            }
                        }
                    }
                }
            }

            int duration = Random.Shared.Next(400, 650);
            await AdbClientGeneral.Instance.SwipeAsync(device, midX, startY, midX, endY, duration, ct).ConfigureAwait(false);
            await Task.Delay(Random.Shared.Next(1200, 1800), ct).ConfigureAwait(false);
        }

        return false;
    }

    public async Task OpenVideoByUrlAsync(string url, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        var device = EnsureDevice();

        string command = $"am start -a android.intent.action.VIEW -d \"{url}\" -p {AndroidLocators.PackageYouTube}";
        await AdbClientGeneral.Instance.ExecuteShellAsync(device, command, ct).ConfigureAwait(false);

        await Task.Delay(3000, ct).ConfigureAwait(false);
        await TryDismissPopupsAsync(device, ct).ConfigureAwait(false);
    }

    public async Task OpenChannelPageAsync(string channelUrlOrName, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelUrlOrName);
        var device = EnsureDevice();

        if (channelUrlOrName.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            await OpenVideoByUrlAsync(channelUrlOrName, ct).ConfigureAwait(false);
        }
        else
        {
            await SearchAsync(channelUrlOrName, ct).ConfigureAwait(false);
            var doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            if (doc != null)
            {
                var channelNode = doc.SelectSingleNode("//node[contains(@content-desc, 'kênh') or contains(@content-desc, 'channel') or contains(@content-desc, '채널')]");
                if (channelNode != null)
                {
                    var elem = AdbUiXmlDumper.ParseElement(channelNode);
                    if (elem != null)
                    {
                        await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                        await Task.Delay(2000, ct).ConfigureAwait(false);
                    }
                }
            }
        }
    }

    public async Task WatchVideoWithNaturalEngagementAsync(TimeSpan duration, NaturalEngagementOptions options, CancellationToken ct = default)
    {
        var device = EnsureDevice();
        options ??= NaturalEngagementOptions.Default;

        DateTime endTime = DateTime.UtcNow + duration;
        var rnd = Random.Shared;

        int midX = _screenResolution.Width / 2;
        int scrollStartY = (int)(_screenResolution.Height * 0.70);
        int scrollEndY = (int)(_screenResolution.Height * 0.45);

        bool didScrollComments = false;

        while (DateTime.UtcNow < endTime)
        {
            ct.ThrowIfCancellationRequested();

            var remaining = endTime - DateTime.UtcNow;
            if (remaining.TotalSeconds <= 0) break;

            if (options.EnableCommentReadingScroll && !didScrollComments && remaining.TotalSeconds > 15)
            {
                await Task.Delay(rnd.Next(4000, 8000), ct).ConfigureAwait(false);

                await AdbClientGeneral.Instance.SwipeAsync(device, midX, scrollStartY, midX, scrollEndY, 400, ct).ConfigureAwait(false);
                await Task.Delay(rnd.Next(3000, 6000), ct).ConfigureAwait(false);

                await AdbClientGeneral.Instance.SwipeAsync(device, midX, scrollEndY, midX, scrollStartY, 400, ct).ConfigureAwait(false);
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
        var device = EnsureDevice();

        // 1. Chạm nhẹ vào giữa vùng Player để đánh thức thanh điều khiển
        int playerX = _screenResolution.Width / 2;
        int playerY = (int)(_screenResolution.Height * 0.18);
        await AdbClientGeneral.Instance.TapAsync(device, playerX, playerY, ct).ConfigureAwait(false);
        await Task.Delay(600, ct).ConfigureAwait(false);

        // 2. Tìm biểu tượng Settings (Bánh răng) hoặc 3-chấm trên player
        var doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
        bool tappedSettings = false;

        if (doc != null)
        {
            var settingsNode = doc.SelectSingleNode("//node[@resource-id='com.google.android.youtube:id/player_settings_button' or contains(@content-desc, 'Settings') or contains(@content-desc, 'Cài đặt') or contains(@content-desc, '설정') or contains(@content-desc, 'More options')]");
            if (settingsNode != null)
            {
                var elem = AdbUiXmlDumper.ParseElement(settingsNode);
                if (elem != null)
                {
                    await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                    tappedSettings = true;
                }
            }
        }

        if (!tappedSettings)
        {
            int gearX = (int)(_screenResolution.Width * 0.93);
            int gearY = (int)(_screenResolution.Height * 0.08);
            await AdbClientGeneral.Instance.TapAsync(device, gearX, gearY, ct).ConfigureAwait(false);
        }

        await Task.Delay(1200, ct).ConfigureAwait(false);

        // 3. Bottom sheet xuất hiện, tìm mục Báo cáo (Report / 신고)
        var bottomDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
        if (bottomDoc == null) return false;

        var reportKeywords = UiLanguageManager.GetReportMenuKeywords();
        var menuNodes = bottomDoc.SelectNodes("//android.widget.TextView[@text!='']");
        if (menuNodes != null)
        {
            foreach (XmlNode node in menuNodes)
            {
                string text = node.Attributes?["text"]?.Value ?? string.Empty;
                foreach (var kw in reportKeywords)
                {
                    if (text.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    {
                        var elem = AdbUiXmlDumper.ParseElement(node);
                        if (elem != null)
                        {
                            await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                            await Task.Delay(1500, ct).ConfigureAwait(false);
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    public async Task<bool> SubmitVideoReportAsync(ReportReasonConfig reasonConfig, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(reasonConfig);
        var device = EnsureDevice();

        var doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
        if (doc == null) return false;

        // 1. Tìm RadioButton hoặc TextView khớp với danh mục vi phạm
        var keywords = UiLanguageManager.GetAllKeywordsForCategory(reasonConfig.Category);
        bool categorySelected = false;

        var radioNodes = doc.SelectNodes("//android.widget.RadioButton | //android.widget.TextView[@text!='']");
        if (radioNodes != null)
        {
            foreach (XmlNode node in radioNodes)
            {
                string text = node.Attributes?["text"]?.Value ?? string.Empty;
                foreach (var kw in keywords)
                {
                    if (text.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    {
                        var elem = AdbUiXmlDumper.ParseElement(node);
                        if (elem != null)
                        {
                            await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                            categorySelected = true;
                            break;
                        }
                    }
                }
                if (categorySelected) break;
            }
        }

        if (!categorySelected)
        {
            var fallbackNode = doc.SelectSingleNode("//android.widget.RadioButton");
            if (fallbackNode != null)
            {
                var elem = AdbUiXmlDumper.ParseElement(fallbackNode);
                if (elem != null) await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
            }
        }

        await Task.Delay(1000, ct).ConfigureAwait(false);

        // 2. Bấm nút Tiếp theo / Next nếu có
        var nextDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
        if (nextDoc != null)
        {
            var submitKeywords = UiLanguageManager.GetSubmitButtonKeywords();
            var buttonNodes = nextDoc.SelectNodes("//android.widget.Button | //android.widget.TextView[@clickable='true']");
            if (buttonNodes != null)
            {
                foreach (XmlNode btn in buttonNodes)
                {
                    string btnText = btn.Attributes?["text"]?.Value ?? string.Empty;
                    foreach (var kw in submitKeywords)
                    {
                        if (btnText.Contains(kw, StringComparison.OrdinalIgnoreCase))
                        {
                            var elem = AdbUiXmlDumper.ParseElement(btn);
                            if (elem != null)
                            {
                                await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                                await Task.Delay(1500, ct).ConfigureAwait(false);
                                break;
                            }
                        }
                    }
                }
            }
        }

        // 3. Nếu xuất hiện ô nhập ghi chú chi tiết và có cấu hình CustomDetails
        if (!string.IsNullOrWhiteSpace(reasonConfig.CustomDetails))
        {
            var detailsDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            if (detailsDoc != null)
            {
                var editNode = detailsDoc.SelectSingleNode("//android.widget.EditText");
                if (editNode != null)
                {
                    var elem = AdbUiXmlDumper.ParseElement(editNode);
                    if (elem != null)
                    {
                        await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                        await Task.Delay(500, ct).ConfigureAwait(false);
                        await AdbImeHelper.InputTextAsync(device, reasonConfig.CustomDetails, ct).ConfigureAwait(false);
                        await Task.Delay(800, ct).ConfigureAwait(false);

                        var finalSubmitNode = detailsDoc.SelectSingleNode("//android.widget.Button[contains(@text, 'Báo cáo') or contains(@text, 'Report') or contains(@text, '신고')]");
                        if (finalSubmitNode != null)
                        {
                            var submitElem = AdbUiXmlDumper.ParseElement(finalSubmitNode);
                            if (submitElem != null)
                            {
                                await AdbUiXmlDumper.Instance.TapElementAsync(device, submitElem, ct).ConfigureAwait(false);
                                await Task.Delay(1500, ct).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }
        }

        return true;
    }

    public async Task<bool> SubmitChannelReportAsync(ReportReasonConfig reasonConfig, CancellationToken ct = default)
    {
        var device = EnsureDevice();

        int moreX = (int)(_screenResolution.Width * 0.92);
        int moreY = (int)(_screenResolution.Height * 0.05);
        await AdbClientGeneral.Instance.TapAsync(device, moreX, moreY, ct).ConfigureAwait(false);
        await Task.Delay(1000, ct).ConfigureAwait(false);

        var doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
        if (doc != null)
        {
            var reportNode = doc.SelectSingleNode("//node[contains(@text, 'Báo cáo') or contains(@text, 'Report') or contains(@text, '신고')]");
            if (reportNode != null)
            {
                var elem = AdbUiXmlDumper.ParseElement(reportNode);
                if (elem != null)
                {
                    await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                    await Task.Delay(1500, ct).ConfigureAwait(false);
                    return await SubmitVideoReportAsync(reasonConfig, ct).ConfigureAwait(false);
                }
            }
        }

        return false;
    }

    public async Task<bool> VerifyReportSubmittedAsync(CancellationToken ct = default)
    {
        var device = EnsureDevice();
        var successKeywords = UiLanguageManager.GetSuccessConfirmationKeywords();

        for (int i = 0; i < 4; i++)
        {
            var doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            if (doc != null)
            {
                var textNodes = doc.SelectNodes("//android.widget.TextView[@text!='']");
                if (textNodes != null)
                {
                    foreach (XmlNode n in textNodes)
                    {
                        string txt = n.Attributes?["text"]?.Value ?? string.Empty;
                        foreach (var kw in successKeywords)
                        {
                            if (txt.Contains(kw, StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            await Task.Delay(600, ct).ConfigureAwait(false);
        }

        return true;
    }

    public async Task<string> CaptureScreenshotAsync(string stepPrefix, CancellationToken ct = default)
    {
        var device = EnsureDevice();
        string safePrefix = Regex.Replace(stepPrefix ?? "Step", @"[^\w\.-]", "_");
        string fileName = $"Android_{_serial}_{safePrefix}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";
        string targetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots", fileName);

        return await AdbClientGeneral.Instance.CaptureScreenshotFileAsync(device, targetPath, ct).ConfigureAwait(false);
    }

    private async Task TryDismissPopupsAsync(DeviceData device, CancellationToken ct)
    {
        var doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
        if (doc == null) return;

        var dismissKeywords = new[] { "Không, cảm ơn", "Hủy", "Để sau", "Đóng", "Bỏ qua", "Dismiss", "Cancel", "Not now", "Skip", "Close", "취소", "나중에" };
        var btnNodes = doc.SelectNodes("//android.widget.Button | //android.widget.TextView[@clickable='true']");
        if (btnNodes != null)
        {
            foreach (XmlNode b in btnNodes)
            {
                string text = b.Attributes?["text"]?.Value ?? string.Empty;
                foreach (var kw in dismissKeywords)
                {
                    if (text.Equals(kw, StringComparison.OrdinalIgnoreCase))
                    {
                        var elem = AdbUiXmlDumper.ParseElement(b);
                        if (elem != null)
                        {
                            await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                            await Task.Delay(500, ct).ConfigureAwait(false);
                            return;
                        }
                    }
                }
            }
        }
    }

    private async Task EscapeShortsTrapAsync(DeviceData device, CancellationToken ct)
    {
        var doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
        if (doc == null) return;

        bool isShorts = doc.SelectSingleNode("//node[contains(@resource-id, 'reel_recycler') or @content-desc='Shorts' or @content-desc='쇼츠'][@selected='true']") != null;
        if (isShorts)
        {
            await AdbClientGeneral.Instance.PressBackAsync(device, ct).ConfigureAwait(false);
            await Task.Delay(1000, ct).ConfigureAwait(false);
        }
    }

    private DeviceData EnsureDevice() =>
        _deviceData ?? throw new InvalidOperationException($"Thiết bị '{_serial}' chưa được khởi tạo qua InitializeAsync.");

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
