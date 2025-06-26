namespace SteamEcho.Core.Models;

public class Achievement(string id, string name, string description, string? icon = null, string? grayIcon = null, double? globalPercentage = null)
{
    public string Id { get; set; } = id;
    public string Name { get; set; } = name;
    public string Description { get; set; } = description;
    public string? Icon { get; set; } = icon;
    public string? GrayIcon { get; set; } = grayIcon;
    public double? GlobalPercentage { get; set; } = globalPercentage;
    public bool IsUnlocked { get; set; } = false;
    public DateTime? UnlockDate { get; set; } = null;

    public void Unlock()
    {
        IsUnlocked = true;
        UnlockDate = DateTime.Now;
    }
}