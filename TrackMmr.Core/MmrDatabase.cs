using Microsoft.Data.Sqlite;

namespace TrackMmr;

public class MmrDatabase
{
    private readonly string _connectionString;

    public MmrDatabase(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
    }

    public void EnsureCreated()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS mmr_history (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                timestamp TEXT NOT NULL,
                match_id INTEGER NOT NULL UNIQUE,
                mmr INTEGER NOT NULL,
                mmr_change INTEGER NOT NULL,
                hero_id INTEGER NOT NULL,
                winner INTEGER NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    public int SaveRecords(List<MmrRecord> records)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var inserted = 0;
        foreach (var record in records)
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT OR IGNORE INTO mmr_history (timestamp, match_id, mmr, mmr_change, hero_id, winner)
                VALUES (@timestamp, @match_id, @mmr, @mmr_change, @hero_id, @winner);
                """;
            command.Parameters.AddWithValue("@timestamp", record.Timestamp.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@match_id", (long)record.MatchId);
            command.Parameters.AddWithValue("@mmr", (long)record.Mmr);
            command.Parameters.AddWithValue("@mmr_change", (long)record.MmrChange);
            command.Parameters.AddWithValue("@hero_id", (long)record.HeroId);
            command.Parameters.AddWithValue("@winner", record.Winner ? 1L : 0L);
            inserted += command.ExecuteNonQuery();
        }

        return inserted;
    }

    public List<MmrRecord> GetHistory(int? days = null)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        if (days.HasValue)
        {
            command.CommandText = """
                SELECT timestamp, match_id, mmr, mmr_change, hero_id, winner
                FROM mmr_history
                WHERE timestamp >= @cutoff
                ORDER BY timestamp DESC;
                """;
            command.Parameters.AddWithValue("@cutoff", DateTime.UtcNow.AddDays(-days.Value).ToString("yyyy-MM-dd HH:mm:ss"));
        }
        else
        {
            command.CommandText = """
                SELECT timestamp, match_id, mmr, mmr_change, hero_id, winner
                FROM mmr_history
                ORDER BY timestamp DESC;
                """;
        }

        var records = new List<MmrRecord>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            records.Add(new MmrRecord(
                DateTime.Parse(reader.GetString(0)).ToLocalTime(),
                (ulong)reader.GetInt64(1),
                (int)reader.GetInt64(2),
                (int)reader.GetInt64(3),
                (int)reader.GetInt64(4),
                reader.GetInt64(5) == 1
            ));
        }

        return records;
    }
}
