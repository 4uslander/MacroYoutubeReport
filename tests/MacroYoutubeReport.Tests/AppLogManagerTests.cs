using MacroYoutubeReport.Core.Logging;
using Xunit;

namespace MacroYoutubeReport.Tests;

public sealed class AppLogManagerTests
{
    [Fact]
    public void AppLogManager_LogAndDrain_WorksCorrectly()
    {
        var logger = AppLogManager.Instance;
        logger.ClearMemoryLogs();

        logger.LogInfo("Khởi động hệ thống", "dev-01");
        logger.LogWarn("Cảnh báo mạng", "dev-02");
        logger.LogError("Lỗi kết nối", "dev-01");

        var pending = logger.DrainPendingLogs(10);
        Assert.Equal(3, pending.Count);

        var dev01Logs = logger.GetFilteredLogs("dev-01", null);
        Assert.Equal(2, dev01Logs.Count);

        var errorLogs = logger.GetFilteredLogs(null, "ERROR");
        Assert.Single(errorLogs);
        Assert.Equal("Lỗi kết nối", errorLogs[0].Message);
    }

    [Fact]
    public void AppLogManager_MemoryCap_DoesNotExceedMax()
    {
        var logger = AppLogManager.Instance;
        logger.ClearMemoryLogs();

        for (int i = 0; i < 2500; i++)
        {
            logger.LogInfo($"Log index {i}", $"dev-{i % 10}");
        }

        var allLogs = logger.GetFilteredLogs("Tất cả thiết bị", "Tất cả");
        Assert.True(allLogs.Count <= 2000, $"Expected at most 2000 logs in memory, got {allLogs.Count}");
    }
}
