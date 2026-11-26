# Save system

Local-only JSON save at `Application.persistentDataPath/save.json` (Windows:
`%USERPROFILE%\AppData\LocalLow\<Company>\<Product>\save.json`; set Company/Product in Project
Settings > Player before shipping). Settings (volume, show-tips) stay in `PlayerPrefs`
(`Assets/UI/Help/GameSettings.cs`).

- `SaveData.cs`: the on-disk shape (Newtonsoft dictionaries).
- `SaveManager.cs`: static API. Loads lazily, saves on events + on quit, writes atomically
  (`save.json.tmp` then swap). An unreadable save is kept as `save.corrupt.json` and a fresh one starts.

## Shape

```
SaveData  { schemaVersion, profileId, buildVersion, createdUtc, lastSavedUtc,
            stats{key:number}, flags[], modes{ modeId: ModeSave } }
ModeSave  { stats, flags, levels{ levelId: LevelRecord }, upgrades{ id: level } }
LevelRecord { completed, attempts, completions, last{stat}, best{stat}, flags[] }
```

`modeId` is one per game variant: `"levels"` (level-based game), `"incremental"` (upgrade game).
Level ids are the level JSON file names (`level_01`).

## Common tasks

- **New per-level stat:** add a `(key, value, StatRule)` tuple in `LevelManager.RecordResult`.
  `StatRule.Min/Max/Sum` decides how `best` is merged (best-of stats only count winning attempts).
- **New flag:** `SaveManager.SetFlag("x")` / `SetLevelFlag(levelId, "x")`; read with `HasFlag` / `HasLevelFlag`.
- **Incremental mode:** `SaveManager.SetUpgradeLevel("multiplier", 3)`, `GetUpgradeLevel(...)` (0 if unset),
  `AddStat("currency", 10, SaveManager.IncrementalMode)`.
- **Reads never throw:** missing keys return 0 / null / false.
- Adding fields is safe without migration: unknown fields are ignored on load, missing ones take their
  initializer. `schemaVersion` is stored for a future migration if a field's meaning ever changes.

Playtesters can send you `save.json` (Settings > Open save folder).
