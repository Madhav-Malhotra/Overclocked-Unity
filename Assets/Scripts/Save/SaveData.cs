using System;
using System.Collections.Generic;

// On-disk shape of save.json (serialized with Newtonsoft). See Assets/Scripts/Save/README.md.
//
// Extensibility rules:
//   - New stat / flag / upgrade = just use a new key; no schema change.
//   - New structural field = add it with a default; Newtonsoft ignores unknown fields on load
//     and leaves missing ones at their initializer, so old and new saves stay compatible.
[Serializable]
public class SaveData
{
    public int schemaVersion = 1;

    // Random id so playtest saves emailed back can be told apart.
    public string profileId = Guid.NewGuid().ToString("N");
    public string buildVersion;
    public string createdUtc = DateTime.UtcNow.ToString("o");
    public string lastSavedUtc;

    // Cross-mode data (sessions, total play time, ...).
    public Dictionary<string, double> stats = new();
    public HashSet<string> flags = new();

    // One entry per game variant: "levels" (level-based game), "incremental", ...
    public Dictionary<string, ModeSave> modes = new();
}

[Serializable]
public class ModeSave
{
    public Dictionary<string, double> stats = new();
    public HashSet<string> flags = new();
    public Dictionary<string, LevelRecord> levels = new();   // keyed by level id (JSON file name)
    public Dictionary<string, int> upgrades = new();         // upgrade id -> purchased level
}

[Serializable]
public class LevelRecord
{
    public bool completed;
    public int attempts;      // every finished attempt, win or lose
    public int completions;
    public Dictionary<string, double> last = new();   // stats from the most recent attempt
    public Dictionary<string, double> best = new();   // best value per stat (see StatRule)
    public HashSet<string> flags = new();             // e.g. "tipsSeen"
}

// How a stat is merged into LevelRecord.best.
public enum StatRule
{
    Min,   // lower is better (e.g. timeTaken)
    Max,   // higher is better
    Sum,   // accumulate across attempts
}
