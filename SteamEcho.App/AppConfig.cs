using Microsoft.Extensions.Configuration;

namespace SteamEcho.App
{
    public class AppConfig
    {
        public SteamAPIConfig SteamAPI { get; set; } = new();

        public static AppConfig Load()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddUserSecrets<AppConfig>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            var appConfig = new AppConfig();
            config.Bind(appConfig);
            return appConfig;
        }
    }

    public class SteamAPIConfig
    {
        public string Client { get; set; } = "";
        public string Key { get; set; } = "";
    }
}