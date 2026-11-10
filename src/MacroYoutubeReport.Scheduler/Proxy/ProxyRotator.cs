using System.Collections.Concurrent;
using System.Net;
using MacroYoutubeReport.Driver.Android.Adb;

namespace MacroYoutubeReport.Scheduler.Proxy;

/// <summary>
/// Quản lý danh sách Proxy và luân chuyển Round-Robin an toàn cho các tác vụ.
/// </summary>
public sealed class ProxyRotator : IProxyRotator
{
    private readonly List<ProxyConfig> _proxies = new();
    private readonly object _lock = new();
    private int _currentIndex = 0;

    public IReadOnlyList<ProxyConfig> GetAllProxies()
    {
        lock (_lock)
        {
            return _proxies.ToList();
        }
    }

    public void AddProxy(ProxyConfig proxy)
    {
        ArgumentNullException.ThrowIfNull(proxy);
        lock (_lock)
        {
            _proxies.Add(proxy);
        }
    }

    public void LoadProxiesFromLines(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        lock (_lock)
        {
            _proxies.Clear();
            foreach (var line in lines)
            {
                var cfg = ProxyConfig.TryParse(line);
                if (cfg != null)
                {
                    _proxies.Add(cfg);
                }
            }
        }
    }

    public ProxyConfig? GetNextProxy()
    {
        lock (_lock)
        {
            if (_proxies.Count == 0) return null;

            int attempts = 0;
            while (attempts < _proxies.Count)
            {
                int index = _currentIndex % _proxies.Count;
                _currentIndex++;
                attempts++;

                var candidate = _proxies[index];
                if (candidate.IsHealthy)
                {
                    return candidate;
                }
            }

            // Fallback: trả về proxy kế tiếp dù đang đánh dấu un-healthy để thử lại
            return _proxies[_currentIndex++ % _proxies.Count];
        }
    }

    public async Task<bool> CheckProxyHealthAsync(ProxyConfig proxy, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(proxy);

        try
        {
            var handler = new HttpClientHandler
            {
                Proxy = new WebProxy(proxy.Host, proxy.Port),
                UseProxy = true
            };

            if (!string.IsNullOrWhiteSpace(proxy.Username))
            {
                handler.Proxy.Credentials = new NetworkCredential(proxy.Username, proxy.Password);
            }

            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
            var response = await httpClient.GetAsync("https://www.google.com/generate_204", ct).ConfigureAwait(false);

            proxy.IsHealthy = response.IsSuccessStatusCode;
            proxy.LastCheckedAt = DateTime.UtcNow;
            return proxy.IsHealthy;
        }
        catch
        {
            proxy.IsHealthy = false;
            proxy.LastCheckedAt = DateTime.UtcNow;
            return false;
        }
    }

    public async Task ApplyProxyToAndroidDeviceAsync(string deviceSerial, ProxyConfig? proxy, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(deviceSerial)) return;

        var dev = await AdbDeviceManager.Instance.GetDeviceBySerialAsync(deviceSerial, ct).ConfigureAwait(false);
        if (dev == null) return;

        if (proxy != null && !string.IsNullOrWhiteSpace(proxy.Host))
        {
            string cmd = $"settings put global http_proxy {proxy.ToAndroidProxyString()}";
            await AdbClientGeneral.Instance.ExecuteShellAsync(dev.RawDevice, cmd, ct).ConfigureAwait(false);
        }
        else
        {
            await ClearProxyOnAndroidDeviceAsync(deviceSerial, ct).ConfigureAwait(false);
        }
    }

    public async Task ClearProxyOnAndroidDeviceAsync(string deviceSerial, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(deviceSerial)) return;

        var dev = await AdbDeviceManager.Instance.GetDeviceBySerialAsync(deviceSerial, ct).ConfigureAwait(false);
        if (dev == null) return;

        await AdbClientGeneral.Instance.ExecuteShellAsync(dev.RawDevice, "settings put global http_proxy :0", ct).ConfigureAwait(false);
    }
}
