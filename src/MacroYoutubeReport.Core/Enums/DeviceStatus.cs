namespace MacroYoutubeReport.Core.Enums;

/// <summary>
/// Trạng thái kết nối và sẵn sàng của thiết bị (Android Serial hoặc Chrome Profile).
/// </summary>
public enum DeviceStatus
{
    Offline,
    Online,
    Busy,
    Cooldown,
    Unauthorized,
    Error
}
