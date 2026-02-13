using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TrackMmr.Desktop.Views;

public partial class SteamGuardDialog : Window
{
    public string Code { get; private set; } = "";

    public SteamGuardDialog()
    {
        InitializeComponent();
        SubmitButton.Click += OnSubmit;
        CodeInput.KeyDown += (s, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
                OnSubmit(s, e);
        };
    }

    public void SetMessage(string message)
    {
        MessageText.Text = message;
    }

    private void OnSubmit(object? sender, RoutedEventArgs e)
    {
        Code = CodeInput.Text?.Trim() ?? "";
        Close();
    }
}
