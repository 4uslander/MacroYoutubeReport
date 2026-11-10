using MacroYoutubeReport.Core.Enums;
using MacroYoutubeReport.Core.Models;

namespace MacroYoutubeReport.Tests;

public class ReportModelsTests
{
    [Fact]
    public void ReportTargetItem_Matches_TitleAndChannel_Correctly()
    {
        var target = new ReportTargetItem
        {
            Keyword = "tin tuc",
            VideoTitle = "Tin Nong Trong Ngay",
            ChannelName = "Kenh Thoi Su"
        };

        Assert.True(target.Matches("Tin Nong Trong Ngay 24h", "Kenh Thoi Su Official"));
        Assert.False(target.Matches("Video khac", "Kenh Thoi Su Official"));
        Assert.False(target.Matches("Tin Nong Trong Ngay", "Kenh Khac"));
    }

    [Fact]
    public void ReportTargetItem_Matches_ByUrl()
    {
        var target = new ReportTargetItem
        {
            TargetUrl = "https://www.youtube.com/watch?v=abc123xyz"
        };

        Assert.True(target.Matches("Any Title", "Any Channel", "https://www.youtube.com/watch?v=abc123xyz"));
        Assert.False(target.Matches("Any Title", "Any Channel", "https://www.youtube.com/watch?v=different"));
    }

    [Fact]
    public void ReportScenarioConfig_GetNextReason_RotatesAndSpinsText()
    {
        var config = new ReportScenarioConfig
        {
            RotateReasons = true,
            ReasonPool = new()
            {
                new ReportReasonConfig(ReportReasonCategory.SpamOrMisleading) { SpinTextEnabled = true }
            },
            DetailTextTemplates = new()
            {
                "Template text 1",
                "Template text 2"
            }
        };

        var reason = config.GetNextReason();
        Assert.Equal(ReportReasonCategory.SpamOrMisleading, reason.Category);
        Assert.Contains(reason.CustomDetails, config.DetailTextTemplates);
    }
}
