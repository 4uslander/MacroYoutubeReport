using AdvancedSharpAdbClient;
using System.Diagnostics;

namespace MacroYoutubeReport.Driver.Android.Adb;

/// <summary>
/// Quản lý vòng đời tiến trình ADB Server cục bộ (127.0.0.1:5037).
/// </summary>
public sealed class AdbServerManager
{
    public const int DefaultAdbPort = 5037;
    public const string DefaultAdbHost = "127.0.0.1";

    private static readonly Lazy<AdbServerManager> _instance = new(() => new AdbServerManager());
    public static AdbServerManager Instance => _instance.Value;

    public AdbClient Client { get; private set; }

    public AdbServerManager()
    {
        Client = new AdbClient();
    }

    /// <summary>
    /// Đảm bảo ADB server đang chạy và sẵn sàng kết nối.
    /// </summary>
    public bool EnsureServerRunning()
    {
        try
        {
            var version = Client.GetAdbVersion();
            if (version > 0)
            {
                return true;
            }
        }
        catch
        {
            // Server chưa chạy, thử khởi động
        }

        return TryStartLocalAdbServer();
    }

    private bool TryStartLocalAdbServer()
    {
        string? adbExePath = ResolveAdbPath();
        if (string.IsNullOrWhiteSpace(adbExePath))
        {
            return false;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = adbExePath,
                Arguments = "start-server",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            process?.WaitForExit(10000);

            Client = new AdbClient();
            return Client.GetAdbVersion() > 0;
        }
        catch
        {
            return false;
        }
    }

    private static string? ResolveAdbPath()
    {
        // 1. Kiểm tra thư mục chạy của app
        string appDirAdb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "adb.exe");
        if (File.Exists(appDirAdb)) return appDirAdb;

        // 2. Kiểm tra trong PATH hệ thống
        string? pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrWhiteSpace(pathEnv))
        {
            foreach (string dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = Path.Combine(dir.Trim(), "adb.exe");
                if (File.Exists(candidate)) return candidate;
            }
        }

        // 3. Fallback thư mục tools trong AutoYouTube nếu có
        string autoYoutubeAdb = @"C:\Project Tools\GIT\AutoYouTube\AutoYouTube\AutoYouTube\adb.exe";
        if (File.Exists(autoYoutubeAdb)) return autoYoutubeAdb;

        return null;
    }
}
