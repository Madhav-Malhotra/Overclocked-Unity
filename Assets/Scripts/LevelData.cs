using System;

[Serializable]
public class InstructionData
{
    public string id;
}

[Serializable]
public class LevelData
{
    public string levelName;
    public float timeLimit;
    public InstructionData[] instructions;
}
