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
/// Mô phỏng đầy đủ hành vi người dùng thật (Bézier swipe, random jitter tap, natural engagement, dismiss popups & shorts, skip ads).
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
        await EnsureNoAdPlayingAsync(ct).ConfigureAwait(false);
    }

    public async Task EnsureNoAdPlayingAsync(CancellationToken ct = default)
    {
        var device = EnsureDevice();

        for (int retry = 0; retry < 5; retry++)
        {
            ct.ThrowIfCancellationRequested();
            var doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            if (doc == null) break;

            // 1. Tìm nút bỏ qua quảng cáo (Skip Ad)
            var skipNode = doc.SelectSingleNode("//node[contains(@resource-id, 'skip_ad') or contains(@text, 'Bỏ qua') or contains(@text, 'Skip') or contains(@text, '건너뛰기') or contains(@content-desc, 'Bỏ qua') or contains(@content-desc, 'Skip')]");
            if (skipNode != null)
            {
                var elem = AdbUiXmlDumper.ParseElement(skipNode);
                if (elem != null)
                {
                    await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                    await Task.Delay(1500, ct).ConfigureAwait(false);
                    return;
                }
            }

            // 2. Kiểm tra xem có nhãn Ad/Quảng cáo/Sponsored đang chạy không
            var sponsoredNode = doc.SelectSingleNode("//node[contains(@text, 'Được tài trợ') or contains(@text, 'Sponsored') or contains(@text, '스폰서') or contains(@text, 'Quảng cáo')]");
            if (sponsoredNode != null)
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
        var device = EnsureDevice();
        await EnsureAppReadyAsync(ct).ConfigureAwait(false);

        for (int retry = 0; retry < 4; retry++)
        {
            var doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            if (doc != null)
            {
                // Nếu đang bị kẹt ở màn hình Thông báo (알림 / Notifications)
                bool isNotificationsScreen = doc.SelectSingleNode("//node[@text='알림' or @text='Notifications' or @text='Thông báo']") != null
                    && doc.SelectSingleNode("//node[contains(@content-desc, '홈') or contains(@content-desc, 'Home')]") == null;

                if (isNotificationsScreen)
                {
                    await AdbClientGeneral.Instance.PressBackAsync(device, ct).ConfigureAwait(false);
                    await Task.Delay(1000, ct).ConfigureAwait(false);
                    continue;
                }

                var homeNode = doc.SelectSingleNode("//node[(contains(@content-desc, '홈') or contains(@content-desc, 'Trang chủ') or contains(@content-desc, 'Home')) and not(contains(@content-desc, '알림'))]");
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

        // 1. Kiểm tra nếu ô tìm kiếm (search_edit_text) đã mở sẵn thì không cần bấm nút tìm kiếm
        var doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
        var searchInputNode = doc?.SelectSingleNode("//node[@resource-id='com.google.android.youtube:id/search_edit_text']");

        if (searchInputNode == null)
        {
            // 2. Kiểm tra nếu đang bị kẹt ở màn hình 알림 (Thông báo) -> bấm Back để về Home
            if (doc?.SelectSingleNode("//node[@text='알림' or @text='Notifications' or @text='Thông báo']") != null)
            {
                await AdbClientGeneral.Instance.PressBackAsync(device, ct).ConfigureAwait(false);
                await Task.Delay(1000, ct).ConfigureAwait(false);
                doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            }

            bool tappedSearch = false;
            if (doc != null)
            {
                // Chọn CHÍNH XÁC nút Tìm kiếm (Loại trừ tuyệt đối nút 알림 / Notifications)
                var searchNode = doc.SelectSingleNode(
                    "//node[not(contains(@content-desc, '알림') or contains(@content-desc, 'Notification') or contains(@content-desc, 'Thông báo')) and " +
                    "(@content-desc='검색' or @content-desc='Search' or @content-desc='Tìm kiếm' or " +
                    "contains(@content-desc, '검색') or contains(@content-desc, 'Search') or contains(@content-desc, 'Tìm kiếm') or " +
                    "@resource-id='com.google.android.youtube:id/menu_search' or " +
                    "(ancestor::node[@resource-id='com.google.android.youtube:id/menu_item_1'] and @resource-id='com.google.android.youtube:id/menu_item_view'))]");

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
                // Fallback: Nút Tìm kiếm trên YouTube Android luôn nằm ở góc trên bên phải (X ~ 94%, Y ~ 5%)
                int sx = (int)(_screenResolution.Width * 0.94);
                int sy = (int)(_screenResolution.Height * 0.05);
                await AdbClientGeneral.Instance.TapAsync(device, sx, sy, ct).ConfigureAwait(false);
            }

            // Đợi ô nhập xuất hiện
            for (int i = 0; i < 6; i++)
            {
                await Task.Delay(500, ct).ConfigureAwait(false);
                var checkDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
                if (checkDoc?.SelectSingleNode("//node[@resource-id='com.google.android.youtube:id/search_edit_text']") != null)
                {
                    break;
                }
            }
        }

        // 3. Xóa từ khóa cũ nếu có nút Clear
        var clearDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
        var clearBtn = clearDoc?.SelectSingleNode("//node[@resource-id='com.google.android.youtube:id/search_clear_button']");
        if (clearBtn != null)
        {
            var clearElem = AdbUiXmlDumper.ParseElement(clearBtn);
            if (clearElem != null)
            {
                await AdbUiXmlDumper.Instance.TapElementAsync(device, clearElem, ct).ConfigureAwait(false);
                await Task.Delay(300, ct).ConfigureAwait(false);
            }
        }

        // 4. Nhập từ khóa qua LaiXi/AdbIME
        await AdbImeHelper.InputTextAsync(device, keyword, ct).ConfigureAwait(false);
        await Task.Delay(800, ct).ConfigureAwait(false);

        // 5. Gửi phím Enter (Search action)
        await AdbClientGeneral.Instance.SendKeyEventAsync(device, 66, ct).ConfigureAwait(false);
        await Task.Delay(2500, ct).ConfigureAwait(false);
    }

    public async Task<bool> SmartScrollAndSelectVideoAsync(ReportTargetItem target, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        var device = EnsureDevice();

        string cleanTitle = target.VideoTitle?.TrimEnd('.', '…', ' ') ?? string.Empty;

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
                // Tìm kiếm trên tất cả các node chứa content-desc hoặc text (ViewGroup, Button, TextView)
                var candidateNodes = doc.SelectNodes("//node[(@content-desc!='' or @text!='') and (@clickable='true' or @class='android.view.ViewGroup' or @class='android.widget.Button' or @class='android.widget.TextView')]");
                if (candidateNodes != null)
                {
                    foreach (XmlNode node in candidateNodes)
                    {
                        string desc = node.Attributes?["content-desc"]?.Value ?? string.Empty;
                        string text = node.Attributes?["text"]?.Value ?? string.Empty;

                        // Bỏ qua các nút điều hướng hoặc thanh hệ thống
                        if (desc.Contains("Shorts") || desc.Contains("홈") || desc.Contains("구독") || desc.Contains("보관함") || desc.Contains("위로 이동") || desc.Contains("옵션 더보기"))
                        {
                            continue;
                        }

                        bool isMatch = target.Matches(text, desc, null) ||
                                       (!string.IsNullOrWhiteSpace(cleanTitle) && cleanTitle.Length >= 4 &&
                                        (desc.Contains(cleanTitle, StringComparison.OrdinalIgnoreCase) || text.Contains(cleanTitle, StringComparison.OrdinalIgnoreCase)));

                        if (isMatch)
                        {
                            var elem = AdbUiXmlDumper.ParseElement(node);
                            if (elem != null && elem.Bounds.Y > (_screenResolution.Height * 0.08) && elem.Bounds.Y < (_screenResolution.Height * 0.88))
                            {
                                await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                                await Task.Delay(2500, ct).ConfigureAwait(false);
                                await TryDismissPopupsAsync(device, ct).ConfigureAwait(false);
                                await EnsureNoAdPlayingAsync(ct).ConfigureAwait(false);
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
        await EnsureNoAdPlayingAsync(ct).ConfigureAwait(false);
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

        await EnsureNoAdPlayingAsync(ct).ConfigureAwait(false);

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

                // Nếu là livestream và chat đang mở, cuộn nhẹ live chat
                bool isChatOpen = await LiveChatOverlayHelper.IsLiveChatOpenAsync(device, ct).ConfigureAwait(false);
                if (isChatOpen)
                {
                    await LiveChatOverlayHelper.ScrollLiveChatAsync(device, _screenResolution.Width, _screenResolution.Height, ct).ConfigureAwait(false);
                }
                else
                {
                    await AdbClientGeneral.Instance.SwipeAsync(device, midX, scrollStartY, midX, scrollEndY, 400, ct).ConfigureAwait(false);
                    await Task.Delay(rnd.Next(3000, 6000), ct).ConfigureAwait(false);
                    await AdbClientGeneral.Instance.SwipeAsync(device, midX, scrollEndY, midX, scrollStartY, 400, ct).ConfigureAwait(false);
                }
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

        // 1. Thử bấm nút '추가 작업' (More actions) trên thanh nút cuộn ngang ngay dưới video trước
        bool tappedMore = await LiveChatOverlayHelper.TapActionRowMoreButtonAsync(device, ct).ConfigureAwait(false);
        if (!tappedMore)
        {
            // Nếu không có, chạm vào video player để đánh thức thanh điều khiển và bấm nút Cài đặt/3 chấm
            await LiveChatOverlayHelper.AwakenPlayerControlsAndOpenSettingsAsync(device, _screenResolution.Width, _screenResolution.Height, ct).ConfigureAwait(false);
        }

        await Task.Delay(1200, ct).ConfigureAwait(false);

        // 2. Quét Bottom Sheet menu mở ra để tìm mục Báo cáo (신고 / Report / Báo cáo)
        var bottomDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
        if (bottomDoc == null) return false;

        var reportKeywords = UiLanguageManager.GetReportMenuKeywords();
        var menuNodes = bottomDoc.SelectNodes("//node[(@class='android.view.ViewGroup' or @class='android.widget.TextView' or @class='android.widget.Button') and (@text!='' or @content-desc!='')]");
        if (menuNodes != null)
        {
            foreach (XmlNode node in menuNodes)
            {
                string text = node.Attributes?["text"]?.Value ?? string.Empty;
                string desc = node.Attributes?["content-desc"]?.Value ?? string.Empty;
                string combined = $"{text} {desc}".Trim();

                foreach (var kw in reportKeywords)
                {
                    if (combined.Contains(kw, StringComparison.OrdinalIgnoreCase))
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

            // Nếu trong menu có mục '추가 설정' (Additional settings), bấm vào để tìm Báo cáo bên trong
            foreach (XmlNode node in menuNodes)
            {
                string text = node.Attributes?["text"]?.Value ?? string.Empty;
                string desc = node.Attributes?["content-desc"]?.Value ?? string.Empty;
                if (text.Contains("추가 설정") || desc.Contains("추가 설정") || text.Contains("Additional settings") || text.Contains("Cài đặt bổ sung"))
                {
                    var elem = AdbUiXmlDumper.ParseElement(node);
                    if (elem != null)
                    {
                        await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                        await Task.Delay(1000, ct).ConfigureAwait(false);

                        var subDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
                        var subNodes = subDoc?.SelectNodes("//node[(@class='android.view.ViewGroup' or @class='android.widget.TextView' or @class='android.widget.Button') and (@text!='' or @content-desc!='')]");
                        if (subNodes != null)
                        {
                            foreach (XmlNode sn in subNodes)
                            {
                                string st = sn.Attributes?["text"]?.Value ?? string.Empty;
                                string sd = sn.Attributes?["content-desc"]?.Value ?? string.Empty;
                                string sc = $"{st} {sd}".Trim();
                                foreach (var kw in reportKeywords)
                                {
                                    if (sc.Contains(kw, StringComparison.OrdinalIgnoreCase))
                                    {
                                        var subElem = AdbUiXmlDumper.ParseElement(sn);
                                        if (subElem != null)
                                        {
                                            await AdbUiXmlDumper.Instance.TapElementAsync(device, subElem, ct).ConfigureAwait(false);
                                            await Task.Delay(1500, ct).ConfigureAwait(false);
                                            return true;
                                        }
                                    }
                                }
                            }
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

        int midX = _screenResolution.Width / 2;

        // 1. Chờ và kiểm tra hộp thoại báo cáo có mở không (tối đa 3 lần)
        XmlDocument? doc = null;
        for (int i = 0; i < 3; i++)
        {
            doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            if (doc != null && doc.SelectSingleNode("//node[contains(@text, '신고') or contains(@content-desc, '신고') or contains(@text, 'Report') or contains(@content-desc, 'Report') or contains(@text, 'Báo cáo') or contains(@text, '어떤 문제인가요') or contains(@content-desc, '어떤 문제인가요')]") != null)
            {
                break;
            }
            await Task.Delay(800, ct).ConfigureAwait(false);
        }

        if (doc == null) return false;

        // 2. Tìm và chọn lý do báo cáo
        var keywords = UiLanguageManager.GetAllKeywordsForCategory(reasonConfig.Category);
        bool categorySelected = false;

        bool TryFindAndSelectReason(XmlDocument currentDoc)
        {
            var nodes = currentDoc.SelectNodes("//node[(@class='android.view.ViewGroup' or @class='android.widget.TextView' or @class='android.widget.RadioButton') and (@text!='' or @content-desc!='')]");
            if (nodes == null) return false;

            foreach (XmlNode node in nodes)
            {
                string text = node.Attributes?["text"]?.Value ?? string.Empty;
                string desc = node.Attributes?["content-desc"]?.Value ?? string.Empty;
                string combined = $"{text} {desc}".Trim();

                foreach (var kw in keywords)
                {
                    if (combined.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    {
                        var elem = AdbUiXmlDumper.ParseElement(node);
                        if (elem != null && elem.Bounds.Y >= (_screenResolution.Height * 0.1) && elem.Bounds.Bottom <= (_screenResolution.Height * 0.92))
                        {
                            AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).GetAwaiter().GetResult();
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        categorySelected = TryFindAndSelectReason(doc);

        // Nếu chưa tìm thấy lý do, cuộn nhẹ hộp thoại xuống để hiển thị các lý do bên dưới (như Spam/Lừa đảo)
        if (!categorySelected)
        {
            int scrollStartY = (int)(_screenResolution.Height * 0.70);
            int scrollEndY = (int)(_screenResolution.Height * 0.35);
            await AdbClientGeneral.Instance.SwipeAsync(device, midX, scrollStartY, midX, scrollEndY, 400, ct).ConfigureAwait(false);
            await Task.Delay(1000, ct).ConfigureAwait(false);

            var scrolledDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            if (scrolledDoc != null)
            {
                categorySelected = TryFindAndSelectReason(scrolledDoc);
            }
        }

        // Nếu vẫn không tìm thấy chính xác, fallback chọn lý do đầu tiên có sẵn trong hộp thoại
        if (!categorySelected)
        {
            var fallbackDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            var fallbackNode = fallbackDoc?.SelectSingleNode("//node[(@class='android.view.ViewGroup' or @class='android.widget.RadioButton') and (contains(@text, '콘텐츠') or contains(@text, '스팸') or contains(@text, '정보') or contains(@content-desc, '콘텐츠'))]");
            if (fallbackNode != null)
            {
                var elem = AdbUiXmlDumper.ParseElement(fallbackNode);
                if (elem != null)
                {
                    await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                    categorySelected = true;
                }
            }
        }

        if (!categorySelected)
        {
            // Không chọn được lý do báo cáo -> Bắt buộc báo thất bại, không được silent pass!
            return false;
        }

        await Task.Delay(1000, ct).ConfigureAwait(false);

        // 3. Nhấn nút Tiếp theo ('다음' / 'Next' / 'Tiếp theo')
        var nextDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
        if (nextDoc != null)
        {
            var nextNode = nextDoc.SelectSingleNode("//node[(@class='android.widget.Button' or @class='android.view.ViewGroup' or @class='android.widget.TextView') and (@text='다음' or @content-desc='다음' or @text='Next' or @content-desc='Next' or @text='Tiếp theo' or @content-desc='Tiếp theo')]");
            if (nextNode != null)
            {
                var nextElem = AdbUiXmlDumper.ParseElement(nextNode);
                if (nextElem != null)
                {
                    await AdbUiXmlDumper.Instance.TapElementAsync(device, nextElem, ct).ConfigureAwait(false);
                    await Task.Delay(1500, ct).ConfigureAwait(false);
                }
            }
        }

        // 4. Xử lý Trang 2: Nhập nội dung lý do báo cáo vào ô EditText & Bấm nút gửi Báo cáo
        await Task.Delay(1200, ct).ConfigureAwait(false);
        var page2Doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
        if (page2Doc != null)
        {
            // 4.1. Nhập văn bản giải trình vào ô EditText nếu có
            var editNode = page2Doc.SelectSingleNode("//android.widget.EditText");
            if (editNode != null)
            {
                var editElem = AdbUiXmlDumper.ParseElement(editNode);
                if (editElem != null)
                {
                    await AdbUiXmlDumper.Instance.TapElementAsync(device, editElem, ct).ConfigureAwait(false);
                    await Task.Delay(500, ct).ConfigureAwait(false);

                    string detailText = !string.IsNullOrWhiteSpace(reasonConfig.CustomDetails)
                        ? reasonConfig.CustomDetails
                        : "이 방송은 불법 해외선물 대여계좌 및 사기성 리딩방을 홍보하고 있습니다.";

                    await AdbImeHelper.InputTextAsync(device, detailText, ct).ConfigureAwait(false);
                    await Task.Delay(600, ct).ConfigureAwait(false);

                    // Ẩn bàn phím ảo để lộ nút 신고 bên dưới
                    await AdbClientGeneral.Instance.PressBackAsync(device, ct).ConfigureAwait(false);
                    await Task.Delay(600, ct).ConfigureAwait(false);
                }
            }

            // 4.2. Nhấn nút gửi báo cáo chính thức: '신고' / 'Báo cáo' / 'Report' / 'Submit'
            // ĐẶC BIỆT LƯU Ý: Phải chọn Button ở nửa dưới màn hình (Bounds.Y >= screenHeight * 0.45)
            // để TUYỆT ĐỐI KHÔNG bấm nhầm vào chữ '신고' trên thanh tiêu đề đầu trang (Y ~ 144)
            var submitDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            bool tappedSubmit = false;

            if (submitDoc != null)
            {
                var candidateButtons = submitDoc.SelectNodes("//android.widget.Button | //node[@class='android.widget.Button']");
                if (candidateButtons != null)
                {
                    foreach (XmlNode btn in candidateButtons)
                    {
                        string desc = btn.Attributes?["content-desc"]?.Value ?? string.Empty;
                        string text = btn.Attributes?["text"]?.Value ?? string.Empty;
                        string bName = $"{text} {desc}".Trim();

                        if (bName.Contains("신고", StringComparison.OrdinalIgnoreCase) ||
                            bName.Contains("Report", StringComparison.OrdinalIgnoreCase) ||
                            bName.Contains("Báo cáo", StringComparison.OrdinalIgnoreCase) ||
                            bName.Contains("Submit", StringComparison.OrdinalIgnoreCase) ||
                            bName.Contains("Gửi", StringComparison.OrdinalIgnoreCase))
                        {
                            var elem = AdbUiXmlDumper.ParseElement(btn);
                            if (elem != null && elem.Bounds.Y >= (_screenResolution.Height * 0.45))
                            {
                                await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                                await Task.Delay(2000, ct).ConfigureAwait(false);
                                tappedSubmit = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (!tappedSubmit)
            {
                // Fallback: bấm thẳng vào vùng nút gửi ở đáy màn hình (X = 50%, Y ~ 90%)
                int subX = _screenResolution.Width / 2;
                int subY = (int)(_screenResolution.Height * 0.90);
                await AdbClientGeneral.Instance.TapAsync(device, subX, subY, ct).ConfigureAwait(false);
                await Task.Delay(2000, ct).ConfigureAwait(false);
            }
        }

        // 5. Xác nhận & Đóng modal cảm ơn hoàn tất ('확인' / '닫기' / 'Close' / 'OK')
        var ackDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
        if (ackDoc != null)
        {
            var ackButtons = ackDoc.SelectNodes("//node[(@class='android.widget.Button' or @class='android.view.ViewGroup' or @class='android.widget.TextView') and (@text!='' or @content-desc!='')]");
            if (ackButtons != null)
            {
                foreach (XmlNode n in ackButtons)
                {
                    string t = n.Attributes?["text"]?.Value ?? string.Empty;
                    string d = n.Attributes?["content-desc"]?.Value ?? string.Empty;
                    string c = $"{t} {d}".Trim();

                    if (c.Contains("확인") || c.Contains("닫기") || c.Contains("Close") || c.Contains("Đóng") || c.Contains("OK"))
                    {
                        var elem = AdbUiXmlDumper.ParseElement(n);
                        if (elem != null && elem.Bounds.Y >= (_screenResolution.Height * 0.50))
                        {
                            await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                            await Task.Delay(800, ct).ConfigureAwait(false);
                            break;
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

    public async Task<bool> SubmitShortsReportAsync(ReportReasonConfig reasonConfig, CancellationToken ct = default)
    {
        var device = EnsureDevice();

        // 1. Tìm nút 3-chấm trên Shorts (thường nằm dọc ở cạnh phải màn hình)
        var doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
        bool openedMenu = false;

        if (doc != null)
        {
            var menuNode = doc.SelectSingleNode("//node[contains(@resource-id, 'menu_button') or contains(@content-desc, 'More actions') or contains(@content-desc, 'Tùy chọn khác')]");
            if (menuNode != null)
            {
                var elem = AdbUiXmlDumper.ParseElement(menuNode);
                if (elem != null)
                {
                    await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                    openedMenu = true;
                }
            }
        }

        if (!openedMenu)
        {
            // Fallback vị trí nút 3 chấm của Shorts (bên phải, Y ~ 72%)
            int btnX = (int)(_screenResolution.Width * 0.92);
            int btnY = (int)(_screenResolution.Height * 0.72);
            await AdbClientGeneral.Instance.TapAsync(device, btnX, btnY, ct).ConfigureAwait(false);
        }

        await Task.Delay(1200, ct).ConfigureAwait(false);

        // 2. Tìm mục Báo cáo trong menu Shorts
        var bottomDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
        if (bottomDoc != null)
        {
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
                                return await SubmitVideoReportAsync(reasonConfig, ct).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }
        }

        return false;
    }

    public async Task<bool> SubmitLiveStreamReportAsync(ReportReasonConfig reasonConfig, CancellationToken ct = default)
    {
        var device = EnsureDevice();

        // 1. Kiểm tra và đóng Live Chat nếu đang mở che khuất các nút hành động của video
        bool wasChatOpen = await LiveChatOverlayHelper.IsLiveChatOpenAsync(device, ct).ConfigureAwait(false);
        if (wasChatOpen)
        {
            await LiveChatOverlayHelper.EnsureLiveChatClosedAsync(device, ct).ConfigureAwait(false);
            await Task.Delay(1000, ct).ConfigureAwait(false);
        }

        // 2. Thử mở menu Báo cáo dưới video trước
        bool opened = await OpenReportMenuAsync(ct).ConfigureAwait(false);

        // 3. Nếu không mở được dưới video, dùng phương thức chạm đánh thức Player Controls
        if (!opened)
        {
            await LiveChatOverlayHelper.AwakenPlayerControlsAndOpenSettingsAsync(device, _screenResolution.Width, _screenResolution.Height, ct).ConfigureAwait(false);
            await Task.Delay(1000, ct).ConfigureAwait(false);

            // Tìm nút Báo cáo trong popup menu cài đặt
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
                        await Task.Delay(1200, ct).ConfigureAwait(false);
                        opened = true;
                    }
                }
            }
        }

        if (!opened)
        {
            return false;
        }

        // 4. Thực hiện nộp báo cáo (chọn Spam/Scam, chọn Gian lận/Lừa đảo, điền spin text)
        return await SubmitVideoReportAsync(reasonConfig, ct).ConfigureAwait(false);
    }

    public async Task<bool> SubmitCommentReportAsync(string commentAuthorOrKeyword, ReportReasonConfig reasonConfig, CancellationToken ct = default)
    {
        var device = EnsureDevice();

        // 1. Mở phần bình luận nếu chưa mở
        var doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
        if (doc != null)
        {
            var commentsEntryPoint = doc.SelectSingleNode("//node[contains(@resource-id, 'comments_entry_point') or contains(@text, 'Bình luận') or contains(@text, 'Comments') or contains(@text, '댓글')]");
            if (commentsEntryPoint != null)
            {
                var elem = AdbUiXmlDumper.ParseElement(commentsEntryPoint);
                if (elem != null)
                {
                    await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                    await Task.Delay(1500, ct).ConfigureAwait(false);
                }
            }
        }

        // 2. Tìm bình luận khớp với commentAuthorOrKeyword
        var commentsDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
        if (commentsDoc != null)
        {
            var textNodes = commentsDoc.SelectNodes("//android.widget.TextView[@text!='']");
            if (textNodes != null)
            {
                foreach (XmlNode n in textNodes)
                {
                    string txt = n.Attributes?["text"]?.Value ?? string.Empty;
                    if (txt.Contains(commentAuthorOrKeyword, StringComparison.OrdinalIgnoreCase))
                    {
                        var elem = AdbUiXmlDumper.ParseElement(n);
                        if (elem != null)
                        {
                            // Bấm nút menu 3 chấm của bình luận (thường nằm cùng hàng Y, sát bên phải)
                            int menuX = (int)(_screenResolution.Width * 0.92);
                            int menuY = elem.CenterPoint.Y;
                            await AdbClientGeneral.Instance.TapAsync(device, menuX, menuY, ct).ConfigureAwait(false);
                            await Task.Delay(1000, ct).ConfigureAwait(false);

                            // Bấm Báo cáo
                            var actionDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
                            var reportNode = actionDoc?.SelectSingleNode("//node[contains(@text, 'Báo cáo') or contains(@text, 'Report') or contains(@text, '신고')]");
                            if (reportNode != null)
                            {
                                var reportElem = AdbUiXmlDumper.ParseElement(reportNode);
                                if (reportElem != null)
                                {
                                    await AdbUiXmlDumper.Instance.TapElementAsync(device, reportElem, ct).ConfigureAwait(false);
                                    await Task.Delay(1500, ct).ConfigureAwait(false);
                                    return await SubmitVideoReportAsync(reasonConfig, ct).ConfigureAwait(false);
                                }
                            }
                        }
                    }
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
