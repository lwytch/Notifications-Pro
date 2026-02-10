using System.Globalization;
using NotificationsPro.Converters;

namespace NotificationsPro.Tests;

public class NotificationOneLineContentConverterTests
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    [Fact]
    public void Convert_DefaultMode_TruncatesBodyPreview()
    {
        var converter = new NotificationOneLineContentConverter();
        var body = new string('a', 90);

        var result = converter.Convert(
            new object[] { true, "Title", true, body },
            typeof(string),
            string.Empty,
            Culture);

        Assert.Equal($"Title - {new string('a', 80)}...", result);
    }

    [Fact]
    public void Convert_FullMode_DoesNotTruncateBody()
    {
        var converter = new NotificationOneLineContentConverter();
        var body = new string('b', 90);

        var result = converter.Convert(
            new object[] { true, "Title", true, body },
            typeof(string),
            "full",
            Culture);

        Assert.Equal($"Title - {body}", result);
    }

    [Fact]
    public void Convert_SegmentTitle_AppendsSeparatorWhenBodyExists()
    {
        var converter = new NotificationOneLineContentConverter();

        var result = converter.Convert(
            new object[] { true, "Mention", true, "Body" },
            typeof(string),
            "segment:title",
            Culture);

        Assert.Equal("Mention - ", result);
    }

    [Fact]
    public void Convert_SegmentApp_InExtendedMode_AppendsSeparatorWhenTailExists()
    {
        var converter = new NotificationOneLineContentConverter();

        var result = converter.Convert(
            new object[] { true, "Slack", true, "Mention", true, "Body" },
            typeof(string),
            "segment:app;full",
            Culture);

        Assert.Equal("Slack - ", result);
    }

    [Fact]
    public void Convert_SegmentBody_RespectsCompactDefault()
    {
        var converter = new NotificationOneLineContentConverter();
        var body = new string('c', 90);

        var result = converter.Convert(
            new object[] { true, "Mention", true, body },
            typeof(string),
            "segment:body",
            Culture);

        Assert.Equal($"{new string('c', 80)}...", result);
    }
}
