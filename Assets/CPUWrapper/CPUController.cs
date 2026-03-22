using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class CPUController : MonoBehaviour
{
    private CPU cpu;

    void Start()
    {
        try
        {
            string levelPath = ResolveLevelPath();
            cpu = new CPU(levelPath);
            CPU.dump_imem(10);
            Debug.Log($"CPU initialized. IMEM file: {levelPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"CPU initialization failed: {ex.Message}");
        }
    }

    public void PrintCPUState()
    {
        if (cpu == null)
        {
            Debug.LogWarning("CPU not initialized.");
            return;
        }

        cpu.PrintState();
    }

    public uint GetALUOutput()
    {
        if (cpu == null)
        {
            Debug.LogWarning("CPU not initialized.");
            return 0;
        }

        return cpu.GetALUOut();
    }

    public void TickCPU()
    {
        if (cpu == null)
        {
            Debug.LogWarning("CPU not initialized.");
            return;
        }

        CPU.tick();
        cpu.PrintState();
    }

    void OnDestroy()
    {
        if (cpu == null)
        {
            return;
        }

        cpu.Dispose();
        cpu = null;
    }

    // temp method
    private static string ResolveLevelPath()
    {
        string assetsLevel = Path.Combine(Application.dataPath, "CPUWrapper", "level1.txt");
        if (File.Exists(assetsLevel))
        {
            return assetsLevel;
        }

        string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string verilatorLevel = Path.Combine(repoRoot, "verilator", "level1.txt");
        if (File.Exists(verilatorLevel))
        {
            return verilatorLevel;
        }

        throw new FileNotFoundException(
            $"Could not find level1.txt. Checked: {assetsLevel} and {verilatorLevel}"
        );
    }
}
