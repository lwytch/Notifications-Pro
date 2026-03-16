using NotificationsPro.Services;

namespace NotificationsPro.Tests;

public class NotificationListenerTests
{
    [Fact]
    public void SplitCombinedBrowserToasts_SplitsTwoChromeNotifications()
    {
        var texts = new[]
        {
            "Google Chrome",
            "Reddit",
            "r/dotnet",
            "New comment",
            "Google Chrome",
            "X",
            "@user posted",
            "A short post body"
        };

        var split = NotificationListener.SplitCombinedBrowserToasts(texts);

        Assert.Equal(2, split.Count);
        Assert.Equal("Reddit", split[0].AppName);
        Assert.Equal("r/dotnet", split[0].Title);
        Assert.Contains("New comment", split[0].Body);
        Assert.Equal("X", split[1].AppName);
        Assert.Equal("@user posted", split[1].Title);
        Assert.Contains("A short post body", split[1].Body);
    }

    [Fact]
    public void SplitCombinedBrowserToasts_ReturnsEmpty_ForSingleNotification()
    {
        var texts = new[]
        {
            "Google Chrome",
            "Reddit",
            "New message"
        };

        var split = NotificationListener.SplitCombinedBrowserToasts(texts);

        Assert.Empty(split);
    }

    [Fact]
    public void SplitCombinedBrowserToasts_ReturnsEmpty_ForNonBrowserMarker()
    {
        var texts = new[]
        {
            "AppName",
            "One",
            "Body one",
            "AppName",
            "Two",
            "Body two"
        };

        var split = NotificationListener.SplitCombinedBrowserToasts(texts);

        Assert.Empty(split);
    }

    [Theory]
    [InlineData("Notifications Pro")]
    [InlineData("NotificationsPro")]
    public void ShouldIgnoreCapturedNotification_ReturnsTrue_ForSelfNotifications(string appName)
    {
        Assert.True(NotificationListener.ShouldIgnoreCapturedNotification(appName, "Startup", "Tray balloon"));
    }

    [Fact]
    public void ShouldIgnoreCapturedNotification_ReturnsFalse_ForExternalNotifications()
    {
        Assert.False(NotificationListener.ShouldIgnoreCapturedNotification("Slack", "Mention", "Hello"));
    }
}
