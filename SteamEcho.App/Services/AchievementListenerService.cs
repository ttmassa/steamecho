using System.IO;
using System.IO.Pipes;
using SteamEcho.Core.Services;

namespace SteamEcho.App.Services;

public class AchievementListenerService : IAchievementListenerService
{
    public event Action<string>? AchievementUnlocked;
    
    public void StartListening()
    {
        Task.Run(() => ListenerLoop());
    }

    private void ListenerLoop()
    {
        while (true)
        {
            try
            {
                // Create a server pipe
                using var pipeServer = new NamedPipeServerStream("SteamEchoPipe", PipeDirection.In);

                // Wait for a game to connect
                pipeServer.WaitForConnection();

                // Read data from the pipe
                using var reader = new StreamReader(pipeServer);
                string? achievementApiName = reader.ReadLine();
                Console.WriteLine($"Received raw message from pipe: '{achievementApiName}'");

                if (!string.IsNullOrEmpty(achievementApiName))
                {
                    // Raise the event with the achievement API name
                    AchievementUnlocked?.Invoke(achievementApiName);
                }
            }
            catch (Exception ex)
            {
                // Optional: Log any exceptions that occur.
                Console.WriteLine($"Pipe listener error: {ex.Message}");
            }
        }
    }
}