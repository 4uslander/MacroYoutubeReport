using MacroYoutubeReport.Scheduler.Accounts;
using MacroYoutubeReport.Scheduler.Engine;
using MacroYoutubeReport.Scheduler.Journal;
using MacroYoutubeReport.Scheduler.Proxy;
using MacroYoutubeReport.UI.WinForms.Presenters;
using MacroYoutubeReport.UI.WinForms.Views;

namespace MacroYoutubeReport.UI.WinForms;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
        {
            MessageBox.Show($"Đã xảy ra lỗi ngoài ý muốn:\n{e.Exception.Message}", "Cảnh báo hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"Lỗi ứng dụng nghiêm trọng:\n{ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        var journal = new SqliteExecutionJournal();
        var accountManager = new AccountManager();
        var proxyRotator = new ProxyRotator();
        var driverFactory = new DefaultDriverFactory();

        var engine = new ReportSchedulerEngine(journal, accountManager, proxyRotator, driverFactory, maxConcurrency: 16);

        var mainForm = new MainForm();
        var presenter = new MainPresenter(mainForm, engine, journal, accountManager, proxyRotator);

        Application.Run(mainForm);
    }
}
