using System;
using UnityEngine;

public class CPUController : MonoBehaviour
{
    private CPU cpu;
    private CPUState stateB;

    void Start()
    {
        InstructionData[] instructions = LevelManager.Instance != null
            ? LevelManager.Instance.GetCurrentLevelInstructions()
            : null;

        InitCPU(instructions);
    }

    // Re-initializes the CPU with a new level's instructions (e.g. on retry or level switch).
    // LevelManager calls this so IMEM is reloaded with the correct level rather than staying stale.
    //
    // Deliberately does NOT call cpu.Dispose() on the old instance before reassigning —
    // see bugs.todo: cpu.Dispose() -> cleanup_design_wrapper() -> design->final() hangs the
    // Editor (freezes with zero console output). OnDestroy() below has the same workaround
    // (GC.SuppressFinalize without Dispose). init_design_wrapper() is safe to call again on
    // an already-live design (it no-ops the allocation and just re-runs reset), so we just
    // drop the old CPU reference and let a new one take over; the old native model leaks
    // for the lifetime of the process instead of being freed.
    public void InitCPU(InstructionData[] instructions)
    {
        cpu = null;

        try
        {
            cpu = new CPU(instructions);
            CPU.dump_imem(10);
            Debug.Log($"CPU initialized with {instructions?.Length ?? 0} instructions.");

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
}
