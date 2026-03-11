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

    [Theory]
    [InlineData(SpokenNotificationTextFormatter.ModeTitleOnly)]
    [InlineData(SpokenNotificationTextFormatter.ModeTitleBody)]
    [InlineData(SpokenNotificationTextFormatter.ModeBodyTimestamp)]
    [InlineData(SpokenNotificationTextFormatter.ModeTitleTimestamp)]
    [InlineData(SpokenNotificationTextFormatter.ModeTitleBodyTimestamp)]
    public void NormalizeMode_KnownValues_ArePreserved(string mode)
    {
        Assert.Equal(mode, SpokenNotificationTextFormatter.NormalizeMode(mode));
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
    public void BuildText_TitleOnly_UsesTitle()
    {
        var result = SpokenNotificationTextFormatter.BuildText(
            "Deployment complete",
            "Everything finished successfully",
            DateTime.Now,
            SpokenNotificationTextFormatter.ModeTitleOnly,
            "Relative");

        Assert.Equal("Deployment complete", result);
    }

    [Fact]
    public void BuildText_TitleBody_UsesTitleAndBody()
    {
        var result = SpokenNotificationTextFormatter.BuildText(
            "Deployment complete",
            "Everything finished successfully",
            DateTime.Now,
            SpokenNotificationTextFormatter.ModeTitleBody,
            "Relative");

        Assert.Equal("Deployment complete. Everything finished successfully", result);
    }

    [Fact]
    public void BuildText_BodyTimestamp_UsesBodyAndTimestamp()
    {
        var result = SpokenNotificationTextFormatter.BuildText(
            "Deployment complete",
            "Everything finished successfully",
            new DateTime(2026, 3, 11, 14, 30, 0),
            SpokenNotificationTextFormatter.ModeBodyTimestamp,
            "Time");

        Assert.Equal("Everything finished successfully. 14:30", result);
    }

    [Fact]
    public void BuildText_TitleTimestamp_UsesTitleAndTimestamp()
    {
        var result = SpokenNotificationTextFormatter.BuildText(
            "Deployment complete",
            "Everything finished successfully",
            new DateTime(2026, 3, 11, 14, 30, 0),
            SpokenNotificationTextFormatter.ModeTitleTimestamp,
            "Time");

        Assert.Equal("Deployment complete. 14:30", result);
    }

    [Fact]
    public void BuildText_TitleOnly_FallsBackToBody()
    {
        var result = SpokenNotificationTextFormatter.BuildText(
            "",
            "Everything finished successfully",
            DateTime.Now,
            SpokenNotificationTextFormatter.ModeTitleOnly,
            "Relative");

        Assert.Equal("Everything finished successfully", result);
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
