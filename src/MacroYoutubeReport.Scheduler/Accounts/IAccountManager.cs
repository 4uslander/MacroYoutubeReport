namespace MacroYoutubeReport.Scheduler.Accounts;

/// <summary>
/// Giao diện quản lý danh sách tài khoản, phân bổ thiết bị và kiểm soát hạn ngạch (Quota).
/// </summary>
public interface IAccountManager
{
    IReadOnlyList<AccountProfile> GetAllAccounts();
    Task LoadAccountsAsync(string? filePath = null, CancellationToken ct = default);
    Task SaveAccountsAsync(string? filePath = null, CancellationToken ct = default);
    void AddOrUpdateAccount(AccountProfile account);
    bool RemoveAccount(string accountId);
    AccountProfile? GetAvailableAccountForDevice(string deviceSerialOrProfile);
    AccountProfile? GetNextAvailableAccount();
    void RecordReportCompleted(string accountId, int cooldownSeconds = 120);
    void ResetAllDailyQuotas();
}
