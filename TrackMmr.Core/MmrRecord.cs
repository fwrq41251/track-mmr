namespace TrackMmr;

public record MmrRecord(
    DateTime Timestamp,
    ulong MatchId,
    int Mmr,
    int MmrChange,
    int HeroId,
    bool Winner
)
{
    public string GetOutcomeDisplay() => Winner ? "Win" : "Loss";

    public string GetMmrChangeDisplay()
    {
        if (MmrChange == 0) return "  0";
        return MmrChange > 0 ? $"+{MmrChange}" : $"{MmrChange}";
    }
}
