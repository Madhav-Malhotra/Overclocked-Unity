using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class CPUController : MonoBehaviour
{
    private CPU cpu;
    private CPUState stateB;

    void Start()
    {
        try
        {
            string levelPath = ResolveLevelPath();
            cpu = new CPU(levelPath);
            CPU.dump_imem(10);
            Debug.Log($"CPU initialized. IMEM file: {levelPath}");

            CPU.get_cpu_state(out stateB);
        }
        catch (Exception ex)
        {
            Debug.LogError($"CPU initialization failed: {ex.Message}");
        }
    }

    public CPUState GetStateB() => stateB;

    public void AdvanceTick()
    {
        if (cpu == null)
        {
            Debug.LogWarning("CPU not initialized.");
            return;
        }

        CPU.tick();
        CPU.get_cpu_state(out stateB);
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

    void OnDestroy()
    {
        if (cpu == null)
            return;

        GC.SuppressFinalize(cpu);
        cpu = null;
    }

    private static string ResolveLevelPath()
    {
        string assetsLevel = Path.Combine(Application.dataPath, "CPUWrapper", "level1.txt");
        if (File.Exists(assetsLevel))
            return assetsLevel;

        string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string verilatorLevel = Path.Combine(repoRoot, "verilator", "level1.txt");
        if (File.Exists(verilatorLevel))
            return verilatorLevel;

        throw new FileNotFoundException(
            $"Could not find level1.txt. Checked: {assetsLevel} and {verilatorLevel}"
        );
    }
}
