using NotificationsPro.Models;
using NotificationsPro.Services;

namespace NotificationsPro.Helpers;

public static class StartupSettingsMigrationHelper
{
    private const int LegacyMaxVisibleNotifications = 3;
    private const double LegacyAnimationDurationMs = 300;
    private const double LegacyOverlayMaxHeight = 480;
    private const int CurrentMaxVisibleNotifications = 40;
    private const double CurrentAnimationDurationMs = 1200;
    private const double OverlayMaxHeightMin = 200;
    private const double OverlayMaxHeightMax = 4320;

    public static bool Apply(AppSettings settings, double? primaryWorkAreaHeight, bool hadExistingSettingsFile)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var changed = false;
        var effectiveHeight = GetEffectiveHeight(primaryWorkAreaHeight);

        if (!hadExistingSettingsFile)
        {
            changed |= SetIfDifferent(settings.MaxVisibleNotifications, CurrentMaxVisibleNotifications, value => settings.MaxVisibleNotifications = value);
            changed |= SetIfDifferent(settings.AnimationDurationMs, CurrentAnimationDurationMs, value => settings.AnimationDurationMs = value);
            changed |= SetIfDifferent(settings.OverlayMaxHeight, effectiveHeight, value => settings.OverlayMaxHeight = value);
            changed |= SetSchemaVersion(settings);
            return changed;
        }

        if (settings.SettingsSchemaVersion >= SettingsManager.CurrentSettingsSchemaVersion)
            return false;

        if (settings.MaxVisibleNotifications == LegacyMaxVisibleNotifications)
            changed |= SetIfDifferent(settings.MaxVisibleNotifications, CurrentMaxVisibleNotifications, value => settings.MaxVisibleNotifications = value);

        if (settings.AnimationDurationMs > 0 && settings.AnimationDurationMs <= LegacyAnimationDurationMs)
            changed |= SetIfDifferent(settings.AnimationDurationMs, CurrentAnimationDurationMs, value => settings.AnimationDurationMs = value);

        if (settings.OverlayMaxHeight <= LegacyOverlayMaxHeight)
            changed |= SetIfDifferent(settings.OverlayMaxHeight, effectiveHeight, value => settings.OverlayMaxHeight = value);

        changed |= SetSchemaVersion(settings);
        return changed;
    }

    private static double GetEffectiveHeight(double? primaryWorkAreaHeight)
    {
        if (!primaryWorkAreaHeight.HasValue || primaryWorkAreaHeight.Value <= 0)
            return Math.Clamp(LegacyOverlayMaxHeight, OverlayMaxHeightMin, OverlayMaxHeightMax);

        return Math.Clamp(primaryWorkAreaHeight.Value, OverlayMaxHeightMin, OverlayMaxHeightMax);
    }

    private static bool SetSchemaVersion(AppSettings settings)
    {
        if (settings.SettingsSchemaVersion == SettingsManager.CurrentSettingsSchemaVersion)
            return false;

        settings.SettingsSchemaVersion = SettingsManager.CurrentSettingsSchemaVersion;
        return true;
    }

    private static bool SetIfDifferent(int current, int next, Action<int> assign)
    {
        if (current == next)
            return false;

        assign(next);
        return true;
    }

    private static bool SetIfDifferent(double current, double next, Action<double> assign)
    {
        if (Math.Abs(current - next) < 0.001)
            return false;

        assign(next);
        return true;
    }
}
