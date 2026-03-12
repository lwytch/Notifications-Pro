using NotificationsPro.Helpers;
using NotificationsPro.Models;
using NotificationsPro.Services;

namespace NotificationsPro.Tests;

public class SpokenNotificationServiceTests
{
    [Fact]
    public void ShouldSpeakItem_AllAllowedNotifications_SpeaksUnmutedNotificationWithoutRule()
    {
        var settings = new AppSettings
        {
            ReadNotificationsAloudTriggerMode = NarrationTriggerModeHelper.AllAllowedNotifications
        };
        var item = new NotificationItem("Slack", "Build", "Completed");

        var result = SpokenNotificationService.ShouldSpeakItem(item, settings);

        Assert.True(result);
    }

    [Fact]
    public void ShouldSpeakItem_AllAllowedNotifications_HonorsAppMuteWhenNoRuleOverride()
    {
        var settings = new AppSettings
        {
            ReadNotificationsAloudTriggerMode = NarrationTriggerModeHelper.AllAllowedNotifications,
            SpokenMutedApps = new() { "Slack" }
        };
        var item = new NotificationItem("Slack", "Build", "Completed");

        var result = SpokenNotificationService.ShouldSpeakItem(item, settings);

        Assert.False(result);
    }

    [Fact]
    public void ShouldSpeakItem_AllAllowedNotifications_HonorsRuleOverride()
    {
        var settings = new AppSettings
        {
            ReadNotificationsAloudTriggerMode = NarrationTriggerModeHelper.AllAllowedNotifications,
            SpokenMutedApps = new() { "Slack" }
        };
        var item = new NotificationItem("Slack", "Build", "Completed")
        {
            ReadAloudEnabledOverride = true
        };

        var result = SpokenNotificationService.ShouldSpeakItem(item, settings);

        Assert.True(result);
    }

    [Fact]
    public void ShouldSpeakItem_RulesOnly_RequiresReadAloudRuleMatch()
    {
        var settings = new AppSettings
        {
            ReadNotificationsAloudTriggerMode = NarrationTriggerModeHelper.OnlyMatchingNarrationRules
        };
        var item = new NotificationItem("Slack", "Build", "Completed");

        var result = SpokenNotificationService.ShouldSpeakItem(item, settings);

        Assert.False(result);
    }

    [Fact]
    public void ShouldSpeakItem_RulesOnly_SpeaksOnlyExplicitReadAloudMatches()
    {
        var settings = new AppSettings
        {
            ReadNotificationsAloudTriggerMode = NarrationTriggerModeHelper.OnlyMatchingNarrationRules,
            SpokenMutedApps = new() { "Slack" }
        };
        var item = new NotificationItem("Slack", "Build", "Completed")
        {
            ReadAloudEnabledOverride = true
        };

        var result = SpokenNotificationService.ShouldSpeakItem(item, settings);

        Assert.True(result);
    }

    [Fact]
    public void ShouldSpeakItem_RulesOnly_SkipRuleSuppressesSpeech()
    {
        var settings = new AppSettings
        {
            ReadNotificationsAloudTriggerMode = NarrationTriggerModeHelper.OnlyMatchingNarrationRules
        };
        var item = new NotificationItem("Slack", "Build", "Completed")
        {
            ReadAloudEnabledOverride = false
        };

        var result = SpokenNotificationService.ShouldSpeakItem(item, settings);

        Assert.False(result);
    }
}
