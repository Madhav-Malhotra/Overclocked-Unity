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
}
