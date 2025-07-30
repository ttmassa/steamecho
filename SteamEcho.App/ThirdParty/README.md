# 📦 ThirdParty

This directory contains third-party dependencies used in SteamEcho.

## 🐨 SmokeAPI

**Source:** [SmokeAPI GitHub Repository](https://github.com/acidicoala/SmokeAPI)

**Usage:**  
SmokeAPI is a free and open-source Steam API replacement that allows applications to interact with Steamworks features without requiring the official Steam client. In SteamEcho, SmokeAPI is used as the core for building the proxy DLLs that enable achievement tracking and unlocking.

**Extensions:**  
To fit the needs of **SteamEcho**, the following extensions were made:
- **IUserStats Interface Implementation:**  
  Extended SmokeAPI by implementing the `IUserStats` interface, enabling real-time achievement tracking.
- **Named Pipe IPC:**  
  Added a named pipe for IPC, allowing SteamEcho to receive achievement updates instantly from the proxy DLLs.

**License:**  
SmokeAPI is completely free to use, and so are the proxy DLLs built for SteamEcho. See the [LICENSE.txt](./SmokeAPI/UNLICENSE.txt) for more details and credits.

**Special Thanks:**  
Huge thanks to the SmokeAPI team for making this project possible! Their work laid the foundation for SteamEcho’s achievement tracking system. If you use or extend these proxy DLLs, please consider checking out and supporting the original [SmokeAPI project](https://github.com/acidicoala/SmokeAPI).

## ➕ Adding/Updating Third-Party Libraries

If you add or update any third-party dependencies, please:
1. Place them in a clearly named subdirectory.
2. Include their original license and a README describing their usage and any modifications.
3. Update this file with relevant details.