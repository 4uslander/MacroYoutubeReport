using AdvancedSharpAdbClient.Models;
using System.Xml;

namespace MacroYoutubeReport.Driver.Android.Adb;

/// <summary>
/// Tiện ích hỗ trợ nhận diện và đóng/mở khung Trò chuyện trực tiếp (Live Chat / 실시간 채팅)
/// trên ứng dụng YouTube Android trong các luồng phát trực tiếp (Live Stream).
/// Giúp làm lộ các nút chức năng cốt lõi (Báo cáo, Thích, Chia sẻ) bị khung chat che khuất.
/// </summary>
public static class LiveChatOverlayHelper
{
    private static readonly string[] CloseChatXPaths = new string[]
    {
        // 1. Nút đóng chuẩn YouTube theo resource-id
        "//node[@resource-id='com.google.android.youtube:id/close_button']",
        "//node[@resource-id='com.google.android.youtube:id/live_chat_overlay_close_button']",
        "//node[@resource-id='com.google.android.youtube:id/panel_close_button']",
        "//node[contains(@resource-id, 'live_chat') and contains(@resource-id, 'close')]",
        "//node[contains(@resource-id, 'panel_close')]",

        // 2. Nút đóng theo content-desc đa ngôn ngữ (VI, EN, KR)
        "//node[contains(@content-desc, 'Đóng cuộc trò chuyện') or contains(@content-desc, 'Đóng trò chuyện') or contains(@content-desc, 'Đóng chat') or @content-desc='Đóng']",
        "//node[contains(@content-desc, 'Close chat') or contains(@content-desc, 'close chat') or contains(@content-desc, 'Hide chat') or @content-desc='Close' or @content-desc='close']",
        "//node[contains(@content-desc, '채팅 닫기') or contains(@content-desc, '실시간 채팅 닫기') or @content-desc='닫기' or @text='닫기']",

        // 3. Nút đóng dạng ImageView / ImageButton trong header chat
        "//node[(@content-desc='닫기' or @text='닫기' or @content-desc='Close' or @content-desc='Đóng' or @content-desc='X' or @text='X') and (ancestor::node[contains(@resource-id, 'live_chat')] or ancestor::node[contains(@resource-id, 'chat')] or ancestor::node[contains(@resource-id, 'header') or contains(@resource-id, 'panel')])]",
        "//node[(@class='android.widget.ImageView' or @class='android.widget.ImageButton') and (contains(@content-desc, '닫기') or contains(@content-desc, 'Đóng') or contains(@content-desc, 'Close') or @content-desc='X' or @text='X')]"
    };

    private static readonly string[] LiveChatContainerXPaths = new string[]
    {
        "//node[@resource-id='com.google.android.youtube:id/live_chat_content']",
        "//node[@resource-id='com.google.android.youtube:id/live_chat_action_panel']",
        "//node[@resource-id='com.google.android.youtube:id/live_chat_text_field_bar']",
        "//node[contains(@resource-id, 'live_chat_overlay')]"
    };

    /// <summary>
    /// Kiểm tra xem khung Live Chat có đang mở bung che khuất màn hình hay không.
    /// Lưu ý: Bỏ qua thanh preview thu gọn 1 dòng (collapsed bar) nằm dưới video.
    /// </summary>
    public static async Task<bool> IsLiveChatOpenAsync(DeviceData device, CancellationToken ct = default)
    {
        var doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
        if (doc == null) return false;

        // 1. Nếu có nút đóng chat rõ ràng -> Chắc chắn Live Chat đang mở
        foreach (var closeXp in CloseChatXPaths)
        {
            if (doc.SelectSingleNode(closeXp) != null)
            {
                return true;
            }
        }

        // 2. Kiểm tra container chat thực sự (chỉ tính nếu chiếm chiều cao lớn >= 300px, tránh thanh preview 1 dòng)
        foreach (var xp in LiveChatContainerXPaths)
        {
            var node = doc.SelectSingleNode(xp);
            if (node != null)
            {
                var elem = AdbUiXmlDumper.ParseElement(node);
                if (elem != null && elem.Bounds.Height >= 300)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Đảm bảo đóng khung Live Chat nếu đang mở che khuất các nút hành động của video.
    /// TUYỆT ĐỐI KHÔNG nhấn phím Back vì sẽ làm đóng luôn video đang xem và quay về danh sách kênh!
    /// </summary>
    public static async Task<bool> EnsureLiveChatClosedAsync(
        DeviceData device, 
        int screenWidth = 0, 
        int screenHeight = 0, 
        CancellationToken ct = default)
    {
        // 1. Kiểm tra ban đầu xem Live Chat có đang mở bung che màn hình không
        bool isOpen = await IsLiveChatOpenAsync(device, ct).ConfigureAwait(false);
        if (!isOpen)
        {
            return true;
        }

        // 2. Thử tìm và chạm nút đóng theo XPath
        var doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
        if (doc != null)
        {
            foreach (var closeXp in CloseChatXPaths)
            {
                var node = doc.SelectSingleNode(closeXp);
                if (node != null)
                {
                    var elem = AdbUiXmlDumper.ParseElement(node);
                    if (elem != null)
                    {
                        await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                        await Task.Delay(800, ct).ConfigureAwait(false);
                        if (!await IsLiveChatOpenAsync(device, ct).ConfigureAwait(false))
                        {
                            return true;
                        }
                    }
                }
            }
        }

        // Tuyệt đối không bấm Back khi video đang phát!
        return true;
    }

    /// <summary>
    /// Kiểm tra xem Bottom Sheet thông tin / mô tả video (Description Panel) có đang mở che khuất giao diện phát hay không.
    /// </summary>
    public static async Task<bool> IsDescriptionPanelOpenAsync(DeviceData device, CancellationToken ct = default)
    {
        var doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
        if (doc == null) return false;

        var panelNode = doc.SelectSingleNode(
            "//node[(@resource-id='com.google.android.youtube:id/description_header' or " +
            "contains(@resource-id, 'description_content') or " +
            "contains(@resource-id, 'engagement_panel') or " +
            "@text='설명' or @text='Description' or @text='Mô tả' or " +
            "@content-desc='설명' or @content-desc='Description' or @content-desc='Mô tả') and " +
            "not(ancestor::node[contains(@resource-id, 'player')])]"
        );

        if (panelNode != null)
        {
            var elem = AdbUiXmlDumper.ParseElement(panelNode);
            // Chỉ coi là đang mở nếu panel có chiều cao lớn (>= 400px), tránh nhầm với nhãn chữ nhỏ
            if (elem != null && elem.Bounds.Height >= 400)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Đảm bảo đóng bảng thông tin/mô tả video nếu đang mở để trả lại giao diện phát video chính thống.
    /// </summary>
    public static async Task<bool> EnsureDescriptionPanelClosedAsync(DeviceData device, CancellationToken ct = default)
    {
        if (!await IsDescriptionPanelOpenAsync(device, ct).ConfigureAwait(false))
        {
            return true;
        }

        // 1. Thử tìm nút đóng X trên bảng mô tả
        var doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
        if (doc != null)
        {
            var closeNode = doc.SelectSingleNode("//node[(@resource-id='com.google.android.youtube:id/close_button' or contains(@content-desc, '닫기') or contains(@content-desc, 'Close')) and not(ancestor::node[contains(@resource-id, 'player')])]");
            if (closeNode != null)
            {
                var elem = AdbUiXmlDumper.ParseElement(closeNode);
                if (elem != null)
                {
                    await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                    await Task.Delay(800, ct).ConfigureAwait(false);
                    if (!await IsDescriptionPanelOpenAsync(device, ct).ConfigureAwait(false))
                    {
                        return true;
                    }
                }
            }
        }

        return !await IsDescriptionPanelOpenAsync(device, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Thực hiện cuộn nhẹ Live Chat để mô phỏng người xem thật đang theo dõi tin nhắn trong luồng live.
    /// </summary>
    public static async Task<bool> ScrollLiveChatAsync(DeviceData device, int screenWidth, int screenHeight, CancellationToken ct = default)
    {
        int x = screenWidth / 2;
        int yStart = (int)(screenHeight * 0.75);
        int yEnd = (int)(screenHeight * 0.45);

        await AdbClientGeneral.Instance.SwipeAsync(device, x, yStart, x, yEnd, 350, ct).ConfigureAwait(false);
        await Task.Delay(500, ct).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Chạm vào vùng video để đánh thức thanh điều khiển (Player Controls) và nhấn nút Cài đặt/Bánh răng trên Player.
    /// Giới hạn nghiêm ngặt tọa độ trên đỉnh Player (Y <= 18% chiều cao màn hình) để tuyệt đối không chạm nhầm vào tiêu đề video.
    /// </summary>
    public static async Task<bool> AwakenPlayerControlsAndOpenSettingsAsync(
        DeviceData device,
        int screenWidth,
        int screenHeight,
        CancellationToken ct = default)
    {
        // 0. Đảm bảo đóng cả Live Chat lẫn bảng mô tả video trước khi thao tác Player Controls
        await EnsureDescriptionPanelClosedAsync(device, ct).ConfigureAwait(false);
        await EnsureLiveChatClosedAsync(device, screenWidth, screenHeight, ct).ConfigureAwait(false);

        // Giới hạn tuyệt đối cho Player Settings: Y nằm trên đỉnh của video player (5% - 18%)
        // Tuyệt đối không chạm vào vùng Y > 18% (nơi bắt đầu tiêu đề video)
        int maxPlayerY = screenHeight > 0 ? (int)(screenHeight * 0.18) : 400;
        int minPlayerY = screenHeight > 0 ? (int)(screenHeight * 0.05) : 50;

        for (int retry = 0; retry < 3; retry++)
        {
            ct.ThrowIfCancellationRequested();

            // 1. Chạm nhẹ vào giữa khung phát video (Y ~ 14%) để đánh thức thanh điều khiển
            int playerX = screenWidth / 2;
            int playerY = (int)(screenHeight * 0.14);
            await AdbClientGeneral.Instance.TapAsync(device, playerX, playerY, ct).ConfigureAwait(false);
            await Task.Delay(400, ct).ConfigureAwait(false);

            // 2. Dump UI tìm nút Bánh răng (Settings) / 3 chấm TRÊN VIDEO PLAYER
            var doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            if (doc != null)
            {
                var candidateNodes = doc.SelectNodes(
                    "//node[@resource-id='com.google.android.youtube:id/player_settings_button' or " +
                    "@resource-id='com.google.android.youtube:id/player_overflow_button' or " +
                    "(@content-desc='설정' or @content-desc='Settings' or @content-desc='Cài đặt' or @content-desc='옵션 더보기' or " +
                    "contains(@content-desc, '설정') or contains(@content-desc, '옵션 더보기') or contains(@content-desc, 'Player settings')) or " +
                    "(@resource-id='com.google.android.youtube:id/overflow_button' and not(ancestor::node[contains(@resource-id, 'metadata')]))]"
                );

                if (candidateNodes != null)
                {
                    foreach (XmlNode node in candidateNodes)
                    {
                        var elem = AdbUiXmlDumper.ParseElement(node);
                        // BẮT BUỘC: Y từ 5% đến 18% (trên đầu Player) và X >= 70% (góc phải)
                        if (elem != null && elem.Bounds.Y >= minPlayerY && elem.Bounds.Y <= maxPlayerY && elem.CenterPoint.X >= (screenWidth * 0.70))
                        {
                            await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                            await Task.Delay(1000, ct).ConfigureAwait(false);

                            // Kiểm tra xem Bottom Sheet cài đặt (chứa Báo cáo/Chất lượng) có mở ra không
                            var verifyDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
                            if (verifyDoc != null && verifyDoc.SelectSingleNode("//node[contains(@text, '신고') or contains(@text, 'Báo cáo') or contains(@text, 'Report') or contains(@text, '품질') or contains(@text, 'Quality') or contains(@text, '자막') or contains(@text, '고급 설정') or contains(@text, '추가 설정')]") != null)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            // 3. Fallback tọa độ: Nút Bánh răng trên YouTube Android luôn nằm ở góc trên bên phải của Video Player
            // Y nằm trong khoảng 8% - 10%, X ~ 94%
            int cornerX = (int)(screenWidth * 0.94);
            int cornerY = Math.Max((int)(screenHeight * 0.088), 170);
            await AdbClientGeneral.Instance.TapAsync(device, cornerX, cornerY, ct).ConfigureAwait(false);
            await Task.Delay(1000, ct).ConfigureAwait(false);

            // Kiểm tra xem Bottom Sheet menu cài đặt đã mở ra chưa
            var verifyCornerDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            if (verifyCornerDoc != null && verifyCornerDoc.SelectSingleNode("//node[contains(@text, '신고') or contains(@text, 'Báo cáo') or contains(@text, 'Report') or contains(@text, '품질') or contains(@text, 'Quality') or contains(@text, '자막') or contains(@text, '고급 설정') or contains(@text, '추가 설정')]") != null)
            {
                return true;
            }

            await Task.Delay(500, ct).ConfigureAwait(false);
        }

        return false;
    }

    /// <summary>
    /// Tìm và nhấn nút Meatball Menu ('...' / '추가 작업' / More actions) hoặc nút Báo cáo trực tiếp trên thanh Action Bar dưới video.
    /// Khóa chặt giới hạn tọa độ để tuyệt đối không chạm nhầm vào khung Tiêu đề (Title) hay video đề xuất.
    /// </summary>
    public static async Task<bool> TapActionRowMoreButtonAsync(DeviceData device, int screenWidth = 0, int screenHeight = 0, CancellationToken ct = default)
    {
        // 0. Đảm bảo đóng bảng thông tin video nếu đang mở
        await EnsureDescriptionPanelClosedAsync(device, ct).ConfigureAwait(false);

        // Vùng tọa độ Y của Action Bar luôn nằm ngay dưới channel info (Y ~ 40% đến 55%)
        // Đặt minY = 40% để tuyệt đối không chạm trúng Tiêu đề video (Title nằm ở Y ~ 35% - 39%)
        int minY = screenHeight > 0 ? (int)(screenHeight * 0.40) : 0;
        int maxY = screenHeight > 0 ? (int)(screenHeight * 0.55) : int.MaxValue;

        // Quét tối đa 2 lượt: Lượt 1 kiểm tra nút hiện tại, Lượt 2 cuộn nhẹ ngang Action Bar để lộ các nút bị khuất bên phải
        for (int attempt = 0; attempt < 2; attempt++)
        {
            var doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            if (doc != null)
            {
                // ƯU TIÊN 1: Tìm Meatball Icon ('...' / '추가 작업' / 'More actions' / 'Tùy chọn khác') trên Action Bar
                var meatballNodes = doc.SelectNodes(
                    "//node[(@class='android.widget.Button' or @class='android.widget.ImageView' or @class='android.view.ViewGroup') and " +
                    "(@content-desc='추가 작업' or @content-desc='More actions' or @content-desc='Tùy chọn khác' or @content-desc='Thao tác khác' or " +
                    "(@clickable='true' and contains(@content-desc, '추가 작업')) or " +
                    "(@content-desc!='' and not(contains(@content-desc, '더보기')) and not(contains(@content-desc, '설명')) and not(contains(@content-desc, '구독')) and not(contains(@content-desc, '채널')) and not(contains(@content-desc, '작업 메뉴')) and " +
                    "(contains(@content-desc, '추가 작업') or contains(@content-desc, 'More actions') or contains(@content-desc, 'Tùy chọn khác'))))]"
                );

                AdbUiElement? meatballElem = null;
                if (meatballNodes != null)
                {
                    foreach (XmlNode node in meatballNodes)
                    {
                        var elem = AdbUiXmlDumper.ParseElement(node);
                        if (elem != null && (screenHeight == 0 || (elem.CenterPoint.Y >= minY && elem.CenterPoint.Y <= maxY)))
                        {
                            meatballElem = elem;
                            break;
                        }
                    }
                }

                // Fallback Meatball: Nếu không bắt được qua content-desc, tìm nút clickable ở góc phải của Action Bar (X >= 80%, Y trong Action Bar)
                if (meatballElem == null && screenWidth > 0 && screenHeight > 0)
                {
                    var rightmostBarNodes = doc.SelectNodes("//node[(@class='android.widget.Button' or @class='android.view.ViewGroup') and @clickable='true']");
                    if (rightmostBarNodes != null)
                    {
                        foreach (XmlNode node in rightmostBarNodes)
                        {
                            var elem = AdbUiXmlDumper.ParseElement(node);
                            if (elem != null && elem.CenterPoint.X >= (screenWidth * 0.80) && 
                                elem.CenterPoint.Y >= minY && elem.CenterPoint.Y <= maxY &&
                                elem.Bounds.Width < (screenWidth * 0.25) && elem.Bounds.Height < (screenHeight * 0.15))
                            {
                                meatballElem = elem;
                                break;
                            }
                        }
                    }
                }

                if (meatballElem != null)
                {
                    await AdbUiXmlDumper.Instance.TapElementAsync(device, meatballElem, ct).ConfigureAwait(false);
                    await Task.Delay(800, ct).ConfigureAwait(false);

                    // Sau khi bấm Meatball Icon, Bottom Sheet sẽ hiện ra chứa duy nhất mục '신고' (Báo cáo) ở đáy màn hình
                    var popupDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
                    if (popupDoc != null)
                    {
                        var reportNodes = popupDoc.SelectNodes(
                            "//node[(@text='신고' or @content-desc='신고' or contains(@text, '신고') or contains(@content-desc, '신고') or " +
                            "@text='Report' or @content-desc='Report' or contains(@text, 'Report') or contains(@content-desc, 'Report') or " +
                            "@text='Báo cáo' or @content-desc='Báo cáo' or contains(@text, 'Báo cáo') or contains(@content-desc, 'Báo cáo')) and " +
                            "not(contains(@text, '실시간')) and not(contains(@content-desc, '실시간')) and not(contains(@text, '채팅')) and not(contains(@content-desc, '채팅'))]"
                        );

                        if (reportNodes != null)
                        {
                            foreach (XmlNode rNode in reportNodes)
                            {
                                var repElem = AdbUiXmlDumper.ParseElement(rNode);
                                // Nút 신고 trong Bottom Sheet nằm ở đáy màn hình (Y >= 65%)
                                if (repElem != null && (screenHeight == 0 || repElem.CenterPoint.Y >= (screenHeight * 0.65)))
                                {
                                    await AdbUiXmlDumper.Instance.TapElementAsync(device, repElem, ct).ConfigureAwait(false);
                                    await Task.Delay(1200, ct).ConfigureAwait(false);
                                    return true;
                                }
                            }
                        }
                    }
                }

                // ƯU TIÊN 2: Tìm trực tiếp nút Báo cáo (신고 / Report / Báo cáo) trên Action Bar (nếu không bị gộp vào Meatball)
                var directReportNode = doc.SelectSingleNode(
                    "//node[(@class='android.widget.Button' or @class='android.widget.ImageView' or @class='android.view.ViewGroup' or @class='android.widget.TextView') and " +
                    "(@text='신고' or @content-desc='신고' or contains(@content-desc, '신고') or " +
                    "@text='Report' or @content-desc='Report' or contains(@content-desc, 'Report') or " +
                    "@text='Báo cáo' or @content-desc='Báo cáo' or contains(@content-desc, 'Báo cáo')) and " +
                    "not(contains(@text, '실시간')) and not(contains(@content-desc, '실시간')) and not(contains(@text, '채팅')) and not(contains(@content-desc, '채팅'))]"
                );

                if (directReportNode != null)
                {
                    var elem = AdbUiXmlDumper.ParseElement(directReportNode);
                    if (elem != null && (screenHeight == 0 || (elem.CenterPoint.Y >= minY && elem.CenterPoint.Y <= maxY)))
                    {
                        await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                        await Task.Delay(1200, ct).ConfigureAwait(false);
                        return true;
                    }
                }

                // Nếu lượt 1 chưa thấy, cuộn nhẹ NGANG thanh Action Bar sang trái (X: 85% sang 25% ở Y ~ 48%)
                // Tuyệt đối không cuộn ở Y < 40% để không chạm trúng Tiêu đề video
                if (attempt == 0 && screenWidth > 0 && screenHeight > 0)
                {
                    int actionY = (int)(screenHeight * 0.48);
                    int startX = (int)(screenWidth * 0.85);
                    int endX = (int)(screenWidth * 0.25);

                    await AdbClientGeneral.Instance.SwipeAsync(device, startX, actionY, endX, actionY, 350, ct).ConfigureAwait(false);
                    await Task.Delay(800, ct).ConfigureAwait(false);
                }
            }
        }

        return false;
    }
}
