using System;

[Serializable]
public class InstructionData
{
    public string label;
    public string hex;
}

// One step of a level's guided tooltip tour. All fields optional except `text`.
// See Assets/Levels/README.md ("Level tips") for the field reference.
[Serializable]
public class LevelTip
{
    public string title;
    public string text;
    // The `id` of a TipTarget component in the scene. A pulsing ring is drawn around that object.
    // Empty = no ring.
    public string target;
    // above | below | left | right = put the card next to `target`.
    // Empty (or no target) = the card sits in a fixed spot at the top right of the screen.
    public string placement;
}

[Serializable]
public class LevelData
{
    public string levelName;
    // Optional 1-2 sentence description shown under the title in the level selector.
    public string blurb;
    // Optional. Levels with no tips (null/empty) show no tooltip tour.
    public LevelTip[] tips;
    public string sceneName;
    public float timeLimit;
    public InstructionData[] instructions;
    // Matches CPUFactory.CPUArchitecture enum names ("Basic", "Superscalar"). Empty/unrecognized
    // values default to Basic — see LevelData.GetCpuArchitecture().
    public string cpuArchitecture;

    public CPUFactory.CPUArchitecture GetCpuArchitecture()
    {
        if (!string.IsNullOrEmpty(cpuArchitecture) &&
            Enum.TryParse(cpuArchitecture, out CPUFactory.CPUArchitecture parsed))
        {
            return parsed;
        }
        return CPUFactory.CPUArchitecture.Basic;
    }
}
