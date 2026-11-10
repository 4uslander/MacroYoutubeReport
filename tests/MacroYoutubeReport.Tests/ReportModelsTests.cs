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

    [Fact]
    public void SearchFilterConfig_DefaultValues_HaveLiveOnlyAndToday()
    {
        var filter = new SearchFilterConfig();
        Assert.True(filter.LiveOnly);
        Assert.Equal(SearchUploadDateFilter.Today, filter.UploadDate);
        Assert.Equal(50, filter.MaxScrollAttempts);
        Assert.True(filter.HasActiveFilter());
    }

    [Fact]
    public void SearchFilterConfig_HasActiveFilter_ReturnsFalse_WhenEmpty()
    {
        var filter = new SearchFilterConfig
        {
            LiveOnly = false,
            UploadDate = SearchUploadDateFilter.All,
            Type = SearchTypeFilter.All,
            Duration = SearchDurationFilter.All,
            SortBy = SearchSortByFilter.Relevance,
            CommonsOnly = false,
            SubtitlesOnly = false,
            FourKOnly = false
        };
        Assert.False(filter.HasActiveFilter());
    }

    [Fact]
    public void ReportScenarioConfig_LiveStreamScenario_HasFilterConfigured()
    {
        var config = ReportScenarioConfig.CreateLiveStreamFinancialScamConfig();
        Assert.NotNull(config.SearchFilter);
        Assert.True(config.SearchFilter.LiveOnly);
        Assert.Equal(SearchUploadDateFilter.Today, config.SearchFilter.UploadDate);
    }
}
