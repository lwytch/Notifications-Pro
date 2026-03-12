using NotificationsPro.Services;
using NotificationsPro.ViewModels;

namespace NotificationsPro.Tests;

public class OverlayViewModelTests : IDisposable
{
    private readonly string _tempDir;

    public OverlayViewModelTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "NotificationsProOverlayVm_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public void OverlayScrollbarSettings_RaiseDependentProperties()
    {
        var settingsManager = new SettingsManager(_tempDir);
        settingsManager.Load();
        var queueManager = new QueueManager(settingsManager);
        var viewModel = new OverlayViewModel(queueManager, settingsManager);

        try
        {
            var changedProperties = new List<string>();
            viewModel.PropertyChanged += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.PropertyName))
                    changedProperties.Add(args.PropertyName!);
            };

            viewModel.OverlayScrollbarVisible = true;
            viewModel.OverlayScrollbarContentGap = 14;

            Assert.Equal(System.Windows.Controls.ScrollBarVisibility.Hidden, viewModel.ScrollbarVisibility);
            Assert.Contains(nameof(OverlayViewModel.OverlayScrollbarVisible), changedProperties);
            Assert.Contains(nameof(OverlayViewModel.ScrollbarVisibility), changedProperties);
            Assert.Contains(nameof(OverlayViewModel.OverlayScrollbarContentGap), changedProperties);
            Assert.Contains(nameof(OverlayViewModel.OverlayContentMargin), changedProperties);
        }
        finally
        {
            viewModel.Cleanup();
        }
    }

    [Fact]
    public void OverlayScrollbar_HidesWhenQueueIsEmpty_AndAddsConfiguredGapWhenVisible()
    {
        var settingsManager = new SettingsManager(_tempDir);
        settingsManager.Load();
        settingsManager.Settings.OverlayScrollbarVisible = true;
        settingsManager.Settings.OverlayScrollbarContentGap = 12;

        var queueManager = new QueueManager(settingsManager);
        var viewModel = new OverlayViewModel(queueManager, settingsManager);

        try
        {
            Assert.Equal(System.Windows.Controls.ScrollBarVisibility.Hidden, viewModel.ScrollbarVisibility);
            Assert.Equal(0, viewModel.OverlayContentMargin.Right);

            queueManager.AddNotification("X", "Title", "Body");

            Assert.Equal(System.Windows.Controls.ScrollBarVisibility.Visible, viewModel.ScrollbarVisibility);
            Assert.Equal(12, viewModel.OverlayContentMargin.Right);

            queueManager.ClearAll();

            Assert.Equal(System.Windows.Controls.ScrollBarVisibility.Hidden, viewModel.ScrollbarVisibility);
            Assert.Equal(0, viewModel.OverlayContentMargin.Right);
        }
        finally
        {
            viewModel.Cleanup();
        }
    }
}
