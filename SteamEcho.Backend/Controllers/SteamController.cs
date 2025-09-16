using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;

namespace SteamEcho.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SteamController(IHttpClientFactory clientFactory, IConfiguration configuration) : ControllerBase
{
    private readonly IHttpClientFactory _clientFactory = clientFactory;
    private readonly IConfiguration _configuration = configuration;

    [HttpGet("ownedgames")]
    public async Task<IActionResult> GetOwnedGames([FromQuery] string steamid)
    {
        var apiKey = _configuration["SteamApiKey"];
        if (string.IsNullOrEmpty(apiKey))
            return BadRequest("API key not configured.");

        var steamApiUrl = $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={apiKey}&steamid={steamid}&include_appinfo=true&format=json";
        var client = _clientFactory.CreateClient();
        var response = await client.GetAsync(steamApiUrl);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[SteamController][ERROR] /ownedgames {response.StatusCode} for steamid={steamid}");
            return StatusCode((int)response.StatusCode, "Steam API error.");
        }

        var content = await response.Content.ReadAsStringAsync();
        return Content(content, "application/json");
    }

    [HttpGet("achievements")]
    public async Task<IActionResult> GetAchievements([FromQuery] long appid, [FromQuery] string steamid)
    {
        var apiKey = _configuration["SteamApiKey"];
        if (string.IsNullOrEmpty(apiKey))
            return BadRequest("API key not configured.");

        var steamApiUrl = $"https://api.steampowered.com/ISteamUserStats/GetPlayerAchievements/v1/?key={apiKey}&steamid={steamid}&appid={appid}";
        var client = _clientFactory.CreateClient();
        var response = await client.GetAsync(steamApiUrl);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, "Steam API error.");

        var content = await response.Content.ReadAsStringAsync();
        return Content(content, "application/json");
    }

    [HttpGet("schema")]
    public async Task<IActionResult> GetSchema([FromQuery] long appid)
    {
        var apiKey = _configuration["SteamApiKey"];
        if (string.IsNullOrEmpty(apiKey))
            return BadRequest("API key not configured.");

        var steamApiUrl = $"https://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/?key={apiKey}&appid={appid}";
        var client = _clientFactory.CreateClient();
        var response = await client.GetAsync(steamApiUrl);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, "Steam API error.");

        var content = await response.Content.ReadAsStringAsync();
        return Content(content, "application/json");
    }

    [HttpGet("globalpercentages")]
    public async Task<IActionResult> GetGlobalAchievementPercentages([FromQuery] long appid)
    {
        var steamApiUrl = $"https://api.steampowered.com/ISteamUserStats/GetGlobalAchievementPercentagesForApp/v2/?gameid={appid}";
        var client = _clientFactory.CreateClient();
        var response = await client.GetAsync(steamApiUrl);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, "Steam API error.");

        var content = await response.Content.ReadAsStringAsync();
        return Content(content, "application/json");
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchGames([FromQuery] string term)
    {
        var steamApiUrl = $"https://store.steampowered.com/api/storesearch/?term={term}&cc=us&l=en";
        var client = _clientFactory.CreateClient();
        var response = await client.GetAsync(steamApiUrl);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, "Steam API error.");

        var content = await response.Content.ReadAsStringAsync();
        return Content(content, "application/json");
    }
}