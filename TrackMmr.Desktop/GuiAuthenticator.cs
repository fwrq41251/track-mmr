using System.Threading.Tasks;
using Avalonia.Threading;
using SteamKit2.Authentication;
using TrackMmr.Desktop.Views;

namespace TrackMmr.Desktop;

public class GuiAuthenticator : IAuthenticator
{
    private readonly Avalonia.Controls.Window _owner;

    public GuiAuthenticator(Avalonia.Controls.Window owner)
    {
        _owner = owner;
    }

    public async Task<string> GetDeviceCodeAsync(bool previousCodeWasIncorrect)
    {
        return await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var dialog = new SteamGuardDialog();
            var msg = previousCodeWasIncorrect
                ? "Previous code was incorrect. Enter your Steam Guard code:"
                : "Enter your Steam Guard code from your authenticator app:";
            dialog.SetMessage(msg);
            await dialog.ShowDialog(_owner);
            return dialog.Code;
        });
    }

    public async Task<string> GetEmailCodeAsync(string email, bool previousCodeWasIncorrect)
    {
        return await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var dialog = new SteamGuardDialog();
            var msg = previousCodeWasIncorrect
                ? $"Previous code was incorrect. Enter the code sent to {email}:"
                : $"Enter the Steam Guard code sent to {email}:";
            dialog.SetMessage(msg);
            await dialog.ShowDialog(_owner);
            return dialog.Code;
        });
    }

    public Task<bool> AcceptDeviceConfirmationAsync()
    {
        return Task.FromResult(true);
    }
}
