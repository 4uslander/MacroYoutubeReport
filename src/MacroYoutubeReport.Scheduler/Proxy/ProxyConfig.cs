namespace MacroYoutubeReport.Scheduler.Proxy;

/// <summary>
/// Cấu hình máy chủ Proxy (HTTP/SOCKS5) dùng để đổi IP cho phiên chạy.
/// </summary>
public sealed class ProxyConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 8080;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string Protocol { get; set; } = "http"; // "http", "socks5"
    public bool IsHealthy { get; set; } = true;
    public DateTime? LastCheckedAt { get; set; }
    public string? OutgoingIp { get; set; }

    public ProxyConfig() { }

    public ProxyConfig(string host, int port, string? user = null, string? pass = null, string protocol = "http")
    {
        Host = host;
        Port = port;
        Username = user;
        Password = pass;
        Protocol = protocol;
    }

    /// <summary>
    /// Parse chuỗi định dạng host:port hoặc host:port:user:pass.
    /// </summary>
    public static ProxyConfig? TryParse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var parts = raw.Trim().Split(new[] { ':', '@' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return null;

        string host = parts[0];
        if (!int.TryParse(parts[1], out int port)) return null;

        string? user = parts.Length >= 4 ? parts[2] : null;
        string? pass = parts.Length >= 4 ? parts[3] : null;

        return new ProxyConfig(host, port, user, pass);
    }

    /// <summary>
    /// Xuất chuỗi định dạng URL cho trình duyệt Chrome hoặc HTTP Client.
    /// </summary>
    public string ToProxyUrl()
    {
        if (!string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password))
        {
            return $"{Protocol}://{Username}:{Password}@{Host}:{Port}";
        }
        return $"{Protocol}://{Host}:{Port}";
    }

    /// <summary>
    /// Chuỗi định dạng cho Android command: host:port.
    /// </summary>
    public string ToAndroidProxyString() => $"{Host}:{Port}";

    public override string ToString() => ToProxyUrl();
}
