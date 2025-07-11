namespace SteamEcho.Core.DTOs;

public class GameInfo(long steamId, string name, string? iconUrl)
{
    public long SteamId { get; set; } = steamId;
    public string Name { get; set; } = name;
    public string? IconUrl { get; set; } = iconUrl;
}
