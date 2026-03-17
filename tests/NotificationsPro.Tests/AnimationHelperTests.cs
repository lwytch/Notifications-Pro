using System.Windows;
using System.Windows.Media;
using NotificationsPro.Helpers;

namespace NotificationsPro.Tests;

public class AnimationHelperTests
{
    [Theory]
    [InlineData(null, NotificationAnimationStyleHelper.SlideFade)]
    [InlineData("", NotificationAnimationStyleHelper.SlideFade)]
    [InlineData("Slide", NotificationAnimationStyleHelper.Slide)]
    [InlineData("slide + fade", NotificationAnimationStyleHelper.SlideFade)]
    [InlineData("FADE", NotificationAnimationStyleHelper.Fade)]
    [InlineData("Drift + Fade", NotificationAnimationStyleHelper.DriftFade)]
    [InlineData("Zoom + Fade", NotificationAnimationStyleHelper.ZoomFade)]
    [InlineData("Pop", NotificationAnimationStyleHelper.Pop)]
    [InlineData("unexpected", NotificationAnimationStyleHelper.SlideFade)]
    public void NotificationAnimationStyleHelper_Normalize_ReturnsExpectedValue(string? value, string expected)
    {
        Assert.Equal(expected, NotificationAnimationStyleHelper.Normalize(value));
    }

    [Theory]
    [InlineData(NotificationAnimationStyleHelper.Slide, true)]
    [InlineData(NotificationAnimationStyleHelper.SlideFade, true)]
    [InlineData(NotificationAnimationStyleHelper.DriftFade, true)]
    [InlineData(NotificationAnimationStyleHelper.Fade, false)]
    [InlineData(NotificationAnimationStyleHelper.ZoomFade, false)]
    [InlineData(NotificationAnimationStyleHelper.Pop, false)]
    [InlineData("unexpected", true)]
    public void NotificationAnimationStyleHelper_UsesDirection_OnlyForDirectionalModes(string value, bool expected)
    {
        Assert.Equal(expected, NotificationAnimationStyleHelper.UsesDirection(value));
    }

    [Fact]
    public void NotificationAnimationStyleHelper_LegacyMappingAndHighlightDelay_AreStable()
    {
        Assert.Equal(NotificationAnimationStyleHelper.Fade, NotificationAnimationStyleHelper.FromLegacyFadeOnly(true));
        Assert.Equal(NotificationAnimationStyleHelper.SlideFade, NotificationAnimationStyleHelper.FromLegacyFadeOnly(false));
        Assert.False(NotificationAnimationStyleHelper.ShouldDelayHighlight(NotificationAnimationStyleHelper.Fade));
        Assert.True(NotificationAnimationStyleHelper.ShouldDelayHighlight(NotificationAnimationStyleHelper.Pop));
        Assert.True(NotificationAnimationStyleHelper.IsLegacyFadeOnly(NotificationAnimationStyleHelper.Fade));
        Assert.False(NotificationAnimationStyleHelper.IsLegacyFadeOnly(NotificationAnimationStyleHelper.Slide));
    }

    [Theory]
    [InlineData(null, AnimationEasingHelper.EaseOut)]
    [InlineData("", AnimationEasingHelper.EaseOut)]
    [InlineData("bounce", AnimationEasingHelper.Bounce)]
    [InlineData("ELASTIC", AnimationEasingHelper.Elastic)]
    [InlineData("Linear", AnimationEasingHelper.Linear)]
    [InlineData("unexpected", AnimationEasingHelper.EaseOut)]
    public void AnimationEasingHelper_Normalize_ReturnsExpectedValue(string? value, string expected)
    {
        Assert.Equal(expected, AnimationEasingHelper.Normalize(value));
    }

    [Theory]
    [InlineData(null, CardBackgroundImageFitModeHelper.FillCard, Stretch.UniformToFill)]
    [InlineData("Fit Inside Card", CardBackgroundImageFitModeHelper.FitInsideCard, Stretch.Uniform)]
    [InlineData("Original Size", CardBackgroundImageFitModeHelper.OriginalSize, Stretch.None)]
    [InlineData("unexpected", CardBackgroundImageFitModeHelper.FillCard, Stretch.UniformToFill)]
    public void CardBackgroundImageFitModeHelper_NormalizeAndMapToStretch(
        string? value,
        string expectedMode,
        Stretch expectedStretch)
    {
        Assert.Equal(expectedMode, CardBackgroundImageFitModeHelper.Normalize(value));
        Assert.Equal(expectedStretch, CardBackgroundImageFitModeHelper.ToStretch(value));
    }

    [Theory]
    [InlineData(null, ImageVerticalFocusHelper.Center, VerticalAlignment.Center)]
    [InlineData("Top", ImageVerticalFocusHelper.Top, VerticalAlignment.Top)]
    [InlineData("Bottom", ImageVerticalFocusHelper.Bottom, VerticalAlignment.Bottom)]
    [InlineData("unexpected", ImageVerticalFocusHelper.Center, VerticalAlignment.Center)]
    public void ImageVerticalFocusHelper_NormalizeAndMapAlignment(
        string? value,
        string expectedMode,
        VerticalAlignment expectedAlignment)
    {
        Assert.Equal(expectedMode, ImageVerticalFocusHelper.Normalize(value));
        Assert.Equal(expectedAlignment, ImageVerticalFocusHelper.ToVerticalAlignment(value));
    }

    [Theory]
    [InlineData(null, NotificationCaptureModeHelper.ModeAuto)]
    [InlineData("Prefer WinRT", NotificationCaptureModeHelper.ModeWinRt)]
    [InlineData("Force Accessibility", NotificationCaptureModeHelper.ModeAccessibility)]
    [InlineData("unexpected", NotificationCaptureModeHelper.ModeAuto)]
    public void NotificationCaptureModeHelper_NormalizeMode_ReturnsExpectedValue(string? value, string expected)
    {
        Assert.Equal(expected, NotificationCaptureModeHelper.NormalizeMode(value));
    }
}
