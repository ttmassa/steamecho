namespace SteamEcho.Core.DTOs;

public class PlayerAchievementStatus(bool isUnlocked, DateTime? unlockDate = null)
{
    public bool IsUnlocked { get; set; } = isUnlocked;
    public DateTime? UnlockDate { get; set; } = unlockDate;
}