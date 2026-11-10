using System.Collections.Concurrent;
using System.Text.Json;

namespace MacroYoutubeReport.Scheduler.Accounts;

/// <summary>
/// Quản lý kho tài khoản Google/YouTube tập trung, lưu trữ cấu hình file JSON và quản lý hạn mức an toàn.
/// </summary>
public sealed class AccountManager : IAccountManager
{
    private readonly ConcurrentDictionary<string, AccountProfile> _accounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _defaultStoragePath;
    private readonly object _fileLock = new();

    public AccountManager(string? storagePath = null)
    {
        _defaultStoragePath = storagePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "accounts.json");
    }

    public IReadOnlyList<AccountProfile> GetAllAccounts()
    {
        foreach (var acc in _accounts.Values)
        {
            acc.CheckAndResetCooldown();
        }
        return _accounts.Values.ToList();
    }

    public async Task LoadAccountsAsync(string? filePath = null, CancellationToken ct = default)
    {
        string path = filePath ?? _defaultStoragePath;
        if (!File.Exists(path)) return;

        try
        {
            string json;
            lock (_fileLock)
            {
                json = File.ReadAllText(path);
            }

            var loaded = JsonSerializer.Deserialize<List<AccountProfile>>(json);
            if (loaded != null)
            {
                _accounts.Clear();
                foreach (var item in loaded)
                {
                    _accounts[item.Id] = item;
                }
            }
        }
        catch
        {
            // Bỏ qua lỗi định dạng file tạm thời
        }
    }

    public async Task SaveAccountsAsync(string? filePath = null, CancellationToken ct = default)
    {
        string path = filePath ?? _defaultStoragePath;
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);

        string json = JsonSerializer.Serialize(_accounts.Values.ToList(), new JsonSerializerOptions { WriteIndented = true });
        lock (_fileLock)
        {
            File.WriteAllText(path, json);
        }
    }

    public void AddOrUpdateAccount(AccountProfile account)
    {
        ArgumentNullException.ThrowIfNull(account);
        _accounts[account.Id] = account;
    }

    public bool RemoveAccount(string accountId)
    {
        return _accounts.TryRemove(accountId, out _);
    }

    public AccountProfile? GetAvailableAccountForDevice(string deviceSerialOrProfile)
    {
        foreach (var acc in _accounts.Values)
        {
            acc.CheckAndResetCooldown();
            if (string.Equals(acc.AssignedDeviceSerialOrProfile, deviceSerialOrProfile, StringComparison.OrdinalIgnoreCase) && acc.IsAvailable)
            {
                return acc;
            }
        }

        // Nếu không có tài khoản gán cứng riêng cho thiết bị, tìm tài khoản chung khả dụng
        return GetNextAvailableAccount();
    }

    public AccountProfile? GetNextAvailableAccount()
    {
        foreach (var acc in _accounts.Values)
        {
            acc.CheckAndResetCooldown();
            if (acc.IsAvailable)
            {
                return acc;
            }
        }
        return null;
    }

    public void RecordReportCompleted(string accountId, int cooldownSeconds = 120)
    {
        if (_accounts.TryGetValue(accountId, out var acc))
        {
            acc.MarkUsed(cooldownSeconds);
        }
    }

    public void ResetAllDailyQuotas()
    {
        foreach (var acc in _accounts.Values)
        {
            acc.ResetDailyQuota();
        }
    }
}
