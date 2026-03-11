using NotificationsPro.Helpers;

namespace NotificationsPro.Tests;

public class SpokenNotificationTextFormatterTests
{
    [Fact]
    public void NormalizeMode_UnknownValue_FallsBackToBodyOnly()
    {
        Assert.Equal(
            SpokenNotificationTextFormatter.ModeBodyOnly,
            SpokenNotificationTextFormatter.NormalizeMode("something else"));
    }

    [Fact]
    public void BuildText_BodyOnly_UsesNormalizedBodyText()
    {
        var result = SpokenNotificationTextFormatter.BuildText(
            "Title",
            "  Body text with \r\n extra spacing  ",
            DateTime.Now,
            SpokenNotificationTextFormatter.ModeBodyOnly,
            "Relative");

        Assert.Equal("Body text with extra spacing", result);
    }

    [Fact]
    public void BuildText_BodyOnly_FallsBackToTitle()
    {
        var result = SpokenNotificationTextFormatter.BuildText(
            "Fallback title",
            "",
            DateTime.Now,
            SpokenNotificationTextFormatter.ModeBodyOnly,
            "Relative");

        Assert.Equal("Fallback title", result);
    }

    [Fact]
    public void BuildText_TitleBodyTimestamp_UsesConfiguredTimestampMode()
    {
        var result = SpokenNotificationTextFormatter.BuildText(
            "Deployment complete",
            "Everything finished successfully",
            new DateTime(2026, 3, 11, 14, 30, 0),
            SpokenNotificationTextFormatter.ModeTitleBodyTimestamp,
            "Time");

        Assert.Equal("Deployment complete. Everything finished successfully. 14:30", result);
    }
}
