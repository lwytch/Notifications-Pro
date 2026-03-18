using System.IO;
using NotificationsPro.Models;
using NotificationsPro.Services;

namespace NotificationsPro.Tests;

public class IconServiceTests
{
    private static readonly byte[] OnePixelPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+X2ioAAAAASUVORK5CYII=");

    [Fact]
    public void ResolveIcon_LoadsCustomIconWithinManagedIconsRoot()
    {
        var testDir = Path.Combine(IconService.GetCustomIconsDir(), "__tests", Guid.NewGuid().ToString("N"));
        var filePath = Path.Combine(testDir, "icon.png");
        Directory.CreateDirectory(testDir);
        File.WriteAllBytes(filePath, OnePixelPng);

        try
        {
            var settings = new AppSettings
            {
                ShowNotificationIcons = true,
                DefaultIconPreset = filePath
            };

            var service = new IconService();
            var icon = StaThreadTestHelper.Run(() => service.ResolveIcon("Codex", settings));

            Assert.NotNull(icon);
        }
        finally
        {
            TryDeleteDirectory(testDir);
        }
    }

    [Fact]
    public void ResolveIcon_RejectsCustomIconOutsideManagedIconsRootEvenWhenPathSharesPrefix()
    {
        var managedRoot = IconService.GetCustomIconsDir().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var testDir = Path.Combine(managedRoot + "-escape", Guid.NewGuid().ToString("N"));
        var filePath = Path.Combine(testDir, "icon.png");
        Directory.CreateDirectory(testDir);
        File.WriteAllBytes(filePath, OnePixelPng);

        try
        {
            var settings = new AppSettings
            {
                ShowNotificationIcons = true,
                DefaultIconPreset = filePath
            };

            var service = new IconService();
            var icon = StaThreadTestHelper.Run(() => service.ResolveIcon("Codex", settings));

            Assert.Null(icon);
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
