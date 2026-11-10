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

        var journal = new SqliteExecutionJournal();
        var accountManager = new AccountManager();
        var proxyRotator = new ProxyRotator();
        var driverFactory = new DefaultDriverFactory();

        var engine = new ReportSchedulerEngine(journal, accountManager, proxyRotator, driverFactory, maxConcurrency: 4);

        var mainForm = new MainForm();
        var presenter = new MainPresenter(mainForm, engine, journal, accountManager, proxyRotator);

        Application.Run(mainForm);
    }
}
