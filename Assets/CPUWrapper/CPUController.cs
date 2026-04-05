using System;
using System.IO;
using UnityEngine;

public class CPUController : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool verboseLogging = true;

    private CPU cpu;
    private int activeProcessingStations;
    private bool cpuPaused;

    void Start()
    {
        try
        {
            string levelPath = ResolveLevelPath();
            cpu = new CPU(levelPath);
            Debug.Log($"CPU initialized. IMEM file: {levelPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"CPU initialization failed: {ex.Message}");
        }
    }

    public bool IsCpuPaused => cpuPaused;

    public void SetCpuPaused(bool paused)
    {
        if (cpuPaused == paused)
        {
            return;
        }

        cpuPaused = paused;

        if (verboseLogging)
        {
            Debug.Log($"[CPUController] CPU simulation paused = {cpuPaused}");
        }
    }

    public void SetStationEnabled(PipelineStage stage, bool enabled)
    {
        if (cpu == null)
        {
            Debug.LogWarning("CPU not initialized.");
            return;
        }

        if (verboseLogging)
        {
            Debug.Log($"[CPUController] Stage enable: {stage} = {enabled}");
        }

        switch (stage)
        {
            case PipelineStage.Fetch:
                cpu.SetFetchEn(enabled);
                break;
            case PipelineStage.Decode:
                cpu.SetFdEn(enabled);
                break;
            case PipelineStage.Execute:
                cpu.SetDxEn(enabled);
                break;
            case PipelineStage.Memory:
                cpu.SetXmEn(enabled);
                break;
            case PipelineStage.Writeback:
                cpu.SetMwEn(enabled);
                break;
            default:
                break;
        }
    }

    public void BeginStationProcessing(PipelineStage stage)
    {
        activeProcessingStations = Mathf.Max(0, activeProcessingStations + 1);

        if (verboseLogging)
        {
            Debug.Log($"[CPUController] Begin processing: {stage} (active stations = {activeProcessingStations})");
        }

        SetCpuPaused(true);
        SetStationEnabled(stage, true);
    }

    public void EndStationProcessing(PipelineStage stage)
    {
        SetStationEnabled(stage, false);
        activeProcessingStations = Mathf.Max(0, activeProcessingStations - 1);

        if (verboseLogging)
        {
            Debug.Log($"[CPUController] End processing: {stage} (active stations = {activeProcessingStations})");
        }

        if (activeProcessingStations == 0)
        {
            SetCpuPaused(false);
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

        if (cpuPaused)
        {
            if (verboseLogging)
            {
                Debug.Log("[CPUController] TickCPU skipped (CPU paused).");
            }

            return;
        }

        CPU.tick();
        cpu.PrintState();
    }

    void OnDestroy()
    {
        if (cpu == null)
            return;

        // Suppress the finalizer so the GC doesn't call cleanup_design_wrapper
        // during domain reload (it hangs and deadlocks Unity).
        GC.SuppressFinalize(cpu);
        cpu = null;

        // cleanup_design_wrapper() blocks indefinitely in the editor, so we
        // skip it here. The native DLL stays loaded between play sessions and
        // init_design_wrapper() will reinitialize state on next enter play mode.
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
