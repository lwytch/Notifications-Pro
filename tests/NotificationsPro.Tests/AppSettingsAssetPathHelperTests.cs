using System.IO;
using NotificationsPro.Helpers;
using NotificationsPro.Models;

namespace NotificationsPro.Tests;

public class AppSettingsAssetPathHelperTests
{
    [Fact]
    public void NormalizeForRuntime_ResolvesManagedAssetsAndDropsUnsafeEntries()
    {
        var soundsRoot = ManagedAssetPathHelper.GetRoot(ManagedAssetPathHelper.SoundsFolderName);
        var iconsRoot = ManagedAssetPathHelper.GetRoot(ManagedAssetPathHelper.IconsFolderName);
        var backgroundsRoot = ManagedAssetPathHelper.GetRoot(ManagedAssetPathHelper.BackgroundsFolderName);

        var settings = new AppSettings
        {
            DefaultSound = @"\\server\share\bad.wav",
            DefaultIconPreset = "icons/default.png",
            CardBackgroundImagePath = "backgrounds/card.png",
            FullscreenOverlayImagePath = "backgrounds/fullscreen.png"
        };

        settings.PerAppSounds["Teams"] = "sounds/teams.wav";
        settings.PerAppSounds["Bad"] = @"C:\Temp\outside.wav";
        settings.PerAppSounds["Traversal"] = @"sounds\..\..\escape.wav";

        settings.PerAppIcons["Slack"] = "Bell";
        settings.PerAppIcons["Teams"] = "icons/teams.png";
        settings.PerAppIcons["Bad"] = @"C:\Temp\icon.png";
        settings.PerAppIcons["None"] = "None";

        settings.PerAppBackgroundImages["Codex"] = "backgrounds/codex.png";
        settings.PerAppBackgroundImages["Bad"] = @"\\server\share\bg.png";

        AppSettingsAssetPathHelper.NormalizeForRuntime(settings);

        Assert.Equal("None", settings.DefaultSound);
        Assert.Equal(Path.Combine(soundsRoot, "teams.wav"), settings.PerAppSounds["Teams"]);
        Assert.DoesNotContain("Bad", settings.PerAppSounds.Keys);
        Assert.DoesNotContain("Traversal", settings.PerAppSounds.Keys);

        Assert.Equal(Path.Combine(iconsRoot, "default.png"), settings.DefaultIconPreset);
        Assert.Equal("Bell", settings.PerAppIcons["Slack"]);
        Assert.Equal(Path.Combine(iconsRoot, "teams.png"), settings.PerAppIcons["Teams"]);
        Assert.DoesNotContain("Bad", settings.PerAppIcons.Keys);
        Assert.DoesNotContain("None", settings.PerAppIcons.Keys);

        Assert.Equal(Path.Combine(backgroundsRoot, "card.png"), settings.CardBackgroundImagePath);
        Assert.Equal(Path.Combine(backgroundsRoot, "fullscreen.png"), settings.FullscreenOverlayImagePath);
        Assert.Equal(Path.Combine(backgroundsRoot, "codex.png"), settings.PerAppBackgroundImages["Codex"]);
        Assert.DoesNotContain("Bad", settings.PerAppBackgroundImages.Keys);
    }

    [Fact]
    public void CreatePortableSnapshot_ConvertsManagedAssetsWithoutMutatingSourceSettings()
    {
        var soundsRoot = ManagedAssetPathHelper.GetRoot(ManagedAssetPathHelper.SoundsFolderName);
        var iconsRoot = ManagedAssetPathHelper.GetRoot(ManagedAssetPathHelper.IconsFolderName);
        var backgroundsRoot = ManagedAssetPathHelper.GetRoot(ManagedAssetPathHelper.BackgroundsFolderName);

        var source = new AppSettings
        {
            DefaultSound = Path.Combine(soundsRoot, "default.wav"),
            DefaultIconPreset = Path.Combine(iconsRoot, "default.png"),
            CardBackgroundImagePath = Path.Combine(backgroundsRoot, "card.png"),
            FullscreenOverlayImagePath = Path.Combine(backgroundsRoot, "fullscreen.png")
        };

        source.PerAppSounds["Teams"] = Path.Combine(soundsRoot, "teams.wav");
        source.PerAppIcons["Slack"] = "Bell";
        source.PerAppIcons["Teams"] = Path.Combine(iconsRoot, "teams.png");
        source.PerAppBackgroundImages["Codex"] = Path.Combine(backgroundsRoot, "codex.png");

        var snapshot = AppSettingsAssetPathHelper.CreatePortableSnapshot(source);

        Assert.Equal(Path.Combine(soundsRoot, "default.wav"), source.DefaultSound);
        Assert.Equal(Path.Combine(iconsRoot, "default.png"), source.DefaultIconPreset);
        Assert.Equal(Path.Combine(backgroundsRoot, "card.png"), source.CardBackgroundImagePath);
        Assert.Equal(Path.Combine(backgroundsRoot, "fullscreen.png"), source.FullscreenOverlayImagePath);

        Assert.Equal("sounds/default.wav", snapshot.DefaultSound);
        Assert.Equal("icons/default.png", snapshot.DefaultIconPreset);
        Assert.Equal("backgrounds/card.png", snapshot.CardBackgroundImagePath);
        Assert.Equal("backgrounds/fullscreen.png", snapshot.FullscreenOverlayImagePath);
        Assert.Equal("sounds/teams.wav", snapshot.PerAppSounds["Teams"]);
        Assert.Equal("Bell", snapshot.PerAppIcons["Slack"]);
        Assert.Equal("icons/teams.png", snapshot.PerAppIcons["Teams"]);
        Assert.Equal("backgrounds/codex.png", snapshot.PerAppBackgroundImages["Codex"]);
    }
}
