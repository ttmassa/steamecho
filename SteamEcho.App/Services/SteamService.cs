using System.IO;
using System.Net.Http;
using System.Text.Json;
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

    public async Task<string?> ResolveSteamIdAsync(string gameName)
    {
        HttpClient client = new();
        string url = $"https://store.steampowered.com/api/storesearch/?term={gameName}&cc=us&l=en";

        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync();
            return content;
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("Error: " + e.Message);
            return null;
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
                    var description = achievement.GetProperty("description").GetString() ?? throw new InvalidDataException("Achievement description not found.");
                    // Optional properties
                    var icon = achievement.GetProperty("icon").GetString();
                    var grayIcon = achievement.GetProperty("iconGray").GetString();

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