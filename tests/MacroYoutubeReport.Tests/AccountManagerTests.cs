using MacroYoutubeReport.Core.Enums;
using MacroYoutubeReport.Scheduler.Accounts;

namespace MacroYoutubeReport.Tests;

public class AccountManagerTests
{
    [Fact]
    public void AccountProfile_Availability_HonorsDailyQuotaAndCooldown()
    {
        var acc = new AccountProfile
        {
            Email = "test@gmail.com",
            MaxDailyReports = 2,
            TotalReportsToday = 0,
            Status = DeviceStatus.Online
        };

        Assert.True(acc.IsAvailable);

        acc.MarkUsed(cooldownSeconds: 60);
        Assert.Equal(1, acc.TotalReportsToday);
        Assert.Equal(DeviceStatus.Cooldown, acc.Status);
        Assert.False(acc.IsAvailable); // In cooldown

        acc.CooldownUntil = DateTime.UtcNow.AddSeconds(-1);
        acc.CheckAndResetCooldown();
        Assert.True(acc.IsAvailable); // Cooldown expired

        acc.MarkUsed(cooldownSeconds: 0);
        Assert.Equal(2, acc.TotalReportsToday);
        Assert.False(acc.IsAvailable); // Max quota reached
    }

    [Fact]
    public void AccountManager_GetAvailableAccountForDevice_FindsCorrectAccount()
    {
        var mgr = new AccountManager();
        var acc1 = new AccountProfile
        {
            Email = "dev1@gmail.com",
            AssignedDeviceSerialOrProfile = "device-01",
            Status = DeviceStatus.Online
        };
        var acc2 = new AccountProfile
        {
            Email = "dev2@gmail.com",
            AssignedDeviceSerialOrProfile = "device-02",
            Status = DeviceStatus.Online
        };

        mgr.AddOrUpdateAccount(acc1);
        mgr.AddOrUpdateAccount(acc2);

        var found = mgr.GetAvailableAccountForDevice("device-01");
        Assert.NotNull(found);
        Assert.Equal("dev1@gmail.com", found.Email);

        var accounts = mgr.GetAllAccounts();
        Assert.Equal(2, accounts.Count);
    }
}
