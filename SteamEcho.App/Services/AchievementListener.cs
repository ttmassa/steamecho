using Steamworks;

namespace SteamEcho.App.Services;

public class AchievementListener
{
    private Callback<UserAchievementStored_t>? _achievementStoredCallback;
    public event Action<string>? AchievementUnlocked;

    public AchievementListener()
    {
        _achievementStoredCallback = Callback<UserAchievementStored_t>.Create(OnAchievementStored);
    }

    private void OnAchievementStored(UserAchievementStored_t pCallback)
    {
        // Only trigger for standard achievements, not stats and progress updates
        if (pCallback.m_nMaxProgress == 0)
        {
            AchievementUnlocked?.Invoke(pCallback.m_rgchAchievementName);
        }
    }

    public void Update()
    {
        SteamAPI.RunCallbacks();
    }
}