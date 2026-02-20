namespace TrackMmr;

public record MmrRecord(
    DateTime Timestamp,
    ulong MatchId,
    int Mmr,
    int MmrChange,
    int HeroId,
    bool Winner,
    int? Kills = null,
    int? Deaths = null,
    int? Assists = null,
    int? Duration = null
)
{
    public string GetOutcomeDisplay() => Winner ? "Win" : "Loss";

    public string GetMmrChangeDisplay()
    {
        if (MmrChange == 0) return "  0";
        return MmrChange > 0 ? $"+{MmrChange}" : $"{MmrChange}";
    }

    public string GetKdaDisplay() =>
        Kills.HasValue ? $"{Kills}/{Deaths}/{Assists}" : "-";

    public string GetDurationDisplay() =>
        Duration.HasValue ? $"{Duration.Value / 60}:{Duration.Value % 60:D2}" : "-";
}
