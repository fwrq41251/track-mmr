using SteamKit2;
using SteamKit2.Authentication;
using SteamKit2.GC;
using SteamKit2.GC.Dota.Internal;
using SteamKit2.Internal;

namespace TrackMmr;

public class SteamService : IDisposable
{
    private readonly SteamClient _steamClient;
    private readonly CallbackManager _manager;
    private readonly SteamUser _steamUser;
    private readonly SteamGameCoordinator _gameCoordinator;

    private readonly AppConfig _config;
    private readonly IAuthenticator? _authenticator;
    private readonly TaskCompletionSource<bool> _loggedOnTcs = new();
    private readonly TaskCompletionSource<bool> _gcReadyTcs = new();
    private readonly TaskCompletionSource<CMsgDOTAGetPlayerMatchHistoryResponse> _matchHistoryTcs = new();

    private bool _isRunning;
    private bool _disposed;

    public SteamService(AppConfig config, IAuthenticator? authenticator = null)
    {
        _config = config;
        _authenticator = authenticator;
        _steamClient = new SteamClient();
        _manager = new CallbackManager(_steamClient);
        _steamUser = _steamClient.GetHandler<SteamUser>()!;
        _gameCoordinator = _steamClient.GetHandler<SteamGameCoordinator>()!;

        _manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
        _manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
        _manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
        _manager.Subscribe<SteamGameCoordinator.MessageCallback>(OnGCMessage);
    }

    public async Task<List<MmrRecord>> FetchMmrAsync()
    {
        _isRunning = true;

        _steamClient.Connect();

        var callbackTask = Task.Run(() =>
        {
            while (_isRunning)
            {
                _manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
        });

        await _loggedOnTcs.Task;

        await StartDota2();
        await _gcReadyTcs.Task;

        var accountId = _steamClient.SteamID!.AccountID;
        RequestMatchHistory(accountId);

        var response = await _matchHistoryTcs.Task;

        _isRunning = false;
        _steamClient.Disconnect();
        await callbackTask;

        var records = new List<MmrRecord>();
        foreach (var match in response.matches)
        {
            if (match.previous_rank == 0 && match.rank_change == 0)
                continue;

            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var matchTime = epoch.AddSeconds(match.start_time).ToLocalTime();
            var mmr = (int)match.previous_rank + match.rank_change;

            records.Add(new MmrRecord(
                matchTime,
                match.match_id,
                mmr,
                match.rank_change,
                match.hero_id,
                match.winner
            ));
        }

        return records;
    }

    private void OnConnected(SteamClient.ConnectedCallback callback)
    {
        Console.WriteLine("Connected to Steam. Authenticating...");
        _ = AuthenticateAsync();
    }

    private async Task AuthenticateAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(_config.RefreshToken))
            {
                Console.WriteLine("Using saved refresh token...");
                _steamUser.LogOn(new SteamUser.LogOnDetails
                {
                    Username = _config.Username,
                    AccessToken = _config.RefreshToken
                });
                return;
            }

            await PerformCredentialAuthAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Authentication error: {ex.Message}");
            _loggedOnTcs.TrySetException(ex);
        }
    }

    private async Task PerformCredentialAuthAsync()
    {
        if (string.IsNullOrEmpty(_config.Username) || string.IsNullOrEmpty(_config.Password))
        {
            throw new InvalidOperationException("Username and password are required for first-time authentication.");
        }

        Console.WriteLine("Authenticating with credentials...");

        var authDetails = new AuthSessionDetails
        {
            Username = _config.Username,
            Password = _config.Password,
            DeviceFriendlyName = "TrackMmr",
            PlatformType = EAuthTokenPlatformType.k_EAuthTokenPlatformType_SteamClient,
            ClientOSType = EOSType.MacOSUnknown,
            Authenticator = _authenticator ?? new UserConsoleAuthenticator()
        };

        var authSession = await _steamClient.Authentication.BeginAuthSessionViaCredentialsAsync(authDetails);
        var pollResult = await authSession.PollingWaitForResultAsync();

        Console.WriteLine($"Authenticated as {pollResult.AccountName}.");

        _config.RefreshToken = pollResult.RefreshToken;
        _config.Password = "";
        _config.Save();

        _steamUser.LogOn(new SteamUser.LogOnDetails
        {
            Username = pollResult.AccountName,
            AccessToken = pollResult.RefreshToken
        });
    }

    private void OnLoggedOn(SteamUser.LoggedOnCallback callback)
    {
        if (callback.Result == EResult.OK)
        {
            Console.WriteLine("Successfully logged on to Steam.");
            _loggedOnTcs.TrySetResult(true);
            return;
        }

        Console.WriteLine($"Logon failed: {callback.Result}");

        if (!string.IsNullOrEmpty(_config.RefreshToken))
        {
            Console.WriteLine("Refresh token expired. Please re-run with: dotnet run -- login");
            _config.RefreshToken = "";
            _config.Save();
        }

        _loggedOnTcs.TrySetException(new Exception($"Failed to log on: {callback.Result}"));
    }

    private void OnDisconnected(SteamClient.DisconnectedCallback callback)
    {
        if (_isRunning && !_loggedOnTcs.Task.IsCompleted)
        {
            Console.WriteLine("Disconnected from Steam. Reconnecting...");
            Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(_ => _steamClient.Connect());
        }
    }

    private async Task StartDota2()
    {
        Console.WriteLine("Launching Dota 2...");

        var playGame = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayed);
        playGame.Body.games_played.Add(new CMsgClientGamesPlayed.GamePlayed { game_id = 570 });
        _steamClient.Send(playGame);

        await Task.Delay(TimeSpan.FromSeconds(5));

        Console.WriteLine("Sending hello to Dota 2 GC...");
        var clientHello = new ClientGCMsgProtobuf<SteamKit2.GC.Dota.Internal.CMsgClientHello>((uint)EGCBaseClientMsg.k_EMsgGCClientHello);
        clientHello.Body.engine = ESourceEngine.k_ESE_Source2;
        _gameCoordinator.Send(clientHello, 570);
    }

    private void RequestMatchHistory(uint accountId)
    {
        Console.WriteLine("Requesting match history...");
        var request = new ClientGCMsgProtobuf<CMsgDOTAGetPlayerMatchHistory>((uint)EDOTAGCMsg.k_EMsgDOTAGetPlayerMatchHistory);
        request.Body.account_id = accountId;
        request.Body.matches_requested = 20;
        _gameCoordinator.Send(request, 570);
    }

    private void OnGCMessage(SteamGameCoordinator.MessageCallback callback)
    {
        if (callback.EMsg == (uint)EGCBaseClientMsg.k_EMsgGCClientWelcome)
        {
            Console.WriteLine("Connected to Dota 2 GC.");
            _gcReadyTcs.TrySetResult(true);
        }
        else if (callback.EMsg == (uint)EDOTAGCMsg.k_EMsgDOTAGetPlayerMatchHistoryResponse)
        {
            var msg = new ClientGCMsgProtobuf<CMsgDOTAGetPlayerMatchHistoryResponse>(callback.Message);
            Console.WriteLine("Received match history.");
            _matchHistoryTcs.TrySetResult(msg.Body);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _isRunning = false;
        _steamClient.Disconnect();
    }
}
