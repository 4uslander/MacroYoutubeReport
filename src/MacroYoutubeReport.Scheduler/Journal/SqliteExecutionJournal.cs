using Microsoft.Data.Sqlite;
using MacroYoutubeReport.Core.Enums;
using MacroYoutubeReport.Core.Models;

namespace MacroYoutubeReport.Scheduler.Journal;

/// <summary>
/// Triển khai nhật ký thực thi dựa trên SQLite, lưu trữ cục bộ bền vững.
/// </summary>
public sealed class SqliteExecutionJournal : IExecutionJournal
{
    private string _connectionString;
    private SqliteConnection? _connection;
    private readonly string _rawDbPath;

    public SqliteExecutionJournal(string dbPath = "report_journal.db")
    {
        _rawDbPath = dbPath;
        _connectionString = BuildConnectionString(ResolveSafeDatabasePath(dbPath));
    }

    private static string BuildConnectionString(string path)
    {
        return new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }

    private static string ResolveSafeDatabasePath(string dbPath)
    {
        try
        {
            // 1. Ưu tiên đường dẫn tại thư mục chứa file .exe của ứng dụng
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string fullPath = Path.IsPathRooted(dbPath) ? dbPath : Path.Combine(baseDir, dbPath);
            string? dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Kiểm tra quyền ghi thực tế trên thư mục
            string testFile = Path.Combine(dir ?? baseDir, $".write_test_{Guid.NewGuid():N}.tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);

            return fullPath;
        }
        catch
        {
            // 2. Fallback nếu thư mục ứng dụng bị chỉ đọc (ví dụ trong Program Files hoặc chạy chưa giải nén)
            try
            {
                string localAppData = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MacroYoutubeReport");
                Directory.CreateDirectory(localAppData);
                return Path.Combine(localAppData, Path.GetFileName(dbPath));
            }
            catch
            {
                // 3. Fallback cuối cùng: temp folder
                return Path.Combine(Path.GetTempPath(), Path.GetFileName(dbPath));
            }
        }
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_connection != null) return;

        try
        {
            _connection = new SqliteConnection(_connectionString);
            await _connection.OpenAsync(ct).ConfigureAwait(false);
        }
        catch (SqliteException)
        {
            // Fallback sang LocalApplicationData nếu đường dẫn ban đầu bị lỗi quyền truy cập file (SQLite Error 14)
            try
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MacroYoutubeReport",
                    Path.GetFileName(_rawDbPath));

                Directory.CreateDirectory(Path.GetDirectoryName(appDataPath)!);
                _connectionString = BuildConnectionString(appDataPath);
                _connection = new SqliteConnection(_connectionString);
                await _connection.OpenAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                // Fallback cứu cánh cuối cùng: In-Memory Database (đảm bảo ứng dụng KHÔNG BAO GIỜ bị crash khi khởi động)
                _connectionString = "Data Source=:memory:;Mode=Memory;Cache=Shared;";
                _connection = new SqliteConnection(_connectionString);
                await _connection.OpenAsync(ct).ConfigureAwait(false);
            }
        }

        const string createTableSql = @"
            CREATE TABLE IF NOT EXISTS ReportRecords (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TaskId TEXT NOT NULL,
                TargetId TEXT,
                TargetNameOrUrl TEXT,
                DeviceSerialOrProfile TEXT,
                Platform INTEGER,
                Category INTEGER,
                ReasonDetails TEXT,
                Status INTEGER,
                WatchDurationSeconds INTEGER,
                ScreenshotProofPath TEXT,
                ErrorMessage TEXT,
                CreatedAt TEXT NOT NULL,
                CompletedAt TEXT
            );
            CREATE INDEX IF NOT EXISTS idx_device_date ON ReportRecords(DeviceSerialOrProfile, CreatedAt);
        ";

        using var cmd = new SqliteCommand(createTableSql, _connection);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task RecordTaskAsync(ReportTaskRecord record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (_connection == null) await InitializeAsync(ct).ConfigureAwait(false);

        const string insertSql = @"
            INSERT INTO ReportRecords 
            (TaskId, TargetId, TargetNameOrUrl, DeviceSerialOrProfile, Platform, Category, ReasonDetails, Status, WatchDurationSeconds, ScreenshotProofPath, ErrorMessage, CreatedAt, CompletedAt)
            VALUES 
            (@TaskId, @TargetId, @TargetNameOrUrl, @Device, @Platform, @Category, @Details, @Status, @WatchSec, @Proof, @Error, @Created, @Completed);
        ";

        using var cmd = new SqliteCommand(insertSql, _connection);
        cmd.Parameters.AddWithValue("@TaskId", record.TaskId);
        cmd.Parameters.AddWithValue("@TargetId", (object?)record.TargetId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TargetNameOrUrl", (object?)record.TargetNameOrUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Device", (object?)record.DeviceSerialOrProfile ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Platform", (int)record.Platform);
        cmd.Parameters.AddWithValue("@Category", (int)record.Category);
        cmd.Parameters.AddWithValue("@Details", (object?)record.ReasonDetails ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", (int)record.Status);
        cmd.Parameters.AddWithValue("@WatchSec", record.WatchDurationSeconds);
        cmd.Parameters.AddWithValue("@Proof", (object?)record.ScreenshotProofPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Error", (object?)record.ErrorMessage ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Created", record.CreatedAt.ToString("o"));
        cmd.Parameters.AddWithValue("@Completed", record.CompletedAt?.ToString("o") ?? (object)DBNull.Value);

        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ReportTaskRecord>> GetRecentRecordsAsync(int limit = 100, CancellationToken ct = default)
    {
        if (_connection == null) await InitializeAsync(ct).ConfigureAwait(false);

        const string selectSql = @"
            SELECT Id, TaskId, TargetId, TargetNameOrUrl, DeviceSerialOrProfile, Platform, Category, ReasonDetails, Status, WatchDurationSeconds, ScreenshotProofPath, ErrorMessage, CreatedAt, CompletedAt
            FROM ReportRecords
            ORDER BY Id DESC
            LIMIT @Limit;
        ";

        using var cmd = new SqliteCommand(selectSql, _connection);
        cmd.Parameters.AddWithValue("@Limit", limit);

        var list = new List<ReportTaskRecord>();
        using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            list.Add(new ReportTaskRecord
            {
                Id = reader.GetInt64(0),
                TaskId = reader.GetString(1),
                TargetId = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                TargetNameOrUrl = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                DeviceSerialOrProfile = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                Platform = (PlatformType)reader.GetInt32(5),
                Category = (ReportReasonCategory)reader.GetInt32(6),
                ReasonDetails = reader.IsDBNull(7) ? null : reader.GetString(7),
                Status = (ExecutionStatus)reader.GetInt32(8),
                WatchDurationSeconds = reader.GetInt32(9),
                ScreenshotProofPath = reader.IsDBNull(10) ? null : reader.GetString(10),
                ErrorMessage = reader.IsDBNull(11) ? null : reader.GetString(11),
                CreatedAt = DateTime.Parse(reader.GetString(12)),
                CompletedAt = reader.IsDBNull(13) ? null : DateTime.Parse(reader.GetString(13))
            });
        }

        return list;
    }

    public async Task<int> GetTodayReportCountAsync(string deviceSerialOrProfile, CancellationToken ct = default)
    {
        if (_connection == null) await InitializeAsync(ct).ConfigureAwait(false);

        string todayStart = DateTime.UtcNow.Date.ToString("o");
        const string countSql = @"
            SELECT COUNT(*) FROM ReportRecords
            WHERE DeviceSerialOrProfile = @Device
              AND Status = @Status
              AND CreatedAt >= @TodayStart;
        ";

        using var cmd = new SqliteCommand(countSql, _connection);
        cmd.Parameters.AddWithValue("@Device", deviceSerialOrProfile);
        cmd.Parameters.AddWithValue("@Status", (int)ExecutionStatus.Succeeded);
        cmd.Parameters.AddWithValue("@TodayStart", todayStart);

        var val = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        return Convert.ToInt32(val);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
        }
    }
}
