using System;

[Serializable]
public class InstructionData
{
    public string label;
    public string hex;
}

[Serializable]
public class LevelData
{
    public string levelName;
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
