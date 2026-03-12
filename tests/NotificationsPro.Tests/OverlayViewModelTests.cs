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
    public void OverlayScrollbarVisible_RaisesScrollbarVisibilityChanged()
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

            Assert.Equal(System.Windows.Controls.ScrollBarVisibility.Hidden, viewModel.ScrollbarVisibility);

            viewModel.OverlayScrollbarVisible = true;

            Assert.Equal(System.Windows.Controls.ScrollBarVisibility.Visible, viewModel.ScrollbarVisibility);
            Assert.Contains(nameof(OverlayViewModel.OverlayScrollbarVisible), changedProperties);
            Assert.Contains(nameof(OverlayViewModel.ScrollbarVisibility), changedProperties);
        }
        finally
        {
            viewModel.Cleanup();
        }
    }
}
