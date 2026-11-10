namespace MacroYoutubeReport.Scheduler.Proxy;

/// <summary>
/// Giao diện bộ quản lý và luân chuyển Proxy / IP.
/// </summary>
public interface IProxyRotator
{
    IReadOnlyList<ProxyConfig> GetAllProxies();
    void AddProxy(ProxyConfig proxy);
    void LoadProxiesFromLines(IEnumerable<string> lines);
    ProxyConfig? GetNextProxy();
    Task<bool> CheckProxyHealthAsync(ProxyConfig proxy, CancellationToken ct = default);
    Task ApplyProxyToAndroidDeviceAsync(string deviceSerial, ProxyConfig? proxy, CancellationToken ct = default);
    Task ClearProxyOnAndroidDeviceAsync(string deviceSerial, CancellationToken ct = default);
}
