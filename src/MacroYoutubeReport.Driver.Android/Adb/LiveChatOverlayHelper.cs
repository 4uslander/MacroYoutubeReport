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

        // 2. Nút đóng theo content-desc đa ngôn ngữ (VI, EN, KR)
        "//node[contains(@content-desc, 'Đóng cuộc trò chuyện') or contains(@content-desc, 'Đóng trò chuyện') or contains(@content-desc, 'Đóng chat')]",
        "//node[contains(@content-desc, 'Close chat') or contains(@content-desc, 'close chat') or contains(@content-desc, 'Hide chat')]",
        "//node[contains(@content-desc, '채팅 닫기') or contains(@content-desc, '실시간 채팅 닫기')]",

        // 3. Nút đóng dạng ImageView / ImageButton trong header chat
        "//node[(@content-desc='닫기' or @text='닫기' or @content-desc='Close' or @content-desc='Đóng') and (ancestor::node[contains(@resource-id, 'live_chat')] or ancestor::node[contains(@resource-id, 'chat')])]",
        "//node[(@class='android.widget.ImageView' or @class='android.widget.ImageButton') and (contains(@content-desc, '닫기') or contains(@content-desc, 'Đóng') or contains(@content-desc, 'Close')) and ancestor::node[contains(@resource-id, 'header') or contains(@resource-id, 'title')]]"
    };

    private static readonly string[] LiveChatContainerXPaths = new string[]
    {
        "//node[@resource-id='com.google.android.youtube:id/live_chat_content']",
        "//node[@resource-id='com.google.android.youtube:id/live_chat_action_panel']",
        "//node[@resource-id='com.google.android.youtube:id/live_chat_text_field_bar']",
        "//node[contains(@resource-id, 'live_chat_overlay')]"
    };

    /// <summary>
    /// Kiểm tra xem khung Live Chat có đang mở trên màn hình hay không.
    /// </summary>
    public static async Task<bool> IsLiveChatOpenAsync(DeviceData device, CancellationToken ct = default)
    {
        var doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
        if (doc == null) return false;

        foreach (var xp in LiveChatContainerXPaths)
        {
            if (doc.SelectSingleNode(xp) != null)
            {
                return true;
            }
        }

        foreach (var closeXp in CloseChatXPaths)
        {
            if (doc.SelectSingleNode(closeXp) != null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Đảm bảo đóng khung Live Chat nếu đang mở che khuất các nút hành động của video.
    /// </summary>
    public static async Task<bool> EnsureLiveChatClosedAsync(DeviceData device, CancellationToken ct = default)
    {
        var doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
        if (doc == null) return false;

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
                    return true;
                }
            }
        }

        return false;
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
    /// Chạm vào vùng video để đánh thức thanh điều khiển (Player Controls) và nhấn nút Cài đặt/3 chấm trên Player.
    /// Có cơ chế retry tối đa 3 lần đề phòng điều khiển tắt nhanh hoặc độ trễ mạng.
    /// </summary>
    public static async Task<bool> AwakenPlayerControlsAndOpenSettingsAsync(
        DeviceData device,
        int screenWidth,
        int screenHeight,
        CancellationToken ct = default)
    {
        for (int retry = 0; retry < 3; retry++)
        {
            ct.ThrowIfCancellationRequested();

            // 1. Chạm nhẹ vào giữa khung phát video (nửa trên màn hình, Y ~ 18-22%) để đánh thức thanh điều khiển
            int playerX = screenWidth / 2;
            int playerY = (int)(screenHeight * 0.20);
            await AdbClientGeneral.Instance.TapAsync(device, playerX, playerY, ct).ConfigureAwait(false);
            await Task.Delay(400, ct).ConfigureAwait(false);

            // 2. Dump UI để tìm nút Bánh răng (Settings) hoặc 3 chấm (Overflow) trên Video Player
            var doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            if (doc != null)
            {
                var btnNode = doc.SelectSingleNode(
                    "//node[@resource-id='com.google.android.youtube:id/player_settings_button' or " +
                    "@resource-id='com.google.android.youtube:id/player_overflow_button' or " +
                    "@resource-id='com.google.android.youtube:id/overflow_button' or " +
                    "contains(@content-desc, 'More options') or contains(@content-desc, 'Tùy chọn khác') or " +
                    "contains(@content-desc, '설정') or contains(@content-desc, 'Cài đặt') or contains(@content-desc, '옵션 더보기') or contains(@content-desc, '옵션')]"
                );

                if (btnNode != null)
                {
                    var elem = AdbUiXmlDumper.ParseElement(btnNode);
                    if (elem != null)
                    {
                        await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                        await Task.Delay(1000, ct).ConfigureAwait(false);
                        return true;
                    }
                }
            }

            // Fallback: Chạm vào góc trên bên phải của video player
            int cornerX = (int)(screenWidth * 0.93);
            int cornerY = (int)(screenHeight * 0.08);
            await AdbClientGeneral.Instance.TapAsync(device, cornerX, cornerY, ct).ConfigureAwait(false);
            await Task.Delay(800, ct).ConfigureAwait(false);

            // Kiểm tra xem menu bottom sheet hoặc popup có mở ra không
            var verifyDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            if (verifyDoc != null && verifyDoc.SelectSingleNode("//node[contains(@text, '신고') or contains(@text, 'Báo cáo') or contains(@text, 'Report') or contains(@text, '품질') or contains(@text, '자막')]") != null)
            {
                return true;
            }

            await Task.Delay(500, ct).ConfigureAwait(false);
        }

        return false;
    }

    /// <summary>
    /// Tìm và nhấn nút '추가 작업' (More actions / Tùy chọn khác) trên thanh hành động nằm ngay dưới video.
    /// Nếu chưa hiển thị, thực hiện vuốt ngang thanh nút từ phải sang trái để làm lộ nút ở cuối thanh.
    /// </summary>
    public static async Task<bool> TapActionRowMoreButtonAsync(DeviceData device, int screenWidth = 0, int screenHeight = 0, CancellationToken ct = default)
    {
        // 1. Thử quét tìm ngay lần đầu
        var doc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
        if (doc != null)
        {
            var moreNode = doc.SelectSingleNode(
                "//node[(@class='android.widget.Button' or @class='android.widget.ImageView' or @class='android.view.ViewGroup') and " +
                "(@content-desc='추가 작업' or @content-desc='More actions' or @content-desc='Tùy chọn khác' or @content-desc='작업 메뉴' or " +
                "contains(@content-desc, '추가 작업') or contains(@content-desc, 'More actions') or contains(@content-desc, 'Tùy chọn khác'))]"
            );

            if (moreNode != null)
            {
                var elem = AdbUiXmlDumper.ParseElement(moreNode);
                if (elem != null)
                {
                    await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                    await Task.Delay(1000, ct).ConfigureAwait(false);
                    return true;
                }
            }
        }

        // 2. Nếu chưa thấy và có thông số độ phân giải: vuốt ngang thanh Action Row (Y ~ 35%) từ phải sang trái
        if (screenWidth > 0 && screenHeight > 0)
        {
            int actionY = (int)(screenHeight * 0.35);
            int startX = (int)(screenWidth * 0.85);
            int endX = (int)(screenWidth * 0.18);

            await AdbClientGeneral.Instance.SwipeAsync(device, startX, actionY, endX, actionY, 350, ct).ConfigureAwait(false);
            await Task.Delay(800, ct).ConfigureAwait(false);

            var scrolledDoc = await AdbUiXmlDumper.Instance.DumpUiXmlDocumentAsync(device, ct).ConfigureAwait(false);
            if (scrolledDoc != null)
            {
                var moreNode = scrolledDoc.SelectSingleNode(
                    "//node[(@class='android.widget.Button' or @class='android.widget.ImageView' or @class='android.view.ViewGroup') and " +
                    "(@content-desc='추가 작업' or @content-desc='More actions' or @content-desc='Tùy chọn khác' or @content-desc='작업 메뉴' or " +
                    "contains(@content-desc, '추가 작업') or contains(@content-desc, 'More actions') or contains(@content-desc, 'Tùy chọn khác'))]"
                );

                if (moreNode != null)
                {
                    var elem = AdbUiXmlDumper.ParseElement(moreNode);
                    if (elem != null)
                    {
                        await AdbUiXmlDumper.Instance.TapElementAsync(device, elem, ct).ConfigureAwait(false);
                        await Task.Delay(1000, ct).ConfigureAwait(false);
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
