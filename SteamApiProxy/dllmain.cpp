#include <windows.h>
#include <fstream>
#include <string>

#ifdef _WIN64
    #define ORIGINAL_DLL "steam_api64_original.dll"
    #define LOG_FILE "SteamApiProxy64.log"
#else
    #define ORIGINAL_DLL "steam_api_original.dll"
    #define LOG_FILE "SteamApiProxy32.log"
#endif

// Function to write a message to our log file
void LogToFile(const std::string& message) {
    std::ofstream log_file(LOG_FILE, std::ios_base::out | std::ios_base::app);
    log_file << message << std::endl;
}

// Function pointer type for the original SetAchievement function
using SetAchievement_t = bool (*)(void* /* ISteamUserStats */, const char*);
SetAchievement_t pfnOriginalSetAchievement = nullptr;

// IPC to send message to the C# app
void SendPipeMessage(const char* message) {
    LogToFile("SendPipeMessage called with: " + std::string(message));
    HANDLE hPipe = CreateFileA(
        "\\\\.\\pipe\\SteamEchoPipe",
        GENERIC_WRITE,
        0,
        NULL,
        OPEN_EXISTING,
        0,
        NULL);

    if (hPipe != INVALID_HANDLE_VALUE) {
        LogToFile("Pipe connection successful.");
        DWORD cbWritten;
        WriteFile(hPipe, message, (DWORD)strlen(message), &cbWritten, NULL);
        LogToFile("Wrote " + std::to_string(cbWritten) + " bytes to pipe.");
        CloseHandle(hPipe);
    } else {
        LogToFile("Pipe connection failed. GetLastError=" + std::to_string(GetLastError()));
    }
}

// Hooked function for setAchievement - __declspec(dllexport) removed
extern "C" bool __stdcall SteamAPI_ISteamUserStats_SetAchievement(void* pSteamUserStats, const char* pchName) {
    // Send the achievement name to the C# app
    LogToFile("Hooked SteamAPI_ISteamUserStats_SetAchievement triggered.");
    LogToFile("Achievement API Name: " + std::string(pchName));
    SendPipeMessage(pchName);

    // Call the original function
    if (pfnOriginalSetAchievement) {
        return pfnOriginalSetAchievement(pSteamUserStats, pchName);
    }

    return true;
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved) {
    if (ul_reason_for_call == DLL_PROCESS_ATTACH) {
        LogToFile("--- DLL Attached ---");
        HMODULE hOriginalDll = LoadLibraryA(ORIGINAL_DLL);
        if (hOriginalDll) {
            LogToFile("Original DLL loaded successfully.");
            // Get the address of the original function using its name
            pfnOriginalSetAchievement = (SetAchievement_t)GetProcAddress(hOriginalDll, "SteamAPI_ISteamUserStats_SetAchievement");
            if (pfnOriginalSetAchievement) {
                LogToFile("GetProcAddress for original function successful.");
            } else {
                LogToFile("GetProcAddress for original function FAILED.");
            }
        } else {
            LogToFile("Failed to load original DLL.");
        }
    }
    return TRUE;
}
