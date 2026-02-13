using Avalonia.Media;

namespace TrackMmr.Desktop.ViewModels;

public class MatchRowViewModel
{
    public MatchRowViewModel(TrackMmr.MmrRecord record)
    {
        Date = record.Timestamp.ToString("yyyy-MM-dd HH:mm");
        MatchId = record.MatchId.ToString();
        Mmr = record.Mmr.ToString();
        MmrChange = record.GetMmrChangeDisplay();
        Hero = TrackMmr.HeroNames.Get(record.HeroId);
        Result = record.GetOutcomeDisplay();
        IsWin = record.Winner;
        ResultBrush = IsWin
            ? new SolidColorBrush(Color.Parse("#4CAF50"))
            : new SolidColorBrush(Color.Parse("#F44336"));
        RowBackground = IsWin
            ? new SolidColorBrush(Color.Parse("#154CAF50"))
            : new SolidColorBrush(Color.Parse("#15F44336"));
    }

    public string Date { get; }
    public string MatchId { get; }
    public string Mmr { get; }
    public string MmrChange { get; }
    public string Hero { get; }
    public string Result { get; }
    public bool IsWin { get; }
    public IBrush ResultBrush { get; }
    public IBrush RowBackground { get; }
}
