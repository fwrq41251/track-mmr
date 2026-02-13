using System.Text.Json;

namespace TrackMmr;

public class AppConfig
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string RefreshToken { get; set; } = "";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public static string GetConfigPath()
    {
        var dir = AppContext.BaseDirectory;
        return Path.Combine(dir, "config.json");
    }

    public static AppConfig Load()
    {
        var path = GetConfigPath();
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
            if (config != null && !string.IsNullOrEmpty(config.Username))
                return config;
        }

        return PromptCredentials();
    }

    public static AppConfig PromptCredentials()
    {
        Console.Write("Steam username: ");
        var username = Console.ReadLine()?.Trim() ?? "";
        Console.Write("Steam password: ");
        var password = Console.ReadLine()?.Trim() ?? "";

        return new AppConfig { Username = username, Password = password };
    }

    public void Save()
    {
        var path = GetConfigPath();
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(path, json);
    }
}
