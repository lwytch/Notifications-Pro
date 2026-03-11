using NotificationsPro.Models;

namespace NotificationsPro.Helpers;

public static class StartupDefaultsHelper
{
    public static void ApplyFirstRunDisplayAwareDefaults(AppSettings settings, double primaryMonitorWorkAreaHeight)
    {
        if (settings == null)
            return;

        if (primaryMonitorWorkAreaHeight <= 0)
            return;

        settings.OverlayMaxHeight = Math.Clamp(
            primaryMonitorWorkAreaHeight,
            ViewModels.SettingsViewModel.OverlayMaxHeightMin,
            ViewModels.SettingsViewModel.OverlayMaxHeightMax);
    }
}
