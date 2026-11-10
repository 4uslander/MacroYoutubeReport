using MacroYoutubeReport.Core.Models;

namespace MacroYoutubeReport.Scheduler.Journal;

/// <summary>
/// Giao diện lưu trữ nhật ký thực thi và thống kê tác vụ báo cáo.
/// </summary>
public interface IExecutionJournal : IAsyncDisposable
{
    Task InitializeAsync(CancellationToken ct = default);
    Task RecordTaskAsync(ReportTaskRecord record, CancellationToken ct = default);
    Task<IReadOnlyList<ReportTaskRecord>> GetRecentRecordsAsync(int limit = 100, CancellationToken ct = default);
    Task<int> GetTodayReportCountAsync(string deviceSerialOrProfile, CancellationToken ct = default);
}
