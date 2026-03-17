using System.Collections;
using System.Windows.Controls;
using NotificationsPro.Helpers;

namespace NotificationsPro.Tests;

public class SettingsTabNavigationHelperTests
{
    [Fact]
    public void FindTabIndexByHeader_MatchesNestedHeaderText_IgnoringCase()
    {
        StaThreadTestHelper.Run(() =>
        {
            IList tabItems = new List<object>
            {
                new TabItem { Header = "Appearance" },
                new TabItem
                {
                    Header = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                        {
                            new Border { Width = 8, Height = 8 },
                            new TextBlock { Text = "Filtering" }
                        }
                    }
                },
                new TabItem { Header = new TextBlock { Text = "Profiles" } }
            };

            var index = SettingsTabNavigationHelper.FindTabIndexByHeader(tabItems, "filtering");

            Assert.Equal(1, index);
        });
    }

    [Fact]
    public void FindTabIndexByHeader_ReturnsMinusOne_WhenHeaderDoesNotExist()
    {
        StaThreadTestHelper.Run(() =>
        {
            IList tabItems = new List<object>
            {
                new TabItem { Header = "Appearance" },
                new TabItem { Header = "Behavior" }
            };

            var index = SettingsTabNavigationHelper.FindTabIndexByHeader(tabItems, "System");

            Assert.Equal(-1, index);
        });
    }
}
