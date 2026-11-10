namespace MacroYoutubeReport.Driver.Android.Locators;

/// <summary>
/// Chứa toàn bộ hằng số Resource-ID, Content-Description và XPath cho YouTube Android App.
/// </summary>
public static class AndroidLocators
{
    public const string PackageYouTube = "com.google.android.youtube";

    // Thanh tìm kiếm
    public const string SearchButtonId = "com.google.android.youtube:id/menu_item_view";
    public const string SearchEditTextId = "com.google.android.youtube:id/search_edit_text";
    public const string SearchClearId = "com.google.android.youtube:id/search_clear_button";

    // Player controls & Settings
    public const string PlayerControlsContainerId = "com.google.android.youtube:id/controls_layout";
    public const string PlayerViewId = "com.google.android.youtube:id/player_view";
    public const string PlayerSettingsGearId = "com.google.android.youtube:id/player_settings_button";
    public const string OverflowMenuContentDesc = "More options";

    // Bottom sheet & Menu Report
    public const string BottomSheetContainerId = "com.google.android.youtube:id/design_bottom_sheet";
    public const string MenuTitleTextViewId = "com.google.android.youtube:id/title";
    public const string ReportListItemText = "Báo cáo";

    // Report dialog
    public const string ReportDialogContainerId = "android:id/content";
    public const string ReportRadioGroupXPath = "//android.widget.RadioGroup";
    public const string ReportRadioButtonXPath = "//android.widget.RadioButton";
    public const string ReportSubmitButtonId = "com.google.android.youtube:id/report_button";
    public const string ReportNextButtonText = "Tiếp theo";
    public const string ReportConfirmButtonText = "Báo cáo";
    public const string ReportReasonEditTextXPath = "//android.widget.EditText";

    // Popups dismissal
    public const string DismissButtonId = "com.google.android.youtube:id/dismiss_button";
    public const string NegativeButtonId = "android:id/button2";
    public const string SkipAdButtonId = "com.google.android.youtube:id/skip_ad_button";
    public const string ModernSkipAdButtonId = "com.google.android.youtube:id/modern_skip_ad_button";
}
