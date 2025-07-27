using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using SteamEcho.Core.DTOs;
using SteamEcho.Core.Exceptions;
using SteamEcho.Core.Models;
using SteamEcho.Core.Services;

namespace SteamEcho.App.Services;

public class SteamService : ISteamService
{
    private readonly string _steamApiKey;

    public SteamService()
    {
        // Load API key from config
        try
        {
            _steamApiKey = AppConfig.Load().SteamAPI.Key;

            if (string.IsNullOrEmpty(_steamApiKey))
            {
                throw new MissingApiKeyException();
            }
        }
        catch (Exception e)
        {
            throw new MissingApiKeyException("Failed to load Steam API key from configuration.", e);
        }
    }

    public async Task<GameInfo?> ResolveSteamIdAsync(string gameName)
    {
        HttpClient client = new();
        string url = $"https://store.steampowered.com/api/storesearch/?term={gameName}&cc=us&l=en";

        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(content))
            {
                Console.WriteLine("No content returned from Steam API.");
                return null;
            }

            // Get first result from
            using var doc = JsonDocument.Parse(content);
            var items = doc.RootElement.GetProperty("items");
            if (items.GetArrayLength() == 0)
            {
                Console.WriteLine("No Steam game with that name exists.");
                return null;
            }

            // Get steam ID from the first game
            var firstGame = items[0];
            long claimedId = firstGame.GetProperty("id").GetInt64();
            string name = firstGame.GetProperty("name").GetString() ?? "Unknown Name";
            string? iconUrl = $"https://cdn.cloudflare.steamstatic.com/steam/apps/{claimedId}/library_600x900.jpg";

            // Check if the icon URL is valid
            using var request = new HttpRequestMessage(HttpMethod.Head, iconUrl);
            HttpResponseMessage iconResponse = await client.SendAsync(request);
            if (!iconResponse.IsSuccessStatusCode)
            {
                iconUrl = null;
            }

            // Create and return GameInfo object
            return new GameInfo(claimedId, name, iconUrl);
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("Error: " + e.Message);
            return null;
        }
    }

    public async Task<List<Achievement>> GetAchievementsAsync(long gameId, SteamUserInfo? user = null)
    {
        // Fetch achievements for a game
        HttpClient client = new();
        var achievements = new List<Achievement>();
        string url = $"https://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/?key={_steamApiKey}&appid={gameId}";

        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(content))
            {
                return achievements;
            }

            using var doc = JsonDocument.Parse(content);
            // Check if the JSON structure is as expected
            if (doc.RootElement.TryGetProperty("game", out var gameElement) &&
                gameElement.TryGetProperty("availableGameStats", out var statsElement) &&
                statsElement.TryGetProperty("achievements", out var achievementsElement))
            {
                // First get global achievement percentages
                var globalPercentagesDict = await GetGlobalAchievementPercentagesDictAsync(gameId);

                // If user is logged in, fetch player achievements
                Dictionary<string, PlayerAchievementStatus> playerAchievements = new();
                if (user != null)
                {
                    playerAchievements = await GetPlayerAchievementsAsync(gameId, user);
                }

                // Create Achievement instances from the JSON data
                foreach (var achievement in achievementsElement.EnumerateArray())
                {
                    var id = achievement.GetProperty("name").GetString() ?? throw new InvalidDataException("Achievement ID not found.");
                    var name = achievement.GetProperty("displayName").GetString() ?? throw new InvalidDataException("Achievement name not found.");
                    // Try to get description because hidden achievements don't have it
                    var description = achievement.TryGetProperty("description", out var descElement) ? descElement.GetString() : "Hidden Achievement";
                    if (string.IsNullOrEmpty(description))
                    {
                        description = "Hidden Achievement";
                    }
                    // Optional properties
                    var icon = achievement.GetProperty("icon").GetString();
                    var grayIcon = achievement.GetProperty("icongray").GetString();
                    var isHidden = achievement.TryGetProperty("hidden", out var hiddenElement) && hiddenElement.GetInt32() == 1;
                    // Try to get global percentage
                    double? globalPercentage = null;
                    if (globalPercentagesDict.TryGetValue(id, out var percent))

                    {
                        globalPercentage = percent;
                    }

                    var newAchievement = new Achievement(id, name, description, icon, grayIcon, isHidden, globalPercentage);

                    // If user is logged in, check if the achievement is already unlocked
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
            Console.WriteLine("Error: " + e.Message);
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

    public async Task<List<Game>> GetOwnedGamesAsync(SteamUserInfo user)
    {
        HttpClient client = new();
        string url = $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={_steamApiKey}&steamid={user.SteamId}&include_appinfo=true&format=json";
        var games = new List<Game>();

        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(content))
            {
                Console.WriteLine("Error: No content returned from Steam API for owned games.");
                return games;
            }

            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("response", out var responseElement) &&
                responseElement.TryGetProperty("games", out var gamesElement))
            {
                foreach (var game in gamesElement.EnumerateArray())
                {
                    long gameId = game.GetProperty("appid").GetInt64();
                    string name = game.GetProperty("name").GetString() ?? "Unknown Game";
                    string? iconUrl = $"https://cdn.cloudflare.steamstatic.com/steam/apps/{gameId}/library_600x900.jpg";

                    // Check if the icon URL is valid
                    using var request = new HttpRequestMessage(HttpMethod.Head, iconUrl);
                    HttpResponseMessage iconResponse = await client.SendAsync(request);
                    if (!iconResponse.IsSuccessStatusCode)
                    {
                        iconUrl = null;
                    }

                    // Try to get executable path
                    string? exePath = null;
                    if (game.TryGetProperty("playtime_forever", out var playtimeElement) && playtimeElement.GetInt32() > 0)
                    {
                        // Attempt to get the executable path from the local Steam library
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

                    // Fetch achievements for the game
                    var achievements = await GetAchievementsAsync(gameId, user);
                    if (achievements.Count > 0)
                    {
                        var gameWithAchievements = new Game(gameId, name, achievements, exePath, iconUrl);
                        games.Add(gameWithAchievements);
                    }
                }
            }
            else
            {
                Console.WriteLine("Warning: No games found in the response.");
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("Error: " + e.Message);
        }
        return games;
    }

    // Helper method to get global achievement percentages as a dictionary
    private static async Task<Dictionary<string, double>> GetGlobalAchievementPercentagesDictAsync(long gameId)
    {
        HttpClient client = new();
        string url = $"https://api.steampowered.com/ISteamUserStats/GetGlobalAchievementPercentagesForApp/v2/?gameid={gameId}";
        var dict = new Dictionary<string, double>();

        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(content))
            {
                Console.WriteLine("Error: No content returned from Steam API for global achievement percentages.");
                return dict;
            }

            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("achievementpercentages", out var percentagesElement) &&
                percentagesElement.TryGetProperty("achievements", out var achievementsElement))
            {
                foreach (var achievement in achievementsElement.EnumerateArray())
                {
                    var id = achievement.GetProperty("name").GetString();
                    var percentStr = achievement.GetProperty("percent").GetString();
                    if (!string.IsNullOrEmpty(id) && double.TryParse(percentStr, out var percentValue))
                    {
                        dict[id] = percentValue;
                    }
                }
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("Error: " + e.Message);
        }

        return dict;
    }

    private async Task<Dictionary<string, PlayerAchievementStatus>> GetPlayerAchievementsAsync(long gameId, SteamUserInfo user)
    {
        HttpClient client = new();
        string url = $"https://api.steampowered.com/ISteamUserStats/GetPlayerAchievements/v1/?key={_steamApiKey}&steamid={user.SteamId}&appid={gameId}";
        var playerAchievements = new Dictionary<string, PlayerAchievementStatus>();

        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string content = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(content))
            {
                Console.WriteLine("Error: No content returned from Steam API for player achievements.");
                return playerAchievements;
            }

            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("playerstats", out var playerStatsElement))
            {
                // Handle private profile exception
                if (playerStatsElement.TryGetProperty("success", out var successElement) && !successElement.GetBoolean())
                {
                    if (playerStatsElement.TryGetProperty("error", out var errorElement) && errorElement.GetString()?.Contains("Profile is not public") == true)
                    {
                        throw new PrivateProfileException();
                    }
                }

                if (playerStatsElement.TryGetProperty("achievements", out var achievementsElement))
                {
                    foreach (var achievement in achievementsElement.EnumerateArray())
                    {
                        var apiName = achievement.GetProperty("apiname").GetString();
                        var isUnlocked = achievement.GetProperty("achieved").GetInt32() == 1;
                        var unlockDate = achievement.TryGetProperty("unlocktime", out var unlockTimeElement) ? DateTimeOffset.FromUnixTimeSeconds(unlockTimeElement.GetInt64()).UtcDateTime : (DateTime?)null;

                        if (!string.IsNullOrEmpty(apiName))
                        {
                            playerAchievements[apiName] = new PlayerAchievementStatus(isUnlocked, unlockDate);
                        }
                        else
                        {
                            Console.WriteLine("Warning: Achievement API name is null or empty.");
                        }
                    }
                }
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("Error fetching player achievements: " + e.Message);
        }

        return playerAchievements;
    }
}