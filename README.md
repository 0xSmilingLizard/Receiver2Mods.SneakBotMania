# SneakBotMania
 
A mod for Receiver 2, that adds various stalker entitiy related settings:
- Spawn a range of stalkers at once instead of a single one
- A timer to periodically and semi-randomly spawn stalkers
- Remove the limit of 1 per level from normal spawning methods

## Install

Install [BepInEx](https://github.com/BepInEx/BepInEx) [(the x64 version)](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.5) into the Receiver 2 folder (the one containing `Receiver2.exe`), then start and exit Receiver 2 to have BepInEx generate its folder structure.
Then place the SneakBotMania folder in `BepInEx/plugins`.

It is recommended to use [BepInEx's Config Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager) (installed the same way as this mod) to have an in-game UI to configure the settings.

## Dependencies

The source code depends on `0Harmony.dll`, `BepInEx.dll`, `UnityEngine.dll`, `UnityEngine.CoreModule.dll`, and `Wolfire.Receiver2.dll`. It is set up to expect these DLLs to be located in the parent folder of the repository root folder. All of these DLLs can be found as part of either Receiver 2's or BepInEx's install.
