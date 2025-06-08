namespace SteamEcho.Core.Models;

public class Achievement(string id, string name, string description)
{
    public string Id { get; set; } = id;
    public string Name { get; set; } = name;
    public string Description { get; set; } = description;
    public bool IsUnlocked { get; set; } = false;
    public DateTime? UnlockDate { get; set; } = null;

    public void Unlock()
    {
        IsUnlocked = true;
        UnlockDate = DateTime.Now;
    }
}