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

    private IReadOnlyList<ManagedAndroidDevice>? _cachedDevices;
    private DateTime _lastScanTime = DateTime.MinValue;
    private readonly SemaphoreSlim _scanLock = new(1, 1);

    public AdbClient Client => AdbServerManager.Instance.Client;

    public Task<IReadOnlyList<ManagedAndroidDevice>> ScanDevicesAsync(CancellationToken ct = default)
        => ScanDevicesAsync(forceRefresh: false, ct);

    public async Task<IReadOnlyList<ManagedAndroidDevice>> ScanDevicesAsync(bool forceRefresh, CancellationToken ct = default)
    {
        if (!forceRefresh && _cachedDevices != null && (DateTime.UtcNow - _lastScanTime).TotalSeconds < 3)
        {
            return _cachedDevices;
        }

        await _scanLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!forceRefresh && _cachedDevices != null && (DateTime.UtcNow - _lastScanTime).TotalSeconds < 3)
            {
                return _cachedDevices;
            }

            if (!AdbServerManager.Instance.EnsureServerRunning())
            {
                return Array.Empty<ManagedAndroidDevice>();
            }

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

            _cachedDevices = result;
            _lastScanTime = DateTime.UtcNow;
            return result;
        }
        catch
        {
            return _cachedDevices ?? Array.Empty<ManagedAndroidDevice>();
        }
        finally
        {
            _scanLock.Release();
        }
    }

    public async Task<ManagedAndroidDevice?> GetDeviceBySerialAsync(string serial, CancellationToken ct = default)
    {
        var devices = await ScanDevicesAsync(ct).ConfigureAwait(false);
        var match = devices.FirstOrDefault(d => string.Equals(d.Serial, serial, StringComparison.OrdinalIgnoreCase));
        if (match != null) return match;

        // Fallback: tìm theo chuỗi con (IP hoặc cổng) nếu serial có định dạng linh hoạt
        return devices.FirstOrDefault(d => d.Serial.EndsWith(serial, StringComparison.OrdinalIgnoreCase)
                                        || serial.EndsWith(d.Serial, StringComparison.OrdinalIgnoreCase)
                                        || d.Serial.Contains(serial, StringComparison.OrdinalIgnoreCase));
    }
}
