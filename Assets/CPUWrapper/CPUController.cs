using System;
using UnityEngine;

public class CPUController : MonoBehaviour
{
    private CPU cpu;
    private CPUState stateB;

    private bool subscribedToLevelManager;

    // Subscribing here (rather than reading LevelManager.GetCurrentLevelInstructions() directly)
    // removes the dependency on Unity's unspecified cross-component Start() order: LoadLevel()
    // fires OnLevelLoaded on every scene load (first load via LevelManager.Start(), reload via
    // LevelManager.OnSceneLoaded), so this subscription alone covers first-load, retry, and
    // next-level uniformly with no separate immediate-read path needed.
    //
    // Awake() order across components is unspecified too, so LevelManager.Instance may still be
    // null here if LevelManager hasn't run its own Awake() yet (both live in the same scene,
    // Playground.unity, with no configured script execution order). Start() is retried as a
    // fallback since Unity guarantees every Awake() completes before any Start() runs, so
    // LevelManager.Instance is guaranteed non-null by then if LevelManager exists in the scene.
    // Do NOT add an eager immediate-read/InitCPU call here on top of the subscription — on a
    // scene reload, LevelManager.Instance.currentLevelData still holds the PREVIOUS level's data
    // until LoadLevel() runs, so an eager read is always stale and causes InitCPU (and
    // init_design_wrapper() on the shared native `design` singleton) to run twice per reload.
    // Confirmed via frame-numbered debug logging: this caused a real Unity Editor crash by
    // level 3 (see imem-bug.md).
    void Awake() => TrySubscribeToLevelManager();

    void Start() => TrySubscribeToLevelManager();

    private void TrySubscribeToLevelManager()
    {
        if (subscribedToLevelManager || LevelManager.Instance == null) return;

        subscribedToLevelManager = true;
        LevelManager.Instance.OnLevelLoaded += InitCPU;
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
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelLoaded -= InitCPU;
        }

        if (cpu == null)
            return;

        GC.SuppressFinalize(cpu);
        cpu = null;
    }
}
