namespace SteamEcho.Core.DTOs;

public class SteamUserInfo(string steamId)
{
    public string SteamId { get; set; } = steamId;
    public string? ApiKey { get; set; }
}