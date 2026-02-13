using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System.Windows.Input;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Input;
using TrackMmr.Desktop.ViewModels;
using TrackMmr.Desktop.Views;

namespace TrackMmr.Desktop;

public partial class App : Application
{
    public ICommand ToggleWindowCommand { get; }
    public ICommand ExitAppCommand { get; }

    public App()
    {
        ToggleWindowCommand = new RelayCommand(ToggleWindow);
        ExitAppCommand = new RelayCommand(ExitApp);
        DataContext = this;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ToggleWindow()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            if (desktop.MainWindow.IsVisible)
            {
                desktop.MainWindow.Hide();
            }
            else
            {
                desktop.MainWindow.Show();
                desktop.MainWindow.Activate();
            }
        }
    }

    private void ExitApp()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow is MainWindow mainWin)
            {
                mainWin.IsExiting = true;
            }
            desktop.Shutdown();
        }
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
