using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient.Receivers;
using System.Text.RegularExpressions;

namespace MacroYoutubeReport.Driver.Android.Adb;

/// <summary>
/// Các lệnh thao tác ADB cấp thấp trên thiết bị Android.
/// </summary>
public sealed class AdbClientGeneral
{
    private static readonly Lazy<AdbClientGeneral> _instance = new(() => new AdbClientGeneral());
    public static AdbClientGeneral Instance => _instance.Value;

    public AdbClient Client => AdbServerManager.Instance.Client;

    public async Task<string> ExecuteShellAsync(DeviceData device, string command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(device);
        var receiver = new ConsoleOutputReceiver();
        try
        {
            await Client.ExecuteRemoteCommandAsync(command, device, receiver, ct).ConfigureAwait(false);
            return receiver.ToString();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    public async Task<bool> TapAsync(DeviceData device, int x, int y, CancellationToken ct = default)
    {
        string result = await ExecuteShellAsync(device, $"input tap {x} {y}", ct).ConfigureAwait(false);
        return !result.Contains("Error", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> SwipeAsync(DeviceData device, int x1, int y1, int x2, int y2, int durationMs = 350, CancellationToken ct = default)
    {
        string result = await ExecuteShellAsync(device, $"input swipe {x1} {y1} {x2} {y2} {durationMs}", ct).ConfigureAwait(false);
        return !result.Contains("Error", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> SendKeyEventAsync(DeviceData device, int keyCode, CancellationToken ct = default)
    {
        string result = await ExecuteShellAsync(device, $"input keyevent {keyCode}", ct).ConfigureAwait(false);
        return !result.Contains("Error", StringComparison.OrdinalIgnoreCase);
    }

    public Task<bool> SendHomeAsync(DeviceData device, CancellationToken ct = default)
    {
        return SendKeyEventAsync(device, 3, ct); // KEYCODE_HOME = 3
    }

    public async Task<bool> PressBackAsync(DeviceData device, CancellationToken ct = default)
    {
        await ExecuteShellAsync(device, "input keyevent 4", ct).ConfigureAwait(false); // KEYCODE_BACK = 4
        return true;
    }

    public async Task<bool> LaunchAppByLauncherAsync(DeviceData device, string packageName, CancellationToken ct = default)
    {
        string output = await ExecuteShellAsync(device, $"am start -a android.intent.action.MAIN -c android.intent.category.LAUNCHER -p {packageName}", ct).ConfigureAwait(false);
        if (output.Contains("Error", StringComparison.OrdinalIgnoreCase))
        {
            await ExecuteShellAsync(device, $"monkey -p {packageName} -c android.intent.category.LAUNCHER 1", ct).ConfigureAwait(false);
        }
        return true;
    }

    public async Task<bool> IsAppInForegroundAsync(DeviceData device, string packageName, CancellationToken ct = default)
    {
        string output = await ExecuteShellAsync(device, "dumpsys window", ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(output)) return false;

        return Regex.IsMatch(output, $@"(mCurrentFocus|mFocusedApp).*?{Regex.Escape(packageName)}", RegexOptions.IgnoreCase);
    }

    public async Task<bool> StopAppAsync(DeviceData device, string packageName, CancellationToken ct = default)
    {
        await ExecuteShellAsync(device, $"am force-stop {packageName}", ct).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Đóng tất cả tab/ứng dụng trên thiết bị Android: buộc dừng các ứng dụng trọng tâm (YouTube, Chrome),
    /// dọn dẹp tiến trình nền và giải phóng khay ứng dụng đa nhiệm (Recent Apps).
    /// </summary>
    public async Task<bool> CloseAllRecentAppsAsync(DeviceData device, CancellationToken ct = default)
    {
        try
        {
            // 1. Buộc dừng các ứng dụng chính
            await ExecuteShellAsync(device, "am force-stop com.google.android.youtube", ct).ConfigureAwait(false);
            await ExecuteShellAsync(device, "am force-stop com.android.chrome", ct).ConfigureAwait(false);
            await ExecuteShellAsync(device, "am force-stop com.sec.android.app.sbrowser", ct).ConfigureAwait(false);
            await ExecuteShellAsync(device, "am kill-all", ct).ConfigureAwait(false);

            // 2. Mở khay đa nhiệm Recent Apps
            await SendKeyEventAsync(device, 187, ct).ConfigureAwait(false); // KEYCODE_APP_SWITCH = 187
            await Task.Delay(500, ct).ConfigureAwait(false);

            // 3. Tìm và nhấn nút Đóng tất cả / Close all (hỗ trợ Samsung One UI, Xiaomi, Pixel, v.v.)
            bool closedViaUi = false;
            try
            {
                closedViaUi = await AdbUiXmlDumper.Instance.TapXPathAsync(device,
                    "//node[(@text='Đóng tất cả' or @text='Close all' or @text='모두 닫기' or @content-desc='Đóng tất cả' or @content-desc='Close all' or contains(@resource-id, 'clear_all') or contains(@resource-id, 'clear_all_button')) and (@clickable='true' or @class='android.widget.Button' or @class='android.widget.TextView')]",
                    ct, withJitter: false).ConfigureAwait(false);
            }
            catch
            {
                // Bỏ qua nếu lỗi dump UI
            }

            if (!closedViaUi)
            {
                // Fallback: đưa về màn hình Home
                await SendHomeAsync(device, ct).ConfigureAwait(false);
            }
            return true;
        }
        catch
        {
            await SendHomeAsync(device, ct).ConfigureAwait(false);
            return false;
        }
    }

    public async Task<string> CaptureScreenshotFileAsync(DeviceData device, string targetFilePath, CancellationToken ct = default)
    {
        string dir = Path.GetDirectoryName(targetFilePath) ?? AppDomain.CurrentDomain.BaseDirectory;
        Directory.CreateDirectory(dir);

        string remoteTmpPath = $"/sdcard/screen_{DateTime.UtcNow.Ticks}.png";
        try
        {
            await ExecuteShellAsync(device, $"screencap -p {remoteTmpPath}", ct).ConfigureAwait(false);

            using var syncService = new SyncService(Client, device);
            using (var stream = File.Create(targetFilePath))
            {
                syncService.Pull(remoteTmpPath, stream, null, false);
            }

            return targetFilePath;
        }
        finally
        {
            await ExecuteShellAsync(device, $"rm -f {remoteTmpPath}", CancellationToken.None).ConfigureAwait(false);
        }
    }

    public async Task<(int Width, int Height)> GetScreenResolutionAsync(DeviceData device, CancellationToken ct = default)
    {
        string output = await ExecuteShellAsync(device, "wm size", ct).ConfigureAwait(false);
        var match = Regex.Match(output, @"(\d{3,4})x(\d{3,4})");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int w) && int.TryParse(match.Groups[2].Value, out int h))
        {
            return (w, h);
        }

        return (1080, 2400);
    }

    /// <summary>
    /// Kiểm tra xem bàn phím ảo của hệ thống Android có đang thực sự hiển thị trên màn hình hay không.
    /// Tránh gửi phím Back nhầm làm tắt các hộp thoại BottomSheet/Dialog khi bàn phím không mở.
    /// </summary>
    public async Task<bool> IsSoftKeyboardShownAsync(DeviceData device, CancellationToken ct = default)
    {
        try
        {
            string output = await ExecuteShellAsync(device, "dumpsys input_method", ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(output)) return false;

            // Kiểm tra mInputShown=true (Android 7 - 14)
            return output.Contains("mInputShown=true", StringComparison.OrdinalIgnoreCase) ||
                   output.Contains("mIsInputViewShown=true", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
