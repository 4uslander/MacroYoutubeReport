namespace MacroYoutubeReport.Core.Models;

/// <summary>
/// Kết quả thực thi của một bước kịch bản báo cáo.
/// </summary>
public sealed class ReportStepResult
{
    public bool IsSuccess { get; }
    public string StepName { get; }
    public string Message { get; }
    public string? ScreenshotPath { get; }
    public DateTime Timestamp { get; }
    public string? ErrorDetails { get; }

    private ReportStepResult(bool isSuccess, string stepName, string message, string? screenshotPath = null, string? errorDetails = null)
    {
        IsSuccess = isSuccess;
        StepName = stepName;
        Message = message;
        ScreenshotPath = screenshotPath;
        Timestamp = DateTime.UtcNow;
        ErrorDetails = errorDetails;
    }

    public static ReportStepResult Success(string stepName, string message, string? screenshot = null) =>
        new(true, stepName, message, screenshot);

    public static ReportStepResult Failure(string stepName, string message, string? errorDetails = null, string? screenshot = null) =>
        new(false, stepName, message, screenshot, errorDetails);

    public static ReportStepResult Skipped(string stepName, string reason) =>
        new(true, stepName, $"Bỏ qua: {reason}");

    public override string ToString() =>
        $"[{(IsSuccess ? "OK" : "FAIL")}] {StepName}: {Message}";
}
