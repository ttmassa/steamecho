using System.IO;
using System.Net.Http;
using System.Text.Json;
using SteamEcho.App.DTOs;
using SteamEcho.Core.Models;

namespace SteamEcho.App.Services;

public class SteamService : ISteamService
{
    private readonly string _steamApiKey;
    
    public SteamService()
    {
        // Load API key from config
        _steamApiKey = AppConfig.Load().SteamAPI.Key;
    }

    public async Task<GameInfo> ResolveSteamIdAsync(string gameName)
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
                throw new InvalidDataException("No content received from Steam API.");
            }

            // Get first result from
            using var doc = JsonDocument.Parse(content);
            var items = doc.RootElement.GetProperty("items");
            if (items.GetArrayLength() == 0)
            {
                Console.WriteLine("No Steam game with that name exists.");
                throw new InvalidDataException("No Steam game found with the specified name.");
            }

            // Get steam ID from the first game
            var firstGame = items[0];
            string steamId = firstGame.GetProperty("id").GetInt32().ToString() ?? throw new InvalidDataException("Steam ID not found in search result.");
            string name = firstGame.GetProperty("name").GetString() ?? "Unknown Name";
            string iconUrl = $"https://cdn.cloudflare.steamstatic.com/steam/apps/{steamId}/library_600x900.jpg";

            // Create and return GameInfo object
            return new GameInfo(steamId, name, iconUrl);
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("Error: " + e.Message);
            throw new InvalidOperationException("Failed to resolve Steam ID.", e);
        }
    }

    public async Task<List<Achievement>> GetAchievementsAsync(string steamId)
    {
        // Fetch achievements for a game
        HttpClient client = new();
        var achievements = new List<Achievement>();
        string url = $"https://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/?key={_steamApiKey}&appid={steamId}";

        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string content = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(content);
            // Check if the JSON structure is as expected
            if (doc.RootElement.TryGetProperty("game", out var gameElement) &&
                gameElement.TryGetProperty("availableGameStats", out var statsElement) &&
                statsElement.TryGetProperty("achievements", out var achievementsElement))
            {
                foreach (var achievement in achievementsElement.EnumerateArray())
                {
                    var id = achievement.GetProperty("name").GetString() ?? throw new InvalidDataException("Achievement ID not found.");
                    var name = achievement.GetProperty("displayName").GetString() ?? throw new InvalidDataException("Achievement name not found.");
                    var description = achievement.TryGetProperty("description", out var descElement) ? descElement.GetString() : "Hidden Achievement";
                    if (string.IsNullOrEmpty(description))
                    {
                        description = "Hidden Achievement";
                    }
                    // Optional properties
                    var icon = achievement.GetProperty("icon").GetString();
                    var grayIcon = achievement.GetProperty("icongray").GetString();

                    achievements.Add(new Achievement(id, name, description, icon, grayIcon));
                }
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("Error: " + e.Message);
        }

        return achievements;
    }
}