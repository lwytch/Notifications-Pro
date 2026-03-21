using System.IO;
using NotificationsPro.Helpers;

namespace NotificationsPro.Tests;

public class SafeErrorDialogHelperTests
{
    [Fact]
    public void BuildErrorMessage_IncludesSummaryTypeAndNextStep_WithoutRawExceptionMessage()
    {
        var result = SafeErrorDialogHelper.BuildErrorMessage(
            "Could not play the narration preview",
            new InvalidOperationException("Secret notification text"),
            "Try a different voice and try again");

        Assert.Contains("Could not play the narration preview.", result);
        Assert.Contains("Details: InvalidOperationException.", result);
        Assert.Contains("Try a different voice and try again.", result);
        Assert.DoesNotContain("Secret notification text", result);
    }

    [Fact]
    public void BuildErrorMessage_UsesBaseExceptionType_AndOmitsWrappedMessages()
    {
        var wrapped = new Exception("wrapper with sensitive text", new IOException(@"C:\Users\demo\secret.png"));

        var result = SafeErrorDialogHelper.BuildErrorMessage("Could not copy the selected background image", wrapped);

        Assert.Contains("Details: IOException.", result);
        Assert.DoesNotContain("wrapper with sensitive text", result);
        Assert.DoesNotContain(@"C:\Users\demo\secret.png", result);
    }

    [Fact]
    public void BuildErrorMessage_HandlesMissingExceptionAndNormalizesPunctuation()
    {
        var result = SafeErrorDialogHelper.BuildErrorMessage("Notifications Pro hit an unexpected error", null, "Restart the app");

        Assert.Equal(
            "Notifications Pro hit an unexpected error.\n\nRestart the app.",
            result);
    }
}
