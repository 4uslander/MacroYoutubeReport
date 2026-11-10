using MacroYoutubeReport.Core.Enums;
using MacroYoutubeReport.Core.Models;
using MacroYoutubeReport.Scheduler.Journal;

namespace MacroYoutubeReport.Tests;

public class SqliteJournalTests
{
    [Fact]
    public async Task SqliteExecutionJournal_CanRecordAndQueryTasks()
    {
        string tempDb = Path.Combine(Path.GetTempPath(), $"test_journal_{Guid.NewGuid():N}.db");
        try
        {
            await using var journal = new SqliteExecutionJournal(tempDb);
            await journal.InitializeAsync();

            var record = new ReportTaskRecord
            {
                TargetId = "vid-001",
                TargetNameOrUrl = "Test Video",
                DeviceSerialOrProfile = "device-emulator-5554",
                Platform = PlatformType.Android,
                Category = ReportReasonCategory.SpamOrMisleading,
                ReasonDetails = "Spam report test",
                Status = ExecutionStatus.Succeeded,
                WatchDurationSeconds = 45,
                ScreenshotProofPath = "Screenshots/test.png",
                CompletedAt = DateTime.UtcNow
            };

            await journal.RecordTaskAsync(record);

            var recent = await journal.GetRecentRecordsAsync(10);
            Assert.NotEmpty(recent);
            Assert.Equal("vid-001", recent[0].TargetId);
            Assert.Equal(ExecutionStatus.Succeeded, recent[0].Status);

            int count = await journal.GetTodayReportCountAsync("device-emulator-5554");
            Assert.Equal(1, count);
        }
        finally
        {
            if (File.Exists(tempDb))
            {
                try { File.Delete(tempDb); } catch { }
            }
        }
    }
}
