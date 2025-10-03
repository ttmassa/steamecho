namespace SteamEcho.App.Models;

public class LanguageOption
{
    public required string DisplayName { get; set; }
    public required string CultureName { get; set; }
    public required string SteamCode { get; set; }
    public required string FlagPath { get; set; }
}