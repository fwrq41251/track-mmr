using Avalonia.Controls;
using TrackMmr.Desktop.ViewModels;

namespace TrackMmr.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
                vm.SetOwner(this);
        };
    }
}
