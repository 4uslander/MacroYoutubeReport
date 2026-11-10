using MacroYoutubeReport.Core.Enums;
using MacroYoutubeReport.Core.Localization;

namespace MacroYoutubeReport.Tests;

public class UiLanguageManagerTests
{
    [Theory]
    [InlineData("Nội dung khiêu dâm", ReportReasonCategory.SexualContent)]
    [InlineData("Spam hoặc gây hiểu nhầm", ReportReasonCategory.SpamOrMisleading)]
    [InlineData("Quấy rối hoặc bắt nạt", ReportReasonCategory.HarassmentOrBullying)]
    [InlineData("Hateful or abusive content", ReportReasonCategory.HatefulOrAbusive)]
    [InlineData("Harmful or dangerous acts", ReportReasonCategory.HarmfulDangerousActs)]
    [InlineData("스팸 또는 혼동을 야기하는 콘텐츠", ReportReasonCategory.SpamOrMisleading)]
    [InlineData("폭력적 또는 혐오스러운 콘텐츠", ReportReasonCategory.ViolentOrRepulsive)]
    [InlineData("잘못된 정보", ReportReasonCategory.Misinformation)]
    public void ResolveCategoryFromText_IdentifiesCorrectCategory(string text, ReportReasonCategory expected)
    {
        var result = UiLanguageManager.ResolveCategoryFromText(text);
        Assert.NotNull(result);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void GetReportMenuKeywords_ContainsCoreWords()
    {
        var viWords = UiLanguageManager.GetReportMenuKeywords(AppLanguage.Vietnamese);
        var enWords = UiLanguageManager.GetReportMenuKeywords(AppLanguage.English);
        var koWords = UiLanguageManager.GetReportMenuKeywords(AppLanguage.Korean);

        Assert.Contains("Báo cáo", viWords);
        Assert.Contains("Report", enWords);
        Assert.Contains("신고", koWords);
    }

    [Fact]
    public void GetSuccessConfirmationKeywords_ContainsThankYou()
    {
        var allSuccess = UiLanguageManager.GetSuccessConfirmationKeywords();
        Assert.Contains(allSuccess, k => k.Contains("Cảm ơn"));
        Assert.Contains(allSuccess, k => k.Contains("Thank") || k.Contains("Thanks"));
        Assert.Contains(allSuccess, k => k.Contains("감사합니다"));
    }
}
