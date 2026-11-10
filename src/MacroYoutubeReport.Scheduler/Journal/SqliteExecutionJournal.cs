using Microsoft.Data.Sqlite;
using MacroYoutubeReport.Core.Enums;
using MacroYoutubeReport.Core.Models;

namespace MacroYoutubeReport.Scheduler.Journal;

/// <summary>
/// Triển khai nhật ký thực thi dựa trên SQLite, lưu trữ cục bộ bền vững.
/// </summary>
public sealed class SqliteExecutionJournal : IExecutionJournal
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;

    public SqliteExecutionJournal(string dbPath = "report_journal.db")
    {
        _connectionString = $"Data Source={dbPath};";
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_connection != null) return;

        _connection = new SqliteConnection(_connectionString);
        await _connection.OpenAsync(ct).ConfigureAwait(false);

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
