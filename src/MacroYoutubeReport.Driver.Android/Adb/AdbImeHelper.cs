using AdvancedSharpAdbClient.Models;

namespace MacroYoutubeReport.Driver.Android.Adb;

/// <summary>
/// Hỗ trợ gõ văn bản tiếng Hàn (Hangul), tiếng Việt (Unicode) qua AdbIME broadcast hoặc shell input text.
/// </summary>
public static class AdbImeHelper
{
    public const string AdbKeyboardImeId = "youhu.laixijs/.KeyboardServices.AdbIME";
    public const string AdbKeyboardBroadcastAction = "ADB_INPUT_TEXT";
    public const string AdbKeyboardBroadcastExtraKey = "msg";

    public static async Task<bool> InputTextAsync(DeviceData device, string text, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(text)) return false;

        bool broadcastSuccess = await TryInputTextByAdbKeyboardAsync(device, text, ct).ConfigureAwait(false);
        if (broadcastSuccess)
        {
            return true;
        }

        string escaped = text.Replace(" ", "%s").Replace("&", "\\&").Replace("\"", "\\\"");
        string result = await AdbClientGeneral.Instance.ExecuteShellAsync(device, $"input text \"{escaped}\"", ct).ConfigureAwait(false);
        return !result.Contains("Error", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<bool> TryInputTextByAdbKeyboardAsync(DeviceData device, string text, CancellationToken ct)
    {
        bool imeReady = await EnsureAdbKeyboardImeAsync(device, ct).ConfigureAwait(false);
        if (!imeReady)
        {
            return false;
        }

        string escaped = EscapeShellSingleQuote(text);
        string command = $"am broadcast -a {AdbKeyboardBroadcastAction} --es {AdbKeyboardBroadcastExtraKey} '{escaped}'";
        string output = await AdbClientGeneral.Instance.ExecuteShellAsync(device, command, ct).ConfigureAwait(false);

        return output.Contains("Broadcast completed", StringComparison.OrdinalIgnoreCase) &&
               output.Contains("result=0", StringComparison.OrdinalIgnoreCase);
    }

    private static string EscapeShellSingleQuote(string value)
    {
        return (value ?? string.Empty).Replace("'", "'\\''");
    }

    private static async Task<bool> EnsureAdbKeyboardImeAsync(DeviceData device, CancellationToken ct)
    {
        string currentIme = await GetDefaultInputMethodAsync(device, ct).ConfigureAwait(false);
        if (currentIme.Contains("AdbIME", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        await AdbClientGeneral.Instance.ExecuteShellAsync(device, $"ime enable {AdbKeyboardImeId}", ct).ConfigureAwait(false);
        await AdbClientGeneral.Instance.ExecuteShellAsync(device, $"ime set {AdbKeyboardImeId}", ct).ConfigureAwait(false);

        string updatedIme = await GetDefaultInputMethodAsync(device, ct).ConfigureAwait(false);
        return updatedIme.Contains("AdbIME", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> GetDefaultInputMethodAsync(DeviceData device, CancellationToken ct)
    {
        string output = await AdbClientGeneral.Instance.ExecuteShellAsync(device, "settings get secure default_input_method", ct).ConfigureAwait(false);
        return (output ?? string.Empty).Trim();
    }
}
