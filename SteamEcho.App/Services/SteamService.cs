using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using SteamEcho.Core.DTOs;
using SteamEcho.Core.Models;
using SteamEcho.Core.Services;

namespace SteamEcho.App.Services;

public class SteamService : ISteamService
{
    private readonly HttpClient _client;
    private readonly string _backendBaseUrl;

    public SteamService()
    {
        _client = new HttpClient();

        // Prefer environment variable, fallback to default
        var envUrl = Environment.GetEnvironmentVariable("STEAMECHO_BACKEND_URL");
        if (!string.IsNullOrWhiteSpace(envUrl))
        {
            _backendBaseUrl = envUrl;
        }
#if DEBUG
        else
        {
            _backendBaseUrl = "http://localhost:5149/api/steam";
        }
#else
        else
        {
            _backendBaseUrl = "https://steamecho.azurewebsites.net/api/steam";
        }
#endif

        // Throw if running locally and using prod URL
        if (IsLocalDev() && _backendBaseUrl.Contains("azurewebsites.net"))
        {
            throw new InvalidOperationException("Local development must not use the production backend URL.");
        }
    }

    // Simple check to see if running in local dev environment
    private static bool IsLocalDev()
    {
        // Check for common local dev indicators
        string machineName = Environment.MachineName.ToLowerInvariant();
        string userDomain = Environment.UserDomainName.ToLowerInvariant();
        string userName = Environment.UserName.ToLowerInvariant();
        bool isDebug = false;
#if DEBUG
        isDebug = true;
#endif
        return isDebug ||
               machineName == "localhost" ||
               userDomain == "localhost" ||
               userName == "developer" ||
               Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
    }

    // Search for Steam games by name and return a list
    public async Task<List<GameInfo>> SearchSteamGamesAsync(string gameName)
    {
        string url = $"{_backendBaseUrl}/search?term={Uri.EscapeDataString(gameName)}";
        var games = new List<GameInfo>();

        try
        {
            HttpResponseMessage response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var items = doc.RootElement.GetProperty("items");
            foreach (var item in items.EnumerateArray())
            {
                long id = item.GetProperty("id").GetInt64();
                string name = item.GetProperty("name").GetString() ?? "Unknown Name";
                string? iconUrl = $"https://cdn.cloudflare.steamstatic.com/steam/apps/{id}/library_600x900.jpg";
                games.Add(new GameInfo(id, name, iconUrl));
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Error: {e.Message} (URL: {url})");
        }
        return games;
    }

    // Fetch achievements for a game and optionally check if they are unlocked for a user
    public async Task<List<Achievement>> GetAchievementsAsync(long gameId, SteamUserInfo? user = null)
    {
        var achievements = new List<Achievement>();

        string schemaUrl = $"{_backendBaseUrl}/schema?appid={gameId}";
        string globalUrl = $"{_backendBaseUrl}/globalpercentages?appid={gameId}";
        string? playerUrl = user != null ? $"{_backendBaseUrl}/achievements?appid={gameId}&steamid={user.SteamId}" : null;

        try
        {
            // Get schema
            var schemaResponse = await _client.GetAsync(schemaUrl);
            if (schemaResponse.StatusCode == HttpStatusCode.Forbidden)
                return achievements; // No achievements, don't log error
            schemaResponse.EnsureSuccessStatusCode();
            string schemaContent = await schemaResponse.Content.ReadAsStringAsync();

            // Get global percentages
            var globalResponse = await _client.GetAsync(globalUrl);
            if (globalResponse.StatusCode == HttpStatusCode.Forbidden)
                // No achievements, don't log error
                return achievements;
            globalResponse.EnsureSuccessStatusCode();
            string globalContent = await globalResponse.Content.ReadAsStringAsync();

            // Get player achievements if user is provided
            Dictionary<string, PlayerAchievementStatus> playerAchievements = [];
            if (playerUrl != null)
            {
                var playerResponse = await _client.GetAsync(playerUrl);
                if (playerResponse.StatusCode == HttpStatusCode.Forbidden)
                    return achievements; // No achievements, don't log error
                playerResponse.EnsureSuccessStatusCode();
                string playerContent = await playerResponse.Content.ReadAsStringAsync();
                using var playerDoc = JsonDocument.Parse(playerContent);
                if (playerDoc.RootElement.TryGetProperty("playerstats", out var playerStatsElement) &&
                    playerStatsElement.TryGetProperty("achievements", out var achievementsList))
                {
                    foreach (var achievement in achievementsList.EnumerateArray())
                    {
                        var apiName = achievement.GetProperty("apiname").GetString();
                        var isUnlocked = achievement.GetProperty("achieved").GetInt32() == 1;
                        var unlockDate = achievement.TryGetProperty("unlocktime", out var unlockTimeElement) ? DateTimeOffset.FromUnixTimeSeconds(unlockTimeElement.GetInt64()).UtcDateTime : (DateTime?)null;
                        if (!string.IsNullOrEmpty(apiName))
                        {
                            playerAchievements[apiName] = new PlayerAchievementStatus(isUnlocked, unlockDate);
                        }
                    }
                }
            }

            // Parse global percentages
            Dictionary<string, double> globalPercentagesDict = [];
            using var globalDoc = JsonDocument.Parse(globalContent);
            if (globalDoc.RootElement.TryGetProperty("achievementpercentages", out var percentagesElement) &&
                percentagesElement.TryGetProperty("achievements", out var achievementsElement))
            {
                foreach (var achievement in achievementsElement.EnumerateArray())
                {
                    var id = achievement.GetProperty("name").GetString();
                    var percentStr = achievement.GetProperty("percent").GetString();
                    if (!string.IsNullOrEmpty(id) && double.TryParse(percentStr, out var percentValue))
                    {
                        globalPercentagesDict[id] = percentValue;
                    }
                }
            }

            // Parse schema
            using var doc = JsonDocument.Parse(schemaContent);
            if (doc.RootElement.TryGetProperty("game", out var gameElement) &&
                gameElement.TryGetProperty("availableGameStats", out var statsElement) &&
                statsElement.TryGetProperty("achievements", out var schemaAchievementsElement)) // <-- renamed here
            {
                foreach (var achievement in schemaAchievementsElement.EnumerateArray()) // <-- and here
                {
                    var id = achievement.GetProperty("name").GetString() ?? throw new InvalidDataException("Achievement ID not found.");
                    var name = achievement.GetProperty("displayName").GetString() ?? throw new InvalidDataException("Achievement name not found.");
                    var description = achievement.TryGetProperty("description", out var descElement) ? descElement.GetString() : "Hidden Achievement";
                    if (string.IsNullOrEmpty(description))
                        description = "Hidden Achievement";
                    var icon = achievement.GetProperty("icon").GetString();
                    var grayIcon = achievement.GetProperty("icongray").GetString();
                    var isHidden = achievement.TryGetProperty("hidden", out var hiddenElement) && hiddenElement.GetInt32() == 1;
                    double? globalPercentage = null;
                    if (globalPercentagesDict.TryGetValue(id, out var percent))
                        globalPercentage = percent;

                    var newAchievement = new Achievement(id, name, description, icon, grayIcon, isHidden, globalPercentage);

                    if (playerAchievements.TryGetValue(id, out var status))
                    {
                        newAchievement.IsUnlocked = status.IsUnlocked;
                        newAchievement.UnlockDate = status.UnlockDate;
                    }

                    achievements.Add(newAchievement);
                }
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Error: {e.Message} (URL: {schemaUrl})");
        }

        return achievements;
    }

    public async Task<SteamUserInfo?> LogToSteamAsync()
    {
        // OpenId authentication to Steam with system browser + local listener
        string redirectUrl = "http://localhost:54321/steam-auth/";
        string openIdUrl = "https://steamcommunity.com/openid/login" +
        "?openid.ns=http://specs.openid.net/auth/2.0" +
        "&openid.mode=checkid_setup" +
        "&openid.return_to=" + WebUtility.UrlEncode(redirectUrl) +
        "&openid.realm=" + Uri.EscapeDataString("http://localhost:54321/") +
        "&openid.identity=http://specs.openid.net/auth/2.0/identifier_select" +
        "&openid.claimed_id=http://specs.openid.net/auth/2.0/identifier_select";

        // Start HTTP listener
        using var listener = new HttpListener();
        listener.Prefixes.Add(redirectUrl);
        listener.Start();

        // Open browser
        Process.Start(new ProcessStartInfo
        {
            FileName = openIdUrl,
            UseShellExecute = true
        });

        // Wait for redirection
        var context = await listener.GetContextAsync();
        var request = context.Request;

        // Send a response to the browser
        var response = context.Response;
        string responseString = @"
            <html>
                <head>
                    <title>Steam Echo</title>
                    <meta http-equiv='refresh' content='0;url=steamecho://auth'>
                    <style>
                        body {
                            margin: 0;
                            display: flex;
                            justify-content: center;
                            align-items: center;
                            height: 100vh;
                            background-color: #323234;
                            color: #F0F0F0;
                        }
                        .container {
                            display: flex;
                            flex-direction: column;
                            align-items: center;
                            justify-content: center;
                        }
                        h1 {
                            font-size: 2em;
                            margin-bottom: 20px;
                        }
                        p {
                            font-size: 1.2em;
                        }
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h1>Authentication successful!</h1>
                        <p>You can close this window and return to the application.</p>
                    </div>
                </body>
            </html>";
        var buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        var responseOutput = response.OutputStream;
        await responseOutput.WriteAsync(buffer);
        responseOutput.Close();
        listener.Stop();

        // Extract Steam ID from OpenID response
        var query = request.QueryString;
        string? claimedId = query["openid.claimed_id"];

        // Validate Steam ID
        using var client = new HttpClient();
        var values = new List<KeyValuePair<string, string>>();
        foreach (string? key in query.AllKeys)
        {
            if (key != null)
            {
                if (key == "openid.mode")
                {
                    values.Add(new KeyValuePair<string, string>(key, "check_authentication"));
                }
                else
                {
                    values.Add(new KeyValuePair<string, string>(key, query[key]!));
                }
            }
        }
        var content = new FormUrlEncodedContent(values);

        var verifyResponse = await client.PostAsync("https://steamcommunity.com/openid/login", content);
        string verifyString = await verifyResponse.Content.ReadAsStringAsync();
        if (!verifyString.Contains("is_valid:true"))
        {
            Console.WriteLine("Error: Invalid OpenID response from Steam.");
            return null;
        }

        // Extract Steam ID from the OpenID URL
        if (!string.IsNullOrEmpty(claimedId))
        {
            var parts = claimedId.Split('/');
            string steamId = parts.Last();
            return new SteamUserInfo(steamId);
        }

        Console.WriteLine("Error: No Steam ID found in OpenID response.");
        return null;
    }

    // Get owned Steam owned games with achievements (full game objects)
    public async Task<List<Game>> GetOwnedGamesAsync(SteamUserInfo user)
    {
        string url = $"{_backendBaseUrl}/ownedgames?steamid={user.SteamId}";
        var games = new List<Game>();

        try
        {
            HttpResponseMessage response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string content = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("response", out var responseElement) &&
                responseElement.TryGetProperty("games", out var gamesElement))
            {
                foreach (var game in gamesElement.EnumerateArray())
                {
                    long gameId = game.GetProperty("appid").GetInt64();
                    string name = game.GetProperty("name").GetString() ?? "Unknown Game";
                    string? iconUrl = $"https://cdn.cloudflare.steamstatic.com/steam/apps/{gameId}/library_600x900.jpg";
                    string? exePath = null;
                    if (game.TryGetProperty("playtime_forever", out var playtimeElement) && playtimeElement.GetInt32() > 0)
                    {
                        string steamLibraryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common");
                        string gameFolder = Path.Combine(steamLibraryPath, name);
                        if (Directory.Exists(gameFolder))
                        {
                            var exeFiles = Directory.GetFiles(gameFolder, "*.exe", SearchOption.TopDirectoryOnly);
                            if (exeFiles.Length > 0)
                            {
                                exePath = exeFiles[0];
                            }
                        }
                    }
                    var achievements = await GetAchievementsAsync(gameId, user);
                    if (achievements.Count > 0)
                    {
                        var gameWithAchievements = new Game(gameId, name, achievements, exePath, iconUrl);
                        games.Add(gameWithAchievements);
                    }
                }
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Error: {e.Message} (URL: {url})");
        }
        return games;
    }
}