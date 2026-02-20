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
                winner INTEGER NOT NULL,
                kills INTEGER,
                deaths INTEGER,
                assists INTEGER,
                duration INTEGER
            );
            """;
        command.ExecuteNonQuery();

        // Migrate: add new columns if they don't exist
        foreach (var col in new[] { "kills", "deaths", "assists", "duration" })
        {
            try
            {
                using var alter = connection.CreateCommand();
                alter.CommandText = $"ALTER TABLE mmr_history ADD COLUMN {col} INTEGER;";
                alter.ExecuteNonQuery();
            }
            catch (SqliteException)
            {
                // Column already exists
            }
        }
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
                INSERT OR IGNORE INTO mmr_history (timestamp, match_id, mmr, mmr_change, hero_id, winner, kills, deaths, assists, duration)
                VALUES (@timestamp, @match_id, @mmr, @mmr_change, @hero_id, @winner, @kills, @deaths, @assists, @duration);
                """;
            command.Parameters.AddWithValue("@timestamp", record.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@match_id", (long)record.MatchId);
            command.Parameters.AddWithValue("@mmr", (long)record.Mmr);
            command.Parameters.AddWithValue("@mmr_change", (long)record.MmrChange);
            command.Parameters.AddWithValue("@hero_id", (long)record.HeroId);
            command.Parameters.AddWithValue("@winner", record.Winner ? 1L : 0L);
            command.Parameters.AddWithValue("@kills", record.Kills.HasValue ? (object)(long)record.Kills.Value : DBNull.Value);
            command.Parameters.AddWithValue("@deaths", record.Deaths.HasValue ? (object)(long)record.Deaths.Value : DBNull.Value);
            command.Parameters.AddWithValue("@assists", record.Assists.HasValue ? (object)(long)record.Assists.Value : DBNull.Value);
            command.Parameters.AddWithValue("@duration", record.Duration.HasValue ? (object)(long)record.Duration.Value : DBNull.Value);
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
                SELECT timestamp, match_id, mmr, mmr_change, hero_id, winner, kills, deaths, assists, duration
                FROM mmr_history
                WHERE timestamp >= @cutoff
                ORDER BY timestamp DESC;
                """;
            command.Parameters.AddWithValue("@cutoff", DateTime.Now.AddDays(-days.Value).ToString("yyyy-MM-dd HH:mm:ss"));
        }
        else
        {
            command.CommandText = """
                SELECT timestamp, match_id, mmr, mmr_change, hero_id, winner, kills, deaths, assists, duration
                FROM mmr_history
                ORDER BY timestamp DESC;
                """;
        }

        var records = new List<MmrRecord>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            records.Add(new MmrRecord(
                DateTime.Parse(reader.GetString(0)),
                (ulong)reader.GetInt64(1),
                (int)reader.GetInt64(2),
                (int)reader.GetInt64(3),
                (int)reader.GetInt64(4),
                reader.GetInt64(5) == 1,
                reader.IsDBNull(6) ? null : (int)reader.GetInt64(6),
                reader.IsDBNull(7) ? null : (int)reader.GetInt64(7),
                reader.IsDBNull(8) ? null : (int)reader.GetInt64(8),
                reader.IsDBNull(9) ? null : (int)reader.GetInt64(9)
            ));
        }

        return records;
    }

    public void UpdateMatchDetails(ulong matchId, int kills, int deaths, int assists, int duration)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE mmr_history
            SET kills = @kills, deaths = @deaths, assists = @assists, duration = @duration
            WHERE match_id = @match_id;
            """;
        command.Parameters.AddWithValue("@match_id", (long)matchId);
        command.Parameters.AddWithValue("@kills", (long)kills);
        command.Parameters.AddWithValue("@deaths", (long)deaths);
        command.Parameters.AddWithValue("@assists", (long)assists);
        command.Parameters.AddWithValue("@duration", (long)duration);
        command.ExecuteNonQuery();
    }

    public List<ulong> GetMatchIdsWithoutDetails()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT match_id FROM mmr_history
            WHERE kills IS NULL
            ORDER BY timestamp DESC;
            """;

        var ids = new List<ulong>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            ids.Add((ulong)reader.GetInt64(0));
        }

        return ids;
    }
}
