using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;
using MacroYoutubeReport.Core.Enums;

namespace MacroYoutubeReport.Driver.Android.Adb;

/// <summary>
/// Đại diện cho một thiết bị Android được quản lý trong ứng dụng.
/// </summary>
public sealed record ManagedAndroidDevice
{
    public string Serial { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string AndroidVersion { get; init; } = string.Empty;
    public DeviceStatus Status { get; set; }
    public DeviceData RawDevice { get; init; } = new();

    public bool IsAvailable => Status == DeviceStatus.Online;
}

/// <summary>
/// Quản lý danh sách các thiết bị Android cắm trực tiếp hoặc kết nối qua TCP/IP.
/// </summary>
public sealed class AdbDeviceManager
{
    private static readonly Lazy<AdbDeviceManager> _instance = new(() => new AdbDeviceManager());
    public static AdbDeviceManager Instance => _instance.Value;

    public AdbClient Client => AdbServerManager.Instance.Client;

    public async Task<IReadOnlyList<ManagedAndroidDevice>> ScanDevicesAsync(CancellationToken ct = default)
    {
        if (!AdbServerManager.Instance.EnsureServerRunning())
        {
            return Array.Empty<ManagedAndroidDevice>();
        }

        try
        {
            var rawList = Client.GetDevices();
            var result = new List<ManagedAndroidDevice>();

            foreach (var dev in rawList)
            {
                var status = dev.State switch
                {
                    DeviceState.Online => DeviceStatus.Online,
                    DeviceState.Unauthorized => DeviceStatus.Unauthorized,
                    DeviceState.Offline => DeviceStatus.Offline,
                    _ => DeviceStatus.Error
                };

                result.Add(new ManagedAndroidDevice
                {
                    Serial = dev.Serial ?? string.Empty,
                    Model = dev.Model ?? string.Empty,
                    AndroidVersion = dev.Product ?? string.Empty,
                    Status = status,
                    RawDevice = dev
                });
            }

            return result;
        }
        catch
        {
            return Array.Empty<ManagedAndroidDevice>();
        }
    }

    public async Task<ManagedAndroidDevice?> GetDeviceBySerialAsync(string serial, CancellationToken ct = default)
    {
        var devices = await ScanDevicesAsync(ct).ConfigureAwait(false);
        return devices.FirstOrDefault(d => string.Equals(d.Serial, serial, StringComparison.OrdinalIgnoreCase));
    }
}
