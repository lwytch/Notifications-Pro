using System.Windows;
using NotificationsPro.ViewModels;

namespace NotificationsPro.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
