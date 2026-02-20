using System.Net.Http.Json;
using System.Text.Json;

namespace TrackMmr;

public class OpenDotaService
{
    private static readonly HttpClient HttpClient = new()
    {
        BaseAddress = new Uri("https://api.opendota.com/api/")
    };

    public record MatchDetails(ulong MatchId, int Kills, int Deaths, int Assists, int Duration);

    public async Task<Dictionary<ulong, MatchDetails>> FetchMatchDetailsAsync(uint accountId)
    {
        var url = $"players/{accountId}/matches";
        var response = await HttpClient.GetFromJsonAsync<JsonElement>(url);

        var result = new Dictionary<ulong, MatchDetails>();
        foreach (var match in response.EnumerateArray())
        {
            var matchId = match.GetProperty("match_id").GetUInt64();
            result[matchId] = new MatchDetails(
                matchId,
                match.GetProperty("kills").GetInt32(),
                match.GetProperty("deaths").GetInt32(),
                match.GetProperty("assists").GetInt32(),
                match.GetProperty("duration").GetInt32()
            );
        }

        return result;
    }
}
