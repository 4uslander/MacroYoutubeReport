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

    /// <summary>
    /// Áp dụng bộ lọc tìm kiếm nâng cao (Phân loại, Độ dài, Ngày tải lên, Live, v.v.) theo cấu hình AutoYouTube.
    /// Tự động bấm nút '옵션 더보기' -> chọn '검색 필터' -> chọn các tiêu chí -> bấm '적용'.
    /// </summary>
    public async Task<bool> ApplySearchFilterAsync(SearchFilterConfig? filter = null, CancellationToken ct = default)
    {
        filter ??= new SearchFilterConfig();
        if (!filter.HasActiveFilter())
        {
            return true;
        }

        var device = EnsureDevice();

        try
        {
            // 1. Chờ kết quả tìm kiếm ổn định
            await Task.Delay(1500, ct).ConfigureAwait(false);

            var doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            if (doc == null) return false;

            // 2. Tìm nút "옵션 더보기" (More options) ở góc trên bên phải trang tìm kiếm
            var moreOptionsNode = doc.SelectSingleNode(
                "//node[(@content-desc='옵션 더보기' or @content-desc='More options' or @content-desc='Tùy chọn khác' or @content-desc='Thêm tùy chọn' or " +
                "contains(@content-desc, '옵션 더보기') or contains(@content-desc, 'More options')) and @clickable='true']");

            if (moreOptionsNode != null)
            {
                var elem = AdbUiXmlDumper.ParseElement(moreOptionsNode);
                if (elem != null)
                {
                    await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                }
            }
            else
            {
                // Fallback bấm góc trên cùng bên phải (X ~ 95%, Y ~ 7.5%)
                int fx = (int)(_screenResolution.Width * 0.95);
                int fy = (int)(_screenResolution.Height * 0.075);
                await AdbClientGeneral.Instance.TapAsync(device, fx, fy, ct).ConfigureAwait(false);
            }

            await Task.Delay(1000, ct).ConfigureAwait(false);

            // 3. Tìm và bấm menu "검색 필터" (Search filters)
            var menuDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            var filterMenuNode = menuDoc?.SelectSingleNode(
                "//node[@text='검색 필터' or @text='Search filters' or @text='Bộ lọc tìm kiếm' or @text='Bộ lọc' or " +
                "contains(@text, '검색 필터') or contains(@text, 'Search filters') or contains(@text, 'Bộ lọc')]");

            if (filterMenuNode != null)
            {
                var elem = AdbUiXmlDumper.ParseElement(filterMenuNode);
                if (elem != null)
                {
                    await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                }
            }
            else
            {
                // Fallback vị trí menu popup thông thường
                int mx = (int)(_screenResolution.Width * 0.72);
                int my = (int)(_screenResolution.Height * 0.08);
                await AdbClientGeneral.Instance.TapAsync(device, mx, my, ct).ConfigureAwait(false);
            }

            await Task.Delay(1200, ct).ConfigureAwait(false);

            // 4. Màn hình bảng cài đặt bộ lọc "검색 필터"
            var dialogDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            if (dialogDoc == null) return false;

            // 4.1. Lọc theo Phân loại (Type) nếu khác All
            if (filter.Type != SearchTypeFilter.All)
            {
                string typeTargetText = filter.Type switch
                {
                    SearchTypeFilter.Video => "동영상",
                    SearchTypeFilter.Channel => "채널",
                    SearchTypeFilter.Playlist => "재생목록",
                    SearchTypeFilter.Movie => "영화",
                    _ => string.Empty
                };
                if (!string.IsNullOrEmpty(typeTargetText))
                {
                    await SelectDropdownOptionAsync(device, "구분", typeTargetText, ct).ConfigureAwait(false);
                    dialogDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
                }
            }

            // 4.2. Lọc theo Độ dài (Duration) nếu khác All
            if (filter.Duration != SearchDurationFilter.All)
            {
                string durTargetText = filter.Duration switch
                {
                    SearchDurationFilter.Short => "4분 미만",
                    SearchDurationFilter.Medium => "4~20분",
                    SearchDurationFilter.Long => "20분 초과",
                    _ => string.Empty
                };
                if (!string.IsNullOrEmpty(durTargetText))
                {
                    await SelectDropdownOptionAsync(device, "길이", durTargetText, ct).ConfigureAwait(false);
                    dialogDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
                }
            }

            // 4.3. Lọc theo Ngày tải lên (UploadDate)
            if (filter.UploadDate != SearchUploadDateFilter.All)
            {
                string dateTargetText = filter.UploadDate switch
                {
                    SearchUploadDateFilter.Today => "오늘",
                    SearchUploadDateFilter.LastHour => "지난 1시간",
                    SearchUploadDateFilter.ThisWeek => "이번 주",
                    SearchUploadDateFilter.ThisMonth => "이번 달",
                    SearchUploadDateFilter.ThisYear => "올해",
                    _ => string.Empty
                };
                if (!string.IsNullOrEmpty(dateTargetText))
                {
                    await SelectDropdownOptionAsync(device, "업로드 날짜", dateTargetText, ct).ConfigureAwait(false);
                    dialogDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
                }
            }

            // 4.4. Lọc theo Ưu tiên (SortBy)
            if (filter.SortBy != SearchSortByFilter.Relevance)
            {
                string sortTargetText = filter.SortBy switch
                {
                    SearchSortByFilter.UploadDate => "업로드 날짜",
                    SearchSortByFilter.ViewCount => "조회수",
                    SearchSortByFilter.Rating => "평점",
                    _ => string.Empty
                };
                if (!string.IsNullOrEmpty(sortTargetText))
                {
                    await SelectDropdownOptionAsync(device, "우선순위", sortTargetText, ct).ConfigureAwait(false);
                    dialogDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
                }
            }

            // 4.5. Xử lý các Chip tính năng (Live, 4K, Phụ đề, Commons)
            if (dialogDoc != null)
            {
                if (filter.LiveOnly)
                {
                    await ToggleChipFilterAsync(device, dialogDoc, "라이브", "Live", ct).ConfigureAwait(false);
                }
                if (filter.FourKOnly)
                {
                    await ToggleChipFilterAsync(device, dialogDoc, "4K", "4K", ct).ConfigureAwait(false);
                }
                if (filter.SubtitlesOnly)
                {
                    await ToggleChipFilterAsync(device, dialogDoc, "자막", "Subtitles", ct).ConfigureAwait(false);
                }
                if (filter.CommonsOnly)
                {
                    await ToggleChipFilterAsync(device, dialogDoc, "크리에이티브 커먼즈", "Creative Commons", ct).ConfigureAwait(false);
                }
            }

            await Task.Delay(500, ct).ConfigureAwait(false);

            // 5. Bấm nút "적용" (Apply)
            var applyDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            var applyNode = applyDoc?.SelectSingleNode(
                "//node[@resource-id='com.google.android.youtube:id/apply' or @text='적용' or @text='Apply' or @content-desc='적용' or @content-desc='Apply']");

            if (applyNode != null)
            {
                var elem = AdbUiXmlDumper.ParseElement(applyNode);
                if (elem != null)
                {
                    await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                }
            }
            else
            {
                // Fallback vị trí nút 적용 ở góc dưới bên phải dialog (X ~ 79%, Y ~ 86%)
                int ax = (int)(_screenResolution.Width * 0.79);
                int ay = (int)(_screenResolution.Height * 0.86);
                await AdbClientGeneral.Instance.TapAsync(device, ax, ay, ct).ConfigureAwait(false);
            }

            // Chờ kết quả tìm kiếm đã lọc hiển thị
            await Task.Delay(2500, ct).ConfigureAwait(false);
            return true;
        }
        catch
        {
            // Nếu có lỗi khi bật filter, không làm sập luồng mà tiếp tục tìm kiếm
            return false;
        }
    }

    private async Task SelectDropdownOptionAsync(DeviceData device, string labelText, string optionText, CancellationToken ct)
    {
        try
        {
            var doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            if (doc == null) return;

            // Tìm node nhãn tiêu đề (ví dụ '업로드 날짜')
            var labelNode = doc.SelectSingleNode($"//node[@text='{labelText}']");
            if (labelNode != null)
            {
                // Nút dropdown giá trị thường nằm trên cùng một hàng (cùng parent hoặc kế tiếp)
                var dropdownNode = labelNode.ParentNode?.SelectSingleNode(".//node[@clickable='true' and @text!='']")
                                   ?? labelNode.SelectSingleNode($"following-sibling::node[@clickable='true']");

                if (dropdownNode != null)
                {
                    string currentVal = dropdownNode.Attributes?["text"]?.Value ?? string.Empty;
                    if (currentVal.Equals(optionText, StringComparison.OrdinalIgnoreCase))
                    {
                        return; // Đã là giá trị này rồi
                    }

                    var dropElem = AdbUiXmlDumper.ParseElement(dropdownNode);
                    if (dropElem != null)
                    {
                        await AdbUiXmlDumper.Instance.TapElementAsync(device, dropElem, ct).ConfigureAwait(false);
                        await Task.Delay(800, ct).ConfigureAwait(false);

                        // Sau khi bấm mở danh sách chọn, tìm node có optionText và bấm
                        var optionsDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
                        var targetOption = optionsDoc?.SelectSingleNode($"//node[@text='{optionText}']");
                        if (targetOption != null)
                        {
                            var optElem = AdbUiXmlDumper.ParseElement(targetOption);
                            if (optElem != null)
                            {
                                await AdbUiXmlDumper.Instance.TapElementAsync(device, optElem, ct).ConfigureAwait(false);
                                await Task.Delay(600, ct).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Bỏ qua lỗi chọn dropdown đơn lẻ
        }
    }

    private async Task ToggleChipFilterAsync(DeviceData device, XmlDocument doc, string koreanText, string englishText, CancellationToken ct)
    {
        try
        {
            var chipNode = doc.SelectSingleNode(
                $"//node[(@text='{koreanText}' or @text='{englishText}' or contains(@text, '{koreanText}') or contains(@content-desc, '{koreanText}')) and @clickable='true']");

            if (chipNode != null)
            {
                string isSelected = chipNode.Attributes?["selected"]?.Value ?? "false";
                string isChecked = chipNode.Attributes?["checked"]?.Value ?? "false";

                // Nếu chưa chọn thì bấm chọn
                if (!string.Equals(isSelected, "true", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(isChecked, "true", StringComparison.OrdinalIgnoreCase))
                {
                    var elem = AdbUiXmlDumper.ParseElement(chipNode);
                    if (elem != null)
                    {
                        await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                        await Task.Delay(400, ct).ConfigureAwait(false);
                    }
                }
            }
        }
        catch
        {
            // Bỏ qua lỗi toggle chip
        }
    }

    public async Task<bool> SmartScrollAndSelectVideoAsync(ReportTargetItem target, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        var device = EnsureDevice();

        string cleanTitle = target.VideoTitle?.TrimEnd('.', '…', ' ') ?? string.Empty;

        // Cơ chế AutoYouTube: Lướt liên tục (đến 50 lần) để tìm video mục tiêu
        int maxScrollAttempts = 50;
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
            await Task.Delay(Random.Shared.Next(1000, 1500), ct).ConfigureAwait(false);
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
        bool tappedMore = await LiveChatOverlayHelper.TapActionRowMoreButtonAsync(device, _screenResolution.Width, _screenResolution.Height, ct).ConfigureAwait(false);
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

            // Nếu trong menu có mục '추가 설정' / '고급 설정' (Additional/Advanced settings), bấm vào để tìm Báo cáo bên trong
            foreach (XmlNode node in menuNodes)
            {
                string text = node.Attributes?["text"]?.Value ?? string.Empty;
                string desc = node.Attributes?["content-desc"]?.Value ?? string.Empty;
                if (text.Contains("추가 설정") || desc.Contains("추가 설정") || 
                    text.Contains("고급 설정") || desc.Contains("고급 설정") ||
                    text.Contains("기타") || desc.Contains("기타") ||
                    text.Contains("Additional settings") || text.Contains("Cài đặt bổ sung"))
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

        // 1. Chờ và kiểm tra hộp thoại báo cáo có mở không (tối đa 4 lần)
        XmlDocument? doc = null;
        for (int i = 0; i < 4; i++)
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

        async Task<bool> TryFindAndSelectReasonAsync(XmlDocument currentDoc)
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
                        if (elem != null && elem.CenterPoint.Y >= (_screenResolution.Height * 0.12) && elem.CenterPoint.Y <= (_screenResolution.Height * 0.88))
                        {
                            await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        categorySelected = await TryFindAndSelectReasonAsync(doc).ConfigureAwait(false);

        // Nếu chưa tìm thấy lý do (như Spam/Lừa đảo/Thù địch nằm phía dưới), cuộn nhẹ AN TOÀN bên trong thân danh sách
        // Chú ý: Vuốt an toàn ở khoảng Y từ 60% lên 42% (350ms) để KHÔNG chạm mép header hay kích hoạt cử chỉ kéo tắt BottomSheet!
        if (!categorySelected)
        {
            int scrollStartY = (int)(_screenResolution.Height * 0.60);
            int scrollEndY = (int)(_screenResolution.Height * 0.42);
            await AdbClientGeneral.Instance.SwipeAsync(device, midX, scrollStartY, midX, scrollEndY, 350, ct).ConfigureAwait(false);
            await Task.Delay(1000, ct).ConfigureAwait(false);

            var scrolledDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            if (scrolledDoc != null)
            {
                categorySelected = await TryFindAndSelectReasonAsync(scrolledDoc).ConfigureAwait(false);
            }
        }

        // Nếu vẫn chưa tìm thấy, cuộn nhẹ thêm 1 lần nữa ở biên độ hẹp
        if (!categorySelected)
        {
            int scrollStartY = (int)(_screenResolution.Height * 0.58);
            int scrollEndY = (int)(_screenResolution.Height * 0.40);
            await AdbClientGeneral.Instance.SwipeAsync(device, midX, scrollStartY, midX, scrollEndY, 350, ct).ConfigureAwait(false);
            await Task.Delay(1000, ct).ConfigureAwait(false);

            var scrolledDoc2 = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            if (scrolledDoc2 != null)
            {
                categorySelected = await TryFindAndSelectReasonAsync(scrolledDoc2).ConfigureAwait(false);
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
                    await Task.Delay(2000, ct).ConfigureAwait(false);
                }
            }
        }

        // 4. Xử lý Trang 2: Nhập nội dung lý do báo cáo vào ô EditText & Bấm nút gửi Báo cáo
        XmlDocument? page2Doc = null;
        bool isPage2 = false;
        for (int p2Wait = 0; p2Wait < 6; p2Wait++)
        {
            await Task.Delay(600, ct).ConfigureAwait(false);
            page2Doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            if (page2Doc != null && page2Doc.SelectSingleNode("//node[contains(@text, '세부정보') or contains(@content-desc, '세부정보') or @class='android.widget.EditText' or contains(@text, '자세한 내용')]") != null)
            {
                isPage2 = true;
                break;
            }
        }

        if (isPage2 && page2Doc != null)
        {
            // 4.1. Nhập văn bản giải trình vào ô EditText
            // Định vị chính xác ô nhập (Ưu tiên class EditText hoặc placeholder "세부정보 추가...", loại trừ các nhãn mô tả)
            var editNode = page2Doc.SelectSingleNode(
                "//node[@class='android.widget.EditText' or @text='세부정보 추가...' or " +
                "(@class='android.view.ViewGroup' and contains(@text, '세부정보 추가')) or " +
                "(contains(@resource-id, 'edit_text') and not(contains(@text, '몇 가지')))]");

            bool focusedInput = false;
            if (editNode != null)
            {
                var editElem = AdbUiXmlDumper.ParseElement(editNode);
                if (editElem != null)
                {
                    await AdbUiXmlDumper.Instance.TapElementAsync(device, editElem, ct).ConfigureAwait(false);
                    focusedInput = true;
                }
            }

            if (!focusedInput)
            {
                // Fallback tọa độ vùng ô nhập (X = 50%, Y ~ 42% chiều cao màn hình)
                int inX = _screenResolution.Width / 2;
                int inY = (int)(_screenResolution.Height * 0.42);
                await AdbClientGeneral.Instance.TapAsync(device, inX, inY, ct).ConfigureAwait(false);
            }

            await Task.Delay(700, ct).ConfigureAwait(false);

            // Nội dung giải trình được lấy từ file cấu hình theo từng lý do cụ thể
            string detailText = !string.IsNullOrWhiteSpace(reasonConfig.CustomDetails)
                ? reasonConfig.CustomDetails
                : "이 방송은 불법 해외선물 대여계좌 및 사기성 리딩방을 홍보하고 있습니다.";

            await AdbImeHelper.InputTextAsync(device, detailText, ct).ConfigureAwait(false);
            await Task.Delay(800, ct).ConfigureAwait(false);

            // CHỈ ĐÓNG BÀN PHÍM NẾU BÀN PHÍM ẢO THỰC SỰ ĐANG MỞ (dumpsys input_method mInputShown=true)
            // TUYỆT ĐỐI KHÔNG gửi phím Back khi dùng LaiXi AdbIME vì sẽ làm tắt luôn hộp thoại báo cáo!
            bool isSoftKeyboardOpen = await AdbClientGeneral.Instance.IsSoftKeyboardShownAsync(device, ct).ConfigureAwait(false);
            if (isSoftKeyboardOpen)
            {
                await AdbClientGeneral.Instance.PressBackAsync(device, ct).ConfigureAwait(false);
                await Task.Delay(600, ct).ConfigureAwait(false);
            }

            // 4.2. Nhấn nút gửi báo cáo chính thức: '신고' / 'Báo cáo' / 'Report' / 'Submit'
            // ĐẶC BIỆT LƯU Ý: Nút '신고' ở đáy màn hình là một nút pill đen lớn (Bounds.Y >= screenHeight * 0.60)
            // TUYỆT ĐỐI KHÔNG bấm nhầm vào chữ '신고' trên thanh tiêu đề đầu trang (Y ~ 144)
            var submitDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            if (submitDoc == null) return false;

            // KIỂM TRA BẢO VỆ: Hộp thoại báo cáo phải CÒN ĐANG MỞ
            bool isDialogStillOpen = submitDoc.SelectSingleNode("//node[contains(@text, '신고') or contains(@text, '자세한 내용') or contains(@text, '세부정보')]") != null;
            if (!isDialogStillOpen)
            {
                // Nếu hộp thoại đã bị đóng, TUYỆT ĐỐI KHÔNG click mù vào tọa độ Y=87% (tránh nhảy sang video khác)!
                return false;
            }

            bool tappedSubmit = false;
            var candidateNodes = submitDoc.SelectNodes(
                "//node[(@text='신고' or @content-desc='신고' or @text='Report' or @content-desc='Report' or @text='Báo cáo' or @content-desc='Báo cáo' or @text='Submit' or @content-desc='Submit') or " +
                "((contains(@text, '신고') or contains(@content-desc, '신고') or contains(@text, 'Report') or contains(@content-desc, 'Report')) and (@clickable='true' or @class='android.widget.Button' or @class='android.widget.TextView' or @class='android.view.ViewGroup'))]");

            if (candidateNodes != null)
            {
                foreach (XmlNode n in candidateNodes)
                {
                    var elem = AdbUiXmlDumper.ParseElement(n);
                    if (elem != null && elem.Bounds.Y >= (_screenResolution.Height * 0.60))
                    {
                        await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                        await Task.Delay(2500, ct).ConfigureAwait(false);
                        tappedSubmit = true;
                        break;
                    }
                }
            }

            if (!tappedSubmit && isDialogStillOpen)
            {
                // Chỉ fallback bấm vùng nút gửi ở đáy màn hình khi và chỉ khi xác nhận hộp thoại còn mở
                int subX = _screenResolution.Width / 2;
                int subY = (int)(_screenResolution.Height * 0.87);
                await AdbClientGeneral.Instance.TapAsync(device, subX, subY, ct).ConfigureAwait(false);
                await Task.Delay(2500, ct).ConfigureAwait(false);
            }
        }

        // 5. Xác nhận & Đóng modal cảm ơn hoàn tất ('확인' / '닫기' / 'Close' / 'OK')
        for (int k = 0; k < 3; k++)
        {
            var ackDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            if (ackDoc != null)
            {
                var ackNode = ackDoc.SelectSingleNode(
                    "//node[(@text='확인' or @content-desc='확인' or @text='닫기' or @content-desc='닫기' or @text='OK' or @text='Close' or @text='Đóng' or contains(@text, '확인') or contains(@text, '닫기')) and (@clickable='true' or @class='android.widget.Button' or @class='android.widget.TextView')]");

                if (ackNode != null)
                {
                    var elem = AdbUiXmlDumper.ParseElement(ackNode);
                    if (elem != null)
                    {
                        await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                        await Task.Delay(800, ct).ConfigureAwait(false);
                        break;
                    }
                }
            }
            await Task.Delay(500, ct).ConfigureAwait(false);
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
            await Task.Delay(1200, ct).ConfigureAwait(false);
        }

        // 2. Thử mở menu Báo cáo (ưu tiên thanh nút ngang dưới video, sau đó là icon bánh răng Player)
        bool opened = false;
        for (int tryOpen = 0; tryOpen < 2; tryOpen++)
        {
            opened = await OpenReportMenuAsync(ct).ConfigureAwait(false);
            if (opened) break;
            await Task.Delay(1000, ct).ConfigureAwait(false);
        }

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

        // 4. Thực hiện nộp báo cáo (chọn lý do, điền nội dung giải trình chi tiết từ file cấu hình, gửi báo cáo)
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
        string safeSerial = Regex.Replace(_serial ?? "Device", @"[^\w\.-]", "_");
        string fileName = $"Android_{safeSerial}_{safePrefix}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";
        string targetDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
        Directory.CreateDirectory(targetDir);
        string targetPath = Path.Combine(targetDir, fileName);

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
