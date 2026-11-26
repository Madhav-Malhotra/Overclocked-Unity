using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

// Local JSON save (Application.persistentDataPath/save.json). Static so any scene / game mode can
// use it without a scene object. Loads lazily on first access, saves on events (never per frame)
// and when the application quits.
//
//   SaveManager.RecordLevelResult("levels", "level_01", success, ("timeTaken", 41.2, StatRule.Min));
//   SaveManager.IsLevelCompleted("level_01");
//   SaveManager.SetUpgradeLevel("incremental", "multiplier", 3);
//
// Reads never throw: missing keys give 0 / null / false.
public static class SaveManager
{
    public const string LevelsMode = "levels";
    public const string IncrementalMode = "incremental";
    public const string TipsSeenFlag = "tipsSeen";

    const string FileName = "save.json";

    static SaveData data;
    static bool dirty;

    public static string SaveFolder => Application.persistentDataPath;
    static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    public static SaveData Data
    {
        get
        {
            if (data == null) Load();
            return data;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        // Domain reload may be disabled in the editor, so statics can survive between Play sessions.
        data = null;
        dirty = false;
        Application.quitting -= OnQuitting;
        Application.quitting += OnQuitting;
        Data.stats["sessions"] = GetStat("sessions") + 1;
        MarkDirty();
        Save();
    }

    static void OnQuitting() => Save();

    // ---------------------------------------------------------------- load / save

    static void Load()
    {
        string path = SavePath;
        try
        {
            if (File.Exists(path))
            {
                data = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(path));
                if (data == null) throw new JsonException("save file was empty");
                return;
            }
        }
        catch (Exception e)
        {
            // Keep the unreadable file for debugging, then start fresh.
            Debug.LogWarning($"SaveManager: could not read {path} ({e.Message}); starting a new save.");
            try { File.Copy(path, Path.Combine(Application.persistentDataPath, "save.corrupt.json"), true); }
            catch (Exception copyError) { Debug.LogWarning($"SaveManager: could not back up bad save ({copyError.Message})."); }
        }
        data = new SaveData();
        dirty = true;
    }

    static void MarkDirty() => dirty = true;

    // Writes to a temp file and swaps it in, so a crash mid-write can't corrupt the existing save.
    public static void Save()
    {
        if (data == null || !dirty) return;
        try
        {
            data.buildVersion = Application.version;
            data.lastSavedUtc = DateTime.UtcNow.ToString("o");

            string path = SavePath;
            string tmp = path + ".tmp";
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(tmp, JsonConvert.SerializeObject(data, Formatting.Indented));
            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
            dirty = false;
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveManager: failed to save ({e.Message}).");
        }
    }

    // Wipes all progress (tips will show again, levels re-lock). Does not touch PlayerPrefs settings.
    public static void ResetAll()
    {
        string profileId = data?.profileId;
        data = new SaveData();
        if (profileId != null) data.profileId = profileId; // same tester, fresh progress
        MarkDirty();
        Save();
    }

    // ---------------------------------------------------------------- modes / levels

    public static ModeSave Mode(string modeId)
    {
        if (!Data.modes.TryGetValue(modeId, out ModeSave mode))
        {
            mode = new ModeSave();
            Data.modes[modeId] = mode;
        }
        return mode;
    }

    static LevelRecord GetOrCreateLevel(string modeId, string levelId)
    {
        ModeSave mode = Mode(modeId);
        if (!mode.levels.TryGetValue(levelId, out LevelRecord record))
        {
            record = new LevelRecord();
            mode.levels[levelId] = record;
        }
        return record;
    }

    // Read-only lookup: null when the level has never been played or tagged.
    public static LevelRecord GetLevel(string levelId, string modeId = LevelsMode)
    {
        if (levelId == null) return null;
        return Data.modes.TryGetValue(modeId, out ModeSave mode) && mode.levels.TryGetValue(levelId, out LevelRecord r) ? r : null;
    }

    public static bool IsLevelCompleted(string levelId, string modeId = LevelsMode) => GetLevel(levelId, modeId)?.completed ?? false;
    public static int GetAttempts(string levelId, string modeId = LevelsMode) => GetLevel(levelId, modeId)?.attempts ?? 0;

    public static double? GetBest(string levelId, string stat, string modeId = LevelsMode)
    {
        LevelRecord r = GetLevel(levelId, modeId);
        return r != null && r.best.TryGetValue(stat, out double v) ? v : null;
    }

    // Records one finished attempt. `last` is overwritten; `best` is merged per each stat's rule.
    public static void RecordLevelResult(string modeId, string levelId, bool success, params (string key, double value, StatRule rule)[] stats)
    {
        if (string.IsNullOrEmpty(levelId)) return;
        LevelRecord record = GetOrCreateLevel(modeId, levelId);

        record.attempts++;
        record.last.Clear();
        foreach (var (key, value, rule) in stats)
        {
            record.last[key] = value;
            // Best-of stats only count winning attempts (a failed run's time isn't a "best time").
            if (!success && rule != StatRule.Sum) continue;
            if (!record.best.TryGetValue(key, out double current)) record.best[key] = value;
            else record.best[key] = rule switch
            {
                StatRule.Min => Math.Min(current, value),
                StatRule.Max => Math.Max(current, value),
                _ => current + value,
            };
        }
        if (success)
        {
            record.completed = true;
            record.completions++;
        }
        MarkDirty();
        Save();
    }

    // ---------------------------------------------------------------- flags

    public static bool HasLevelFlag(string levelId, string flag, string modeId = LevelsMode) => GetLevel(levelId, modeId)?.flags.Contains(flag) ?? false;

    public static void SetLevelFlag(string levelId, string flag, string modeId = LevelsMode)
    {
        if (string.IsNullOrEmpty(levelId)) return;
        if (GetOrCreateLevel(modeId, levelId).flags.Add(flag)) { MarkDirty(); Save(); }
    }

    public static bool HasFlag(string flag) => Data.flags.Contains(flag);

    public static void SetFlag(string flag)
    {
        if (Data.flags.Add(flag)) { MarkDirty(); Save(); }
    }

    // ---------------------------------------------------------------- stats / upgrades (global + per mode)

    public static double GetStat(string key, string modeId = null)
    {
        Dictionary<string, double> stats;
        if (modeId == null) stats = Data.stats;
        else if (!Data.modes.TryGetValue(modeId, out ModeSave mode)) return 0;
        else stats = mode.stats;
        return stats.TryGetValue(key, out double v) ? v : 0;
    }

    public static void SetStat(string key, double value, string modeId = null)
    {
        (modeId == null ? Data.stats : Mode(modeId).stats)[key] = value;
        MarkDirty();
        Save();
    }

    public static void AddStat(string key, double delta, string modeId = null) => SetStat(key, GetStat(key, modeId) + delta, modeId);

    public static int GetUpgradeLevel(string upgradeId, string modeId = IncrementalMode)
        => Data.modes.TryGetValue(modeId, out ModeSave mode) && mode.upgrades.TryGetValue(upgradeId, out int level) ? level : 0;

    public static void SetUpgradeLevel(string upgradeId, int level, string modeId = IncrementalMode)
    {
        Mode(modeId).upgrades[upgradeId] = level;
        MarkDirty();
        Save();
    }
}
