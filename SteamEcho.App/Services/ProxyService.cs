using System.IO;
using SteamEcho.App.Views;
using SteamEcho.Core.Models;
using SteamEcho.Core.Services;

namespace SteamEcho.App.Services;

public class ProxyService : IProxyService
{
    public bool CheckProxyStatus(Game game)
    {
        if (string.IsNullOrEmpty(game.ExecutablePath) || !File.Exists(game.ExecutablePath))
        {
            game.IsProxyReady = false;
            return false;
        }

        var gameDirectory = Path.GetDirectoryName(game.ExecutablePath);
        if (string.IsNullOrEmpty(gameDirectory) || !Directory.Exists(gameDirectory))
        {
            game.IsProxyReady = false;
            return false;
        }

        // Search all subdirectories for proxy DLL pairs
        bool foundProxy = false;
        foreach (var dir in Directory.GetDirectories(gameDirectory, "*", SearchOption.AllDirectories).Prepend(gameDirectory))
        {
            bool isProxyReady32 = File.Exists(Path.Combine(dir, "steam_api_o.dll")) && File.Exists(Path.Combine(dir, "steam_api.dll")) && File.Exists(Path.Combine(dir, "SmokeAPI.config.json"));
            bool isProxyReady64 = File.Exists(Path.Combine(dir, "steam_api64_o.dll")) && File.Exists(Path.Combine(dir, "steam_api64.dll")) && File.Exists(Path.Combine(dir, "SmokeAPI.config.json"));
            if (isProxyReady32 || isProxyReady64)
            {
                foundProxy = true;
                break;
            }
        }

        game.IsProxyReady = foundProxy;
        return true;
    }

    public void ToggleProxy(Game game)
    {
        if (game == null) return;

        if (string.IsNullOrEmpty(game.ExecutablePath) || !File.Exists(game.ExecutablePath))
        {
            var dialog = new MessageDialog(Resources.Resources.ErrorNoExecutableMessage, Resources.Resources.ErrorNoExecutableTitle);
            dialog.ShowDialog();
            return;
        }

        var gameDirectory = Path.GetDirectoryName(game.ExecutablePath);
        if (gameDirectory == null || !Directory.Exists(gameDirectory))
        {
            var dialog = new MessageDialog(Resources.Resources.ErrorInvalidExecutableMessage, Resources.Resources.ErrorInvalidExecutableTitle);
            dialog.ShowDialog();
            return;
        }

        if (game.IsProxyReady)
        {
            // Ask for confirmation before disabling the proxy
            var confirmDialog = new ConfirmDialog(
                Resources.Resources.ConfirmRemoveProxyMessage,
                Resources.Resources.ConfirmRemoveProxyTitle
            );
            var result = confirmDialog.ShowDialog();
            if (result != true) return;

            // Disable the proxy
            try
            {
                UnprocessSteamApiDll(gameDirectory, "x86");
                UnprocessSteamApiDll(gameDirectory, "x64");

                game.IsProxyReady = false;
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog(
                    string.Format(Resources.Resources.ErrorUninstallProxyMessage, ex.Message),
                    Resources.Resources.ErrorUninstallProxyTitle
                );
                dialog.ShowDialog();
            }
        }
        else
        {
            // Setup the proxy
            try
            {
                bool setupDone = false;
                setupDone |= ProcessSteamApiDll(gameDirectory, "x86");
                setupDone |= ProcessSteamApiDll(gameDirectory, "x64");

                if (setupDone)
                {
                    game.IsProxyReady = true;
                }
                else
                {
                    var dialog = new MessageDialog(Resources.Resources.ErrorProxySetupMessage, Resources.Resources.ErrorProxySetupTitle);
                    dialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog(
                    string.Format(Resources.Resources.ErrorProxySetupMessage, ex.Message),
                    Resources.Resources.ErrorProxySetupTitle
                );
                dialog.ShowDialog();
            }
        }
    }
    
    #region Helper Methods
    
    private static bool ProcessSteamApiDll(string gameDirectory, string bitness) {
        if (bitness != "x86" && bitness != "x64")
        {
            throw new ArgumentException("Bitness must be either 'x86' or 'x64'.");
        }

        string dllName = bitness == "x86" ? "steam_api.dll" : "steam_api64.dll";
        string renamedDllName = bitness == "x86" ? "steam_api_o.dll" : "steam_api64_o.dll";
        
        // Look for both original and already-renamed DLLs
        var originalDllPaths = Directory.GetFiles(gameDirectory, dllName, SearchOption.AllDirectories);
        var renamedDllPaths = Directory.GetFiles(gameDirectory, renamedDllName, SearchOption.AllDirectories);

        // Combine all unique directories where DLLs were found
        var directoriesToProcess = originalDllPaths.Concat(renamedDllPaths)
            .Select(Path.GetDirectoryName)
            .Where(d => d != null)
            .Distinct()
            .ToList();

        if (directoriesToProcess.Count == 0) return false;

        foreach (var dllDirectory in directoriesToProcess)
        {
            var originalDllPath = Path.Combine(dllDirectory!, dllName);
            var renamedDllPath = Path.Combine(dllDirectory!, renamedDllName);

            // Rename original dll
            if (File.Exists(originalDllPath) && !File.Exists(renamedDllPath))
            {
                File.Move(originalDllPath, renamedDllPath);
            }

            // Copy proxy dll
            string proxyDllSourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ThirdParty", "SmokeAPI", bitness, dllName);
            if (File.Exists(proxyDllSourcePath) && !File.Exists(originalDllPath))
            {
                File.Copy(proxyDllSourcePath, originalDllPath);
            }

            // Copy config file
            string configSourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ThirdParty", "SmokeAPI", "SmokeAPI.config.json");
            string configDestPath = Path.Combine(dllDirectory!, "SmokeAPI.config.json");
            if (File.Exists(configSourcePath) && !File.Exists(configDestPath))
            {
                File.Copy(configSourcePath, configDestPath);
            }
        }
        return true;
    }

    private static void UnprocessSteamApiDll(string gameDirectory, string bitness)
    {
        if (bitness != "x86" && bitness != "x64")
        {
            throw new ArgumentException("Bitness must be either 'x86' or 'x64'.");
        }

        string dllName = bitness == "x86" ? "steam_api.dll" : "steam_api64.dll";
        string renamedDllName = bitness == "x86" ? "steam_api_o.dll" : "steam_api64_o.dll";

        // Find all directories where the original DLL was renamed.
        var renamedDllPaths = Directory.GetFiles(gameDirectory, renamedDllName, SearchOption.AllDirectories);

        foreach (var renamedDllPath in renamedDllPaths)
        {
            var dllDirectory = Path.GetDirectoryName(renamedDllPath);
            if (dllDirectory == null) continue;

            var originalDllPath = Path.Combine(dllDirectory, dllName);
            var configPath = Path.Combine(dllDirectory, "SmokeAPI.config.json");

            // Delete the proxy DLL (which has the original name)
            if (File.Exists(originalDllPath))
            {
                File.Delete(originalDllPath);
            }

            // Delete the config file
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }

            // Rename the original DLL back
            if (File.Exists(renamedDllPath))
            {
                File.Move(renamedDllPath, originalDllPath);
            }
        }
    }

    #endregion
}