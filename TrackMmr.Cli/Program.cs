using TrackMmr;

var dbPath = Path.Combine(AppContext.BaseDirectory, "mmr.db");
var db = new MmrDatabase(dbPath);
db.EnsureCreated();

if (args.Length > 0 && args[0].Equals("history", StringComparison.OrdinalIgnoreCase))
{
    int? days = args.Length > 1 && int.TryParse(args[1], out var d) ? d : null;
    var history = db.GetHistory(days);
    DisplayHistory(history, days);
    return;
}

// "login" command: reset credentials
var config = args.Length > 0 && args[0].Equals("login", StringComparison.OrdinalIgnoreCase)
    ? AppConfig.PromptCredentials()
    : AppConfig.Load();

using var steam = new SteamService(config);
var records = await steam.FetchMmrAsync();
var inserted = db.SaveRecords(records);

Console.WriteLine();
Console.WriteLine($"Fetched {records.Count} ranked matches, {inserted} new records saved.");

if (records.Count > 0)
{
    var latest = records[0];
    Console.WriteLine($"Current MMR: {latest.Mmr}");
}

Console.WriteLine();
var recent = db.GetHistory(30);
DisplayHistory(recent, 30);

static void DisplayHistory(List<MmrRecord> records, int? days)
{
    if (records.Count == 0)
    {
        Console.WriteLine(days.HasValue
            ? $"No MMR records found in the last {days} days."
            : "No MMR records found.");
        return;
    }

    var header = days.HasValue ? $"MMR History (last {days} days)" : "MMR History (all time)";
    Console.WriteLine(header);
    Console.WriteLine(new string('=', header.Length));
    Console.WriteLine();
    Console.WriteLine($"{"Date",-21}| {"Match ID",-14}| {"MMR",-7}| {"Change",-8}| {"Hero",-20}| Result");
    Console.WriteLine(new string('-', 83));

    foreach (var r in records)
    {
        Console.WriteLine($"{r.Timestamp:yyyy-MM-dd HH:mm:ss}  | {r.MatchId,-14}| {r.Mmr,-7}| {r.GetMmrChangeDisplay(),-8}| {HeroNames.Get(r.HeroId),-20}| {r.GetOutcomeDisplay()}");
    }
}
