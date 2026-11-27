# Changelog

Append-only, newest at the BOTTOM. Read only the tail (`tail -n 60 changelog.md`), never the whole file; add entries with `cat >> changelog.md` (never overwrite). See `/sk-implement` Phase 4 for the entry format.

2026-11-26T21:42:21Z
https://github.com/Madhav-Malhotra/Overclocked-Unity/pull/13
Local save system, level selector, shared UI utilities
- Persist progress per game mode (levels now, incremental later) as JSON: `SaveManager.cs`, `SaveData.cs`
- Level picker with locking, blurbs and friendly names; tips show once; best time on end screen: `HelpOverlay.cs`, `LevelManager.cs`
- Settings: reset progress, open save folder, visible slider; fixed level countdown running in the main menu: `HelpOverlay.cs`
- Reusable UI: one alert popup `Toast.cs` and utility classes (`.on-hover`, `.bordered`, ...) in `UiUtilities.uss`

2026-11-27T03:20:39Z
https://github.com/Madhav-Malhotra/Overclocked-Unity/pull/14
Audio system: SFX, UI sounds, music, volume sliders
- Named sounds as data: `AudioManager.cs`, `SoundEvent.cs` + assets in `Assets/Resources/Audio/SoundEvents/` (add one via `SoundEventGenerator.cs`, see `Assets/Scripts/Audio/README.md`)
- Click/hover sounds on every UI Toolkit button, hover debounced: `UiSounds.cs`; hooks in `Toast.cs`, `InstructionBrickHoldingSystem.cs`, `TickButtonHandler.cs`, `EndScreenUI.cs`
- Looping background music across scenes (`music_main`) and Master/Music/SFX sliders: `GameSettings.cs`, `HelpOverlay.cs`
