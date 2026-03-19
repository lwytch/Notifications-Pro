using System.IO;
using NotificationsPro.Services;

namespace NotificationsPro.Tests;

public class BackgroundImageServiceTests
{
    private static readonly byte[] OnePixelPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+X2ioAAAAASUVORK5CYII=");

    [Fact]
    public void ResolveBackgroundImage_LoadsManagedBackgroundImage()
    {
        var testDir = Path.Combine(BackgroundImageService.GetCustomBackgroundsDir(), "__tests", Guid.NewGuid().ToString("N"));
        var filePath = Path.Combine(testDir, "card.png");
        Directory.CreateDirectory(testDir);
        File.WriteAllBytes(filePath, OnePixelPng);

        try
        {
            var service = new BackgroundImageService();
            var image = StaThreadTestHelper.Run(() => service.ResolveBackgroundImage(filePath, 0, 1.0, 1.0, 1.0, false));

            Assert.NotNull(image);
        }
        finally
        {
            TryDeleteDirectory(testDir);
        }
    }

    [Fact]
    public void ResolveBackgroundImage_RejectsSiblingPrefixPathOutsideManagedBackgroundsRoot()
    {
        var managedRoot = BackgroundImageService.GetCustomBackgroundsDir().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var testDir = Path.Combine(managedRoot + "-escape", Guid.NewGuid().ToString("N"));
        var filePath = Path.Combine(testDir, "card.png");
        Directory.CreateDirectory(testDir);
        File.WriteAllBytes(filePath, OnePixelPng);

        try
        {
            var service = new BackgroundImageService();
            var image = StaThreadTestHelper.Run(() => service.ResolveBackgroundImage(filePath, 0, 1.0, 1.0, 1.0, false));

            Assert.Null(image);
            Assert.Equal(0, service.CachedImageCount);
        }
        finally
        {
            TryDeleteDirectory(testDir);
        }
    }

    [Fact]
    public void ResolveBackgroundImage_KeepsCacheBounded()
    {
        var testDir = Path.Combine(BackgroundImageService.GetCustomBackgroundsDir(), "__tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);

        try
        {
            var service = new BackgroundImageService();
            for (var index = 0; index < BackgroundImageService.MaxCachedImages + 6; index++)
            {
                var filePath = Path.Combine(testDir, $"card-{index}.png");
                File.WriteAllBytes(filePath, OnePixelPng);

                var image = StaThreadTestHelper.Run(() => service.ResolveBackgroundImage(filePath, index, 1.0, 1.0, 1.0, false));
                Assert.NotNull(image);
            }

            Assert.InRange(service.CachedImageCount, 1, BackgroundImageService.MaxCachedImages);
        }
        finally
        {
            TryDeleteDirectory(testDir);
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
        }
    }
}
