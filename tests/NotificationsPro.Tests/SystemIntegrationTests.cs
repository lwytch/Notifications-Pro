using NotificationsPro.Helpers;
using NotificationsPro.Models;
using NotificationsPro.Services;
using NotificationsPro.ViewModels;

namespace NotificationsPro.Tests;

public class SystemIntegrationTests
{
    // --- AppSettings: M9 default values ---

    [Fact]
    public void DefaultSettings_StartWithWindows_IsFalse()
    {
        var s = new AppSettings();
        Assert.False(s.StartWithWindows);
    }

    [Fact]
    public void DefaultSettings_SelectedMonitorIndex_IsZero()
    {
        var s = new AppSettings();
        Assert.Equal(0, s.SelectedMonitorIndex);
    }

    // --- AppSettings: M9 properties persist through clone ---

    [Fact]
    public void Clone_PreservesM9Properties()
    {
        var s = new AppSettings
        {
            StartWithWindows = true,
            SelectedMonitorIndex = 2
        };

        var clone = s.Clone();
        Assert.True(clone.StartWithWindows);
        Assert.Equal(2, clone.SelectedMonitorIndex);
    }

    // --- AppSettings: M9 properties round-trip through JSON ---

    [Fact]
    public void SettingsRoundTrip_M9Properties()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"NotificationsPro_Test_{Guid.NewGuid():N}");
        try
        {
            var sm = new SettingsManager(tempDir);
            sm.Settings.StartWithWindows = true;
            sm.Settings.SelectedMonitorIndex = 1;
            sm.Save();

            var sm2 = new SettingsManager(tempDir);
            sm2.Load();
            Assert.True(sm2.Settings.StartWithWindows);
            Assert.Equal(1, sm2.Settings.SelectedMonitorIndex);
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    // --- StartupHelper ---

    [Fact]
    public async Task StartupHelper_IsStartupEnabled_DoesNotThrow()
    {
        // Just verify the method works without throwing
        var result = await StartupHelper.IsStartupEnabledAsync();
        Assert.IsType<bool>(result);
    }

    // --- MonitorInfo ---

    [Fact]
    public void MonitorInfo_ToString_ReturnsDisplayName()
    {
        var info = new MonitorInfo(0, "Monitor 1: 1920x1080 (Primary)", true);
        Assert.Equal("Monitor 1: 1920x1080 (Primary)", info.ToString());
        Assert.Equal(0, info.Index);
        Assert.True(info.IsPrimary);
    }

    [Fact]
    public void MonitorInfo_NonPrimary()
    {
        var info = new MonitorInfo(1, "Monitor 2: 2560x1440", false);
        Assert.Equal("Monitor 2: 2560x1440", info.ToString());
        Assert.Equal(1, info.Index);
        Assert.False(info.IsPrimary);
    }
}
