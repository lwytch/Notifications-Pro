using System.Windows;
using NotificationsPro.Helpers;

namespace NotificationsPro.Tests;

public class SettingsWindowPlacementHelperTests
{
    [Fact]
    public void CreatePopupBounds_CentersWithinWorkArea_WhenNoStoredPosition()
    {
        var workArea = new Rect(0, 0, 1920, 1040);

        var bounds = SettingsWindowPlacementHelper.CreatePopupBounds(780, 560, workArea);

        Assert.Equal(570, bounds.Left);
        Assert.Equal(234, bounds.Top);
        Assert.Equal(780, bounds.Width);
        Assert.Equal(572, bounds.Height);
    }

    [Fact]
    public void CreatePopupBounds_UsesStoredPosition_OnSecondaryDisplay()
    {
        var workArea = new Rect(1920, 0, 1920, 1040);

        var bounds = SettingsWindowPlacementHelper.CreatePopupBounds(780, 560, workArea, 2250, 120);

        Assert.Equal(2250, bounds.Left);
        Assert.Equal(120, bounds.Top);
        Assert.Equal(780, bounds.Width);
        Assert.Equal(572, bounds.Height);
    }

    [Fact]
    public void ClampBoundsToWorkArea_RepositionsOffscreenWindowBackIntoVisibleArea()
    {
        var workArea = new Rect(1920, 0, 1920, 1040);

        var bounds = SettingsWindowPlacementHelper.ClampBoundsToWorkArea(
            new Rect(3500, 900, 780, 740),
            workArea);

        Assert.Equal(3060, bounds.Left);
        Assert.Equal(300, bounds.Top);
        Assert.Equal(780, bounds.Width);
        Assert.Equal(740, bounds.Height);
    }
}
