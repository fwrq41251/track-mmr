using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace TrackMmr.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly MmrDatabase _db;
    private readonly Avalonia.Threading.DispatcherTimer _autoRefreshTimer;
    private Avalonia.Controls.Window? _owner;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _currentMmr = "-";

    [ObservableProperty]
    private string _mmrDelta = "";

    [ObservableProperty]
    private string _rankTitle = "UNKNOWN";

    [ObservableProperty]
    private int _sessionWins;

    [ObservableProperty]
    private int _sessionLosses;

    [ObservableProperty]
    private string _winRate = "0%";

    [ObservableProperty]
    private ISeries[] _mmrSeries = [];

    [ObservableProperty]
    private Axis[] _xAxes = [];

    [ObservableProperty]
    private Axis[] _yAxes = [];

    public ObservableCollection<MatchRowViewModel> Matches { get; } = new();

    [ObservableProperty]
    private string _username = "";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private bool _needsLogin;

    public MainWindowViewModel()
    {
        var dbPath = System.IO.Path.Combine(AppContext.BaseDirectory, "mmr.db");
        _db = new MmrDatabase(dbPath);
        _db.EnsureCreated();

        _autoRefreshTimer = new Avalonia.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(5)
        };
        _autoRefreshTimer.Tick += async (_, _) =>
        {
            if (!IsBusy && !NeedsLogin)
                await FetchMmrAsync();
        };
        _autoRefreshTimer.Start();

        var configPath = AppConfig.GetConfigPath();
        if (System.IO.File.Exists(configPath))
        {
            var config = AppConfig.Load();
            if (!string.IsNullOrEmpty(config.RefreshToken) || !string.IsNullOrEmpty(config.Username))
            {
                NeedsLogin = false;
                LoadHistory();
                return;
            }
        }

        NeedsLogin = true;
    }

    public void SetOwner(Avalonia.Controls.Window owner)
    {
        _owner = owner;
    }

    partial void OnIsBusyChanged(bool value)
    {
        FetchMmrCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanFetch))]
    private async Task FetchMmrAsync()
    {
        IsBusy = true;
        StatusText = "Connecting to Steam...";
        try
        {
            var config = AppConfig.Load();
            var authenticator = _owner != null ? new GuiAuthenticator(_owner) : null;
            using var steam = new SteamService(config, authenticator);

            StatusText = "Fetching match history...";
            var records = await steam.FetchMmrAsync();
            var inserted = _db.SaveRecords(records);

            StatusText = $"Fetched {records.Count} matches, {inserted} new. Auto-refresh in 5 min.";
            LoadHistory();
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
            if (ex.Message.Contains("log on", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("token", StringComparison.OrdinalIgnoreCase))
            {
                NeedsLogin = true;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanFetch() => !IsBusy;

    [RelayCommand]
    private async Task LoginAndFetchAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            StatusText = "Please enter username and password.";
            return;
        }

        var config = new AppConfig { Username = Username, Password = Password };
        NeedsLogin = false;
        IsBusy = true;
        StatusText = "Authenticating...";

        try
        {
            var authenticator = _owner != null ? new GuiAuthenticator(_owner) : null;
            using var steam = new SteamService(config, authenticator);
            var records = await steam.FetchMmrAsync();
            var inserted = _db.SaveRecords(records);
            StatusText = $"Fetched {records.Count} matches, {inserted} new. Auto-refresh in 5 min.";
            Password = "";
            LoadHistory();
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
            NeedsLogin = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void LoadHistory()
    {
        var records = _db.GetHistory();

        Matches.Clear();
        foreach (var r in records)
        {
            Matches.Add(new MatchRowViewModel(r));
        }

        if (records.Count > 0)
        {
            var latest = records[0];
            CurrentMmr = latest.Mmr.ToString();
            
            // Calculate Delta from previous match
            if (records.Count > 1)
            {
                var delta = latest.MmrChange;
                MmrDelta = delta >= 0 ? $"+{delta} (prev)" : $"{delta} (prev)";
            }
            else
            {
                MmrDelta = "0 (prev)";
            }

            // Simple Rank calculation logic (placeholders for now)
            RankTitle = GetRankTitle(latest.Mmr);

            // Session Stats (last 20 matches as "session" for now)
            var sessionRecords = records.Take(20).ToList();
            SessionWins = sessionRecords.Count(r => r.Winner);
            SessionLosses = sessionRecords.Count(r => !r.Winner);
            var total = SessionWins + SessionLosses;
            WinRate = total > 0 ? $"{(double)SessionWins / total:P0}" : "0%";
        }

        UpdateChart(records);
    }

    private string GetRankTitle(int mmr)
    {
        if (mmr < 770) return "HERALD";
        if (mmr < 1540) return "GUARDIAN";
        if (mmr < 2310) return "CRUSADER";
        if (mmr < 3080) return "ARCHON";
        if (mmr < 3850) return "LEGEND";
        if (mmr < 4620) return "ANCIENT";
        if (mmr < 5420) return "DIVINE";
        return "IMMORTAL";
    }

    private void UpdateChart(System.Collections.Generic.List<MmrRecord> records)
    {
        if (records.Count == 0)
        {
            MmrSeries = [];
            return;
        }

        var ordered = records.AsEnumerable().Reverse().TakeLast(10).ToList();

        var values = ordered.Select(r => (double)r.Mmr).ToArray();
        var labels = ordered.Select(r => r.Timestamp.ToString("MMM_dd").ToUpper()).ToArray();

        MmrSeries =
        [
            new LineSeries<double>
            {
                Values = values,
                Name = "MMR Delta",
                Fill = null,
                GeometrySize = 10,
                Stroke = new SolidColorPaint(SKColor.Parse("#38BDF8"), 3),
                GeometryStroke = new SolidColorPaint(SKColor.Parse("#38BDF8"), 3),
                GeometryFill = new SolidColorPaint(SKColor.Parse("#38BDF8")),
                LineSmoothness = 0,
            }
        ];

        XAxes =
        [
            new Axis
            {
                Labels = labels,
                LabelsRotation = 0,
                TextSize = 12,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#94A3B8")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#1E293B")),
            }
        ];

        YAxes =
        [
            new Axis
            {
                TextSize = 12,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#94A3B8")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#1E293B")),
                Labeler = value => value.ToString("N0")
            }
        ];
    }
}
