namespace MacroYoutubeReport.Driver.Chrome.Locators;

/// <summary>
/// Chứa toàn bộ CSS selector và URL cho YouTube trên trình duyệt Web Chrome.
/// </summary>
public static class ChromeLocators
{
    public const string YoutubeHomeUrl = "https://www.youtube.com/";
    public const string YoutubeSearchUrl = "https://www.youtube.com/results?search_query=";

    // Search bar
    public const string SearchInput = "input#search, ytd-searchbox input";
    public const string SearchButton = "button#search-icon-legacy";

    // Video container & elements
    public const string VideoRow = "ytd-video-renderer, ytd-rich-item-renderer";
    public const string VideoTitleLink = "#video-title";
    public const string ChannelNameLink = "#channel-info #channel-name a, #byline a";

    // Player & Report menu
    public const string Player = "#movie_player, video.html5-main-video";
    public const string MoreActionsButton = "ytd-menu-renderer button[aria-label='More actions'], ytd-menu-renderer button[aria-label='Thao tác khác'], ytd-menu-renderer button[aria-label='Tùy chọn khác']";
    public const string MenuDropdown = "tp-yt-iron-dropdown";
    public const string MenuItemReport = "ytd-menu-service-item-renderer";

    // Report modal
    public const string ReportModal = "yt-report-form-modal-renderer, tp-yt-paper-dialog";
    public const string ReportRadioOption = "tp-yt-paper-radio-button, ytd-report-details-form-renderer tp-yt-paper-radio-button";
    public const string ReportNextButton = "yt-button-renderer#next-button button, button[aria-label='Next'], button[aria-label='Tiếp theo']";
    public const string ReportSubmitButton = "yt-button-renderer#submit-button button, button[aria-label='Report'], button[aria-label='Báo cáo']";
    public const string ReportTextArea = "textarea#textarea, yt-formatted-string + textarea";
}
