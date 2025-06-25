namespace SteamEcho.App.DTOs;

public class GameInfo(string steamId, string name, string iconUrl)
{
    public string SteamId { get; set; } = steamId;
    public string Name { get; set; } = name;
    public string IconUrl { get; set; } = iconUrl;
}