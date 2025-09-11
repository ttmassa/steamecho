# SteamEcho

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/timotheemassa)

SteamEcho is a Windows app that manages and displays achievements for all your Steam games, whether they were purchased on the platform or not. 

## Important notice

This project was developed for personal use. Its purpose is **in no way** to promote or facilitate video game piracy.

Most games, especially indie ones, are the result of hard work and passion from developers who deserve to be paid for their creativity. I really encourage you to buy the games you love and support their creators financially. It's thanks to your support that the video game industry can keep giving us memorable experiences.

## How it works

SteamEcho uses a DLL proxy approach to intercept calls to the Steam API.

1.  **Proxy DLL (`SmokeAPI`)**: A C++ DLL replaces the original one in the game folder. The project provides two versions of the DLL to suit the game's architecture: `steam_api.dll` for 32-bit games and `steam_api64.dll` for 64-bit games. When the game unlocks an achievement, our DLL intercepts the call.
2.  **IPC Communication**: The proxy DLL sends the ID of the unlocked achievement to the main application via a named communication channel (Named Pipe).
3.  **WPF application (`SteamEcho.App`)**: SteamEcho listens for messages from the DLL. When an achievement is received, it retrieves the details (title, description, icon) from the official Steam API and displays a notification on the screen. 
On top of that, the app provides many other features such as customizing your notifications, stats about your achievement progress and more!

## How to use the app?

1.  **Launch**: Find and launch `SteamEcho.App.exe`.
2.  **Add a game**:
    *   Click on the "+" icon.
    *   Select the game's executable. The app will automatically suggest a list of games based on the file name.
3.  **Proxy installation**:
    *   In your library, click on the game you just added.
    *   Click on the **"Setup"** button.
    *   If necessary, you will be asked to add the path to the executable file by right-clicking the game in the library, and then clicking on “Set executable.”
    *   The app will save the original DLL and copy the necessary proxy (32 or 64-bit).
4.  **Play**: Launch your game. When you unlock an achievement, a notification will pop up, happy hunting!

## Overview

Voici un exemple de l'interface et des notifications de succès :

<img src="SteamEcho.App/Assets/Gifs/achievement_notification.gif" width="852" alt="SteamEcho Overview" />

## Structure

*   **`SteamEcho.App`**: The main project. It is a WPF (.NET) application that contains the UI, and all services.
* **`SteamEcho.Backend`**: An ASP.NET Core Web API that acts as a proxy between the `SteamEcho.App` and the Steam API.
*   **`SteamEcho.Core`**: A .NET class library that defines data models (Game, Achievement) and service interfaces.

## Proxy DLL compilation (`SmokeAPI`)

The proxy DLLs (`steam_api.dll` and `steam_api64.dll`) used by SteamEcho are based on the open-source [SmokeAPI](https://github.com/acidicoala/SmokeAPI) project. They have been modified to add the necessary features for this application, such as IPC communication via Named Pipes to report unlocked achievements.

The DLLs are pre-compiled and included in the `SteamEcho.App/ThirdParty/SmokeAPI` directory. There is no need to compile them manually. The application will automatically use these files during the game setup process.

For more details on the modifications made to SmokeAPI, please see the `README.md` file in the `SteamEcho.App/ThirdParty` directory.

### Compiling the DLLs (Optional)

If you need to modify the proxy DLLs, the source code for the modified version of SmokeAPI is included in the solution. You can re-compile them using Visual Studio:

1.  Open the `steamecho.sln` solution in Visual Studio.
2.  The solution should contain the C++ project for the proxy DLLs.
3.  Build the project to generate the new `steam_api.dll` (32-bit) and `steam_api64.dll` (64-bit) files.
4.  Replace the existing DLLs in the `SteamEcho.App/ThirdParty/SmokeAPI` directory with the newly compiled ones.

## Technical Stack

*   **Proxy DLL**: C++ for intercepting low-level calls to the Steam API.
*   **App**: C# with WPF for the UI and .NET for the application logic.
*   **Database**: SQLite to store game, achievement, and user information locally.

### For Contributors (Development Setup)

To contribute to the project, you will need to run the backend on your local machine.

1.  **Get a Steam API Key**: You need your own Steam API key for development. You can get one from the [Steam Community Developer page](https://steamcommunity.com/dev/apikey).

2.  **Configure Your API Key**: The application will automatically use the key you provide. We recommend using .NET's Secret Manager to keep your key separate from the project code.
    *   **Using User Secrets (Recommended)**: In the `SteamEcho.Backend` directory, run the following command:
        1. ```bash
            dotnet user-secrets init
            ```

        2. ```bash
            dotnet user-secrets set "SteamApiKey" "YOUR_KEY_HERE"
            ```
    *   **Using `appsettings.Development.json`**: Alternatively, you can add the key directly to `SteamEcho.Backend/appsettings.Development.json`.

3.  **Run the Backend**: Launch the `SteamEcho.Backend` project from your IDE.

4.  **Connect the Frontend**: The WPF application (`SteamEcho.App`) is configured to connect to the local backend when debugging. If you change the backend port in `Properties/launchSettings.json`, you must update the URL in `SteamEcho.App/Services/SteamService.cs`.

### For Maintainers (Production Deployment)

Deployment to the production Azure environment is handled automatically by the `.github/workflows/azure-deploy.yml` workflow.

## Contribute

This project is open source and all contributions are welcome! To participate, please follow these steps:

1.  **Fork the project**: Create a copy of the repository on your own GitHub account.
2.  **Create a branch**: `git checkout -b feature/NewFeature`
3.  **Commit your changes**: `git commit -m 'Add my new awesome feature'`
4.  **Push on your branch**: `git push origin feature/NewFeature`
5.  **Open a Pull Request**: Submit a pull request from your branch to the main repository.

Each contribution will be reviewed before being integrated. Please document your code and follow the existing style.

The project is still under development. Feel free to suggest new features or report any issues you encounter by opening an “Issue” on GitHub. Your help is greatly appreciated!

## License

This project is distributed under the MIT license. See the `LICENSE` file for more details.