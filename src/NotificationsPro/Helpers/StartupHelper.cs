using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace NotificationsPro.Helpers;

public static class StartupHelper
{
    private const string StartupTaskId = "NotificationsProStartupId";

    public static async Task<bool> IsStartupEnabledAsync()
    {
        try
        {
            var startupTask = await StartupTask.GetAsync(StartupTaskId);
            return startupTask.State == StartupTaskState.Enabled;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> EnableStartupAsync()
    {
        try
        {
            var startupTask = await StartupTask.GetAsync(StartupTaskId);
            var state = await startupTask.RequestEnableAsync();
            return state == StartupTaskState.Enabled;
        }
        catch
        {
            return false;
        }
    }

    public static async Task DisableStartupAsync()
    {
        try
        {
            var startupTask = await StartupTask.GetAsync(StartupTaskId);
            startupTask.Disable();
        }
        catch
        {
            // Silently fail
        }
    }
}
