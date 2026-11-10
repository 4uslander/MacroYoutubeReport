using MacroYoutubeReport.Core.Enums;

namespace MacroYoutubeReport.Scheduler.Accounts;

/// <summary>
/// Đại diện cho một tài khoản Google/YouTube dùng để thực hiện báo cáo vi phạm.
/// </summary>
public sealed class AccountProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? RecoveryEmail { get; set; }
    public string? AssignedDeviceSerialOrProfile { get; set; }
    public string? AssignedProxyId { get; set; }

    public DeviceStatus Status { get; set; } = DeviceStatus.Online;
    public int TotalReportsToday { get; set; } = 0;
    public int MaxDailyReports { get; set; } = 8;
    public DateTime? LastUsedAt { get; set; }
    public DateTime? CooldownUntil { get; set; }
    public string? Notes { get; set; }

    /// <summary>
    /// Kiểm tra xem tài khoản có sẵn sàng nhận nhiệm vụ báo cáo tiếp theo không.
    /// </summary>
    public bool IsAvailable =>
        Status != DeviceStatus.Busy &&
        Status != DeviceStatus.Unauthorized &&
        Status != DeviceStatus.Error &&
        TotalReportsToday < MaxDailyReports &&
        (!CooldownUntil.HasValue || DateTime.UtcNow >= CooldownUntil.Value);

    public void MarkUsed(int cooldownSeconds = 120)
    {
        TotalReportsToday++;
        LastUsedAt = DateTime.UtcNow;
        if (cooldownSeconds > 0)
        {
            CooldownUntil = DateTime.UtcNow.AddSeconds(cooldownSeconds);
            Status = DeviceStatus.Cooldown;
        }
    }

    public void CheckAndResetCooldown()
    {
        if (CooldownUntil.HasValue && DateTime.UtcNow >= CooldownUntil.Value && Status == DeviceStatus.Cooldown)
        {
            Status = DeviceStatus.Online;
            CooldownUntil = null;
        }
    }

    public void ResetDailyQuota()
    {
        TotalReportsToday = 0;
        CooldownUntil = null;
        if (Status == DeviceStatus.Cooldown)
        {
            Status = DeviceStatus.Online;
        }
    }
}
