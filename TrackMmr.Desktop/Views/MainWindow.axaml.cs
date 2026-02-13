using Avalonia.Controls;
using TrackMmr.Desktop.ViewModels;

namespace TrackMmr.Desktop.Views;

public partial class MainWindow : Window
{
    public bool IsExiting { get; set; }

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
                vm.SetOwner(this);
        };
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!IsExiting)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnClosing(e);
    }
}
