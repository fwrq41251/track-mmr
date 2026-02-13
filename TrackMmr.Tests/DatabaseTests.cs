using Xunit;
using TrackMmr;

namespace TrackMmr.Tests;

public class DatabaseTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly MmrDatabase _db;

    public DatabaseTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"mmr_test_{Guid.NewGuid()}.db");
        _db = new MmrDatabase(_testDbPath);
        _db.EnsureCreated();
    }

    public void Dispose()
    {
        if (File.Exists(_testDbPath))
        {
            File.Delete(_testDbPath);
        }
    }

    [Fact]
    public void EnsureCreated_ShouldCreateEmptyTable()
    {
        var history = _db.GetHistory();
        Assert.Empty(history);
    }

    [Fact]
    public void SaveRecords_ShouldInsertAndPreventDuplicates()
    {
        var records = new List<MmrRecord>
        {
            new MmrRecord(DateTime.Now, 12345, 6000, 25, 1, true),
            new MmrRecord(DateTime.Now.AddHours(-1), 12344, 5975, -25, 2, false)
        };

        var inserted = _db.SaveRecords(records);
        Assert.Equal(2, inserted);

        // Try insert same records again
        var reinserted = _db.SaveRecords(records);
        Assert.Equal(0, reinserted);
    }

    [Fact]
    public void GetHistory_ShouldFilterByDays()
    {
        var oldDate = DateTime.Now.AddDays(-10);
        var newDate = DateTime.Now;

        var records = new List<MmrRecord>
        {
            new MmrRecord(newDate, 101, 6000, 25, 1, true),
            new MmrRecord(oldDate, 102, 5975, 25, 1, true)
        };

        _db.SaveRecords(records);

        var allHistory = _db.GetHistory();
        var recentHistory = _db.GetHistory(days: 5);

        Assert.Equal(2, allHistory.Count);
        Assert.Single(recentHistory);
        Assert.Equal(101u, recentHistory[0].MatchId);
    }
}
