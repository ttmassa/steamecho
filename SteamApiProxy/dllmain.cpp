#include <windows.h>

#ifdef _WIN64
    // The .def file handles all exports now
    #define ORIGINAL_DLL "steam_api64_original.dll"
#else
    // The .def file handles all exports now
    #define ORIGINAL_DLL "steam_api_original.dll"
#endif

// Function pointer type for the original SetAchievement function
using SetAchievement_t = bool (*)(void* /* ISteamUserStats */, const char*);
SetAchievement_t pfnOriginalSetAchievement = nullptr;

// IPC to send message to the C# app
void SendPipeMessage(const char* message) {
    HANDLE hPipe;
    DWORD dwWritten;

    hPipe = CreateFile(TEXT("\\\\.\\pipe\\SteamEchoPipe"), 
                       GENERIC_WRITE, 
                       0, 
                       NULL, 
                       OPEN_EXISTING, 
                       0, 
                       NULL);

    if (hPipe != INVALID_HANDLE_VALUE) {
        WriteFile(hPipe, message, strlen(message) + 1, &dwWritten, NULL);
        CloseHandle(hPipe);
    }
}

// Hooked function for setAchievement - __declspec(dllexport) removed
extern "C" bool __stdcall SteamAPI_ISteamUserStats_SetAchievement(void* pSteamUserStats, const char* pchName) {
    // Send the achievement name to the C# app
    SendPipeMessage(pchName);

    // Call the original function
    if (pfnOriginalSetAchievement) {
        return pfnOriginalSetAchievement(pSteamUserStats, pchName);
    }

    return true;
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved) {
    if (ul_reason_for_call == DLL_PROCESS_ATTACH) {
        HMODULE hOriginalDll = LoadLibraryA(ORIGINAL_DLL);
        if (hOriginalDll) {
            // Get the address of the original function using its name
            pfnOriginalSetAchievement = (SetAchievement_t)GetProcAddress(hOriginalDll, "SteamAPI_ISteamUserStats_SetAchievement");
        }
        return TRUE;
    }
}
