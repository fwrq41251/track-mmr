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
            CurrentMmr = records[0].Mmr.ToString();
        }

        UpdateChart(records);
    }

    private void UpdateChart(System.Collections.Generic.List<MmrRecord> records)
    {
        if (records.Count == 0)
        {
            MmrSeries = [];
            return;
        }

        var ordered = records.AsEnumerable().Reverse().ToList();

        var values = ordered.Select(r => (double)r.Mmr).ToArray();
        var labels = ordered.Select(r => r.Timestamp.ToString("MM/dd")).ToArray();

        MmrSeries =
        [
            new LineSeries<double>
            {
                Values = values,
                Name = "MMR",
                Fill = new SolidColorPaint(SKColors.DodgerBlue.WithAlpha(30)),
                GeometrySize = 8,
                Stroke = new SolidColorPaint(SKColors.DodgerBlue, 3),
                GeometryStroke = new SolidColorPaint(SKColors.DodgerBlue, 3),
                GeometryFill = new SolidColorPaint(SKColors.White),
                LineSmoothness = 0.35,
            }
        ];

        XAxes =
        [
            new Axis
            {
                Labels = labels,
                LabelsRotation = 0,
                TextSize = 11,
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                SeparatorsPaint = new SolidColorPaint(SKColors.DarkGray.WithAlpha(20)),
            }
        ];

        YAxes =
        [
            new Axis
            {
                TextSize = 11,
                LabelsPaint = new SolidColorPaint(SKColors.Gray),
                SeparatorsPaint = new SolidColorPaint(SKColors.DarkGray.WithAlpha(20)),
                Labeler = value => value.ToString("N0")
            }
        ];
    }
}
