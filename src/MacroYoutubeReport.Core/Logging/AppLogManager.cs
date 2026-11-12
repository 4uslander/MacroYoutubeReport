using System.Collections.Concurrent;
using System.Threading.Channels;

namespace MacroYoutubeReport.Core.Logging;

/// <summary>
/// Đại diện cho một bản ghi nhật ký có phân loại theo thiết bị và mức độ.
/// </summary>
public sealed class LogEntry
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public string Level { get; init; } = "INFO";
    public string Message { get; init; } = string.Empty;
    public string? DeviceSerial { get; init; }
    public string FormattedTimestamp => Timestamp.ToString("HH:mm:ss");

    public override string ToString()
    {
        string dev = string.IsNullOrWhiteSpace(DeviceSerial) ? "" : $"[{DeviceSerial}] ";
        return $"[{FormattedTimestamp}] [{Level}] {dev}{Message}";
    }
}

/// <summary>
/// Trình quản lý log hiệu năng cao, thread-safe:
/// - Buffer bất đồng bộ (Non-blocking Channel) ghi file nhật ký xoay vòng theo ngày (%LocalAppData%\MacroYoutubeReport\logs).
/// - Giữ ring-buffer bộ nhớ trong RAM (tối đa 2.000 dòng) phục vụ lọc đa chiều theo thiết bị/mức độ.
/// - Hỗ trợ rút hàng loạt (Drain) cho giao diện người dùng cập nhật theo chu kỳ mà không nghẽn UI Thread.
/// - Tự động dọn dẹp các tệp log và ảnh chụp cũ quá 7 ngày để tối ưu lưu trữ.
/// </summary>
public sealed class AppLogManager : IAsyncDisposable
{
    private static readonly Lazy<AppLogManager> _instance = new(() => new AppLogManager());
    public static AppLogManager Instance => _instance.Value;

    private const int MaxMemoryLogCount = 2000;
    private readonly ConcurrentQueue<LogEntry> _memoryLogs = new();
    private readonly ConcurrentQueue<LogEntry> _uiPendingQueue = new();

    private readonly Channel<LogEntry> _fileLogChannel;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _fileWriterTask;
    private readonly string _logDirectory;

    public string LogDirectory => _logDirectory;

    public AppLogManager()
    {
        _logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MacroYoutubeReport",
            "logs");

        Directory.CreateDirectory(_logDirectory);

        _fileLogChannel = Channel.CreateUnbounded<LogEntry>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        _fileWriterTask = Task.Run(() => ProcessFileLoggingLoopAsync(_cts.Token), CancellationToken.None);

        // Tự động dọn dẹp file cũ hơn 7 ngày trong luồng ngầm
        _ = Task.Run(CleanupOldLogsAndProofs, CancellationToken.None);
    }

    /// <summary>
    /// Ghi nhận 1 dòng log. Hàm này non-blocking, an toàn đa luồng 100%.
    /// </summary>
    public void Log(string message, string level = "INFO", string? deviceSerial = null)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level.ToUpperInvariant(),
            Message = message.Trim(),
            DeviceSerial = string.IsNullOrWhiteSpace(deviceSerial) ? null : deviceSerial.Trim()
        };

        // 1. Đưa vào Memory Ring Buffer
        _memoryLogs.Enqueue(entry);
        while (_memoryLogs.Count > MaxMemoryLogCount)
        {
            _memoryLogs.TryDequeue(out _);
        }

        // 2. Đưa vào Pending UI Queue cho WinForms Timer rút batch
        _uiPendingQueue.Enqueue(entry);

        // 3. Đưa vào Channel ghi đĩa ngầm
        _fileLogChannel.Writer.TryWrite(entry);
    }

    public void LogInfo(string message, string? deviceSerial = null) => Log(message, "INFO", deviceSerial);
    public void LogSuccess(string message, string? deviceSerial = null) => Log(message, "SUCCESS", deviceSerial);
    public void LogWarn(string message, string? deviceSerial = null) => Log(message, "WARN", deviceSerial);
    public void LogError(string message, string? deviceSerial = null) => Log(message, "ERROR", deviceSerial);

    /// <summary>
    /// Rút toàn bộ các log mới phát sinh để đưa lên giao diện một lần duy nhất theo nhịp Timer.
    /// </summary>
    public IReadOnlyList<LogEntry> DrainPendingLogs(int maxBatch = 100)
    {
        var list = new List<LogEntry>(Math.Min(_uiPendingQueue.Count, maxBatch));
        while (list.Count < maxBatch && _uiPendingQueue.TryDequeue(out var entry))
        {
            list.Add(entry);
        }
        return list;
    }

    /// <summary>
    /// Lấy danh sách log trong bộ nhớ phù hợp với bộ lọc thiết bị và cấp độ lỗi.
    /// Dùng khi người dùng chọn lọc thiết bị hoặc mức độ log trên UI.
    /// </summary>
    public IReadOnlyList<LogEntry> GetFilteredLogs(string? deviceSerialFilter, string? levelFilter, int maxCount = 1000)
    {
        bool hasDevFilter = !string.IsNullOrWhiteSpace(deviceSerialFilter) &&
                            !string.Equals(deviceSerialFilter, "ALL", StringComparison.OrdinalIgnoreCase) &&
                            !deviceSerialFilter.Contains("Tất cả", StringComparison.OrdinalIgnoreCase) &&
                            !deviceSerialFilter.Contains("All", StringComparison.OrdinalIgnoreCase);

        bool hasLevelFilter = !string.IsNullOrWhiteSpace(levelFilter) &&
                              !string.Equals(levelFilter, "ALL", StringComparison.OrdinalIgnoreCase) &&
                              !levelFilter.Contains("Tất cả", StringComparison.OrdinalIgnoreCase) &&
                              !levelFilter.Contains("All", StringComparison.OrdinalIgnoreCase);

        var query = _memoryLogs.AsEnumerable();

        if (hasDevFilter)
        {
            query = query.Where(l => string.Equals(l.DeviceSerial, deviceSerialFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (hasLevelFilter)
        {
            query = query.Where(l => string.Equals(l.Level, levelFilter, StringComparison.OrdinalIgnoreCase));
        }

        return query.TakeLast(maxCount).ToList();
    }

    /// <summary>
    /// Xóa toàn bộ nhật ký đang hiển thị trên RAM.
    /// </summary>
    public void ClearMemoryLogs()
    {
        while (_memoryLogs.TryDequeue(out _)) { }
        while (_uiPendingQueue.TryDequeue(out _)) { }
    }

    private async Task ProcessFileLoggingLoopAsync(CancellationToken ct)
    {
        var buffer = new List<LogEntry>(100);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Chờ ít nhất 1 phần tử
                if (await _fileLogChannel.Reader.WaitToReadAsync(ct).ConfigureAwait(false))
                {
                    while (_fileLogChannel.Reader.TryRead(out var item))
                    {
                        buffer.Add(item);
                        if (buffer.Count >= 200) break;
                    }

                    if (buffer.Count > 0)
                    {
                        await FlushToFileAsync(buffer).ConfigureAwait(false);
                        buffer.Clear();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                await Task.Delay(500, CancellationToken.None).ConfigureAwait(false);
            }
        }

        // Flush những log còn lại khi dừng ứng dụng
        while (_fileLogChannel.Reader.TryRead(out var remaining))
        {
            buffer.Add(remaining);
        }
        if (buffer.Count > 0)
        {
            await FlushToFileAsync(buffer).ConfigureAwait(false);
        }
    }

    private async Task FlushToFileAsync(IReadOnlyList<LogEntry> entries)
    {
        try
        {
            string fileName = $"app_{DateTime.UtcNow:yyyyMMdd}.log";
            string filePath = Path.Combine(_logDirectory, fileName);

            using var sw = new StreamWriter(filePath, append: true, System.Text.Encoding.UTF8);
            foreach (var e in entries)
            {
                string dev = string.IsNullOrWhiteSpace(e.DeviceSerial) ? "System" : e.DeviceSerial;
                await sw.WriteLineAsync($"[{e.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{e.Level,-7}] [{dev}] {e.Message}").ConfigureAwait(false);
            }
        }
        catch
        {
            // Bỏ qua lỗi ghi đĩa để không làm ảnh hưởng tác vụ chính
        }
    }

    /// <summary>
    /// Dọn dẹp các tệp log và ảnh chụp màn hình cũ quá 7 ngày để giải phóng dung lượng ổ cứng.
    /// </summary>
    public void CleanupOldLogsAndProofs()
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddDays(-7);

            // 1. Dọn tệp log cũ
            if (Directory.Exists(_logDirectory))
            {
                foreach (var file in Directory.GetFiles(_logDirectory, "*.log"))
                {
                    try
                    {
                        var info = new FileInfo(file);
                        if (info.LastWriteTimeUtc < cutoff)
                        {
                            info.Delete();
                        }
                    }
                    catch { }
                }
            }

            // 2. Dọn ảnh chụp cũ trong thư mục Screenshots
            string screenshotsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
            if (Directory.Exists(screenshotsDir))
            {
                foreach (var file in Directory.GetFiles(screenshotsDir, "*.png"))
                {
                    try
                    {
                        var info = new FileInfo(file);
                        if (info.LastWriteTimeUtc < cutoff)
                        {
                            info.Delete();
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _fileLogChannel.Writer.TryComplete();
        try
        {
            await _fileWriterTask.ConfigureAwait(false);
        }
        catch { }
        _cts.Dispose();
    }
}
