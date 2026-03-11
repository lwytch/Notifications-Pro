using System.Globalization;
using NotificationsPro.Helpers;

namespace NotificationsPro.Tests;

public class VoiceAccessTextFormatterTests
{
    [Fact]
    public void BuildAutomationName_Off_ReturnsGenericNotificationLabel()
    {
        var result = VoiceAccessTextFormatter.BuildAutomationName(
            "Title",
            "Body",
            "just now",
            new DateTime(2026, 3, 11, 14, 30, 0),
            VoiceAccessTextFormatter.ModeOff,
            "Relative",
            CultureInfo.InvariantCulture);

        Assert.Equal("Notification", result);
    }

    [Fact]
    public void BuildAutomationName_BodyOnly_UsesBodyText()
    {
        var result = VoiceAccessTextFormatter.BuildAutomationName(
            "Title",
            "  Body text with \r\n extra spacing  ",
            "just now",
            new DateTime(2026, 3, 11, 14, 30, 0),
            VoiceAccessTextFormatter.ModeBodyOnly,
            "Relative",
            CultureInfo.InvariantCulture);

        Assert.Equal("Body text with extra spacing", result);
    }

    [Fact]
    public void BuildAutomationName_BodyOnly_FallsBackToTitle()
    {
        var result = VoiceAccessTextFormatter.BuildAutomationName(
            "Fallback title",
            "",
            "just now",
            new DateTime(2026, 3, 11, 14, 30, 0),
            VoiceAccessTextFormatter.ModeBodyOnly,
            "Relative",
            CultureInfo.InvariantCulture);

        Assert.Equal("Fallback title", result);
    }

    [Fact]
    public void BuildAutomationName_TitleBodyTimestamp_UsesCurrentTimestampMode()
    {
        var result = VoiceAccessTextFormatter.BuildAutomationName(
            "Deployment complete",
            "Everything finished successfully",
            "just now",
            new DateTime(2026, 3, 11, 14, 30, 0),
            VoiceAccessTextFormatter.ModeTitleBodyTimestamp,
            "Time",
            CultureInfo.InvariantCulture);

        Assert.Equal("Deployment complete. Everything finished successfully. 14:30", result);
    }

    [Fact]
    public void IncludesTimestamp_IsOnlyTrueForFullMode()
    {
        Assert.False(VoiceAccessTextFormatter.IncludesTimestamp(VoiceAccessTextFormatter.ModeOff));
        Assert.False(VoiceAccessTextFormatter.IncludesTimestamp(VoiceAccessTextFormatter.ModeBodyOnly));
        Assert.True(VoiceAccessTextFormatter.IncludesTimestamp(VoiceAccessTextFormatter.ModeTitleBodyTimestamp));
    }
}
