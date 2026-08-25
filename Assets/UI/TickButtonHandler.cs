using UnityEngine;
using UnityEngine.InputSystem;

public class TickButtonHandler : MonoBehaviour
{
    public CPUController cpuController;
    public TickFeedbackUI feedbackUI;

    private CPUStation[] stations;
    private StartPlatform startPlatform;
    private bool isBlackboxScene;

    void Start()
    {
        stations = FindObjectsByType<CPUStation>(FindObjectsSortMode.None);
        startPlatform = FindFirstObjectByType<StartPlatform>();
        if (startPlatform == null)
            Debug.LogWarning("TickButtonHandler: no StartPlatform found in scene.");

        // Blackbox scenes drive ticking entirely from BlackboxStation's own coroutine
        // (RunUntilRetired), never from player T presses. Gate on scene identity rather than
        // "stations happens to be empty" so this stays correct even if station wiring changes.
        isBlackboxScene = LevelManager.Instance != null
            && LevelManager.Instance.CurrentLevelData != null
            && LevelManager.Instance.CurrentLevelData.sceneName == "Blackbox";
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
            OnTickPressed();
    }

    public void OnTickPressed()
    {
        if (cpuController == null)
        {
            Debug.LogWarning("TickButtonHandler: cpuController not assigned.");
            return;
        }

        if (isBlackboxScene) return;

        CPUState[] statesB = new CPUState[cpuController.WayCount];
        for (int way = 0; way < statesB.Length; way++)
        {
            statesB[way] = cpuController.GetStateB(way);
            Debug.Log($"[DEBUG OnTickPressed] WayCount={cpuController.WayCount} way={way} pc=0x{statesB[way].pc:X8}");
        }

        foreach (var station in stations)
        {
            if (station == null || !station.HasBrick) continue;
            Debug.Log($"[DEBUG station] name={station.name} AssignedStage={station.AssignedStage} AssignedWay={station.AssignedWay} brick.InstructionPc=0x{station.CurrentBrick.InstructionPc:X8} brick.Way={station.CurrentBrick.Way}");
        }

        uint nextSpawnPc = startPlatform != null ? startPlatform.NextSpawnPc : uint.MaxValue;
        ValidateResult result = PipelineValidator.Validate(statesB, stations, nextSpawnPc);

        if (result.isValid)
        {
            feedbackUI?.Hide();
            CaptureMonitorData(statesB);
            MarkRetiredWritebackBrick(statesB);
            cpuController.AdvanceTick();
        }
        else
        {
            feedbackUI?.ShowErrors(result.errors);
            foreach (var error in result.errors)
                Debug.LogWarning($"[Tick validation] {error.stage}: {error.message}");
        }
    }

    // Snapshots per-instruction register/address values off the pre-advance CPU state
    // (statesB as of the START of this tick), before this tick's AdvanceTick() shifts
    // the pipeline and addr_rd_mw/wb_data start describing a different instruction.
    // Indexed by each station's own way — a no-op distinction on single-way scenes, where
    // every station's AssignedWay is 0.
    //   - Memory station -> alu_out (dmem address of the instruction currently in Memory)
    //   - Writeback station -> addr_rd_mw + wb_data (rd/value of the instruction currently
    //     in Writeback, captured once the brick has reached this station). Skipped for
    //     stores, which have no destination register.
    private void CaptureMonitorData(CPUState[] statesB)
    {
        foreach (var station in stations)
        {
            if (station == null || !station.HasBrick) continue;
            InstructionMonitorCapture.CaptureAtStage(station.CurrentBrick, statesB[station.AssignedWay], station.AssignedStage);
        }
    }

    // A brick only counts as fully retired once a validated tick confirms it's actually sitting
    // at Writeback with the hardware's mw_pc for this cycle — not just because it's physically
    // placed on the Writeback station (placement alone used to mark it processed via
    // CPUStation's processing timer, which let players skip pressing T entirely).
    // statesB here is the pre-tick snapshot PipelineValidator just validated against, so mw_pc
    // still describes the instruction retiring this cycle for each way.
    private void MarkRetiredWritebackBrick(CPUState[] statesB)
    {
        foreach (var station in stations)
        {
            if (station == null || !station.HasBrick) continue;
            if (station.AssignedStage != PipelineStage.Writeback) continue;
            if (statesB[station.AssignedWay].mw_pc == station.CurrentBrick.InstructionPc)
                station.CurrentBrick.MarkProcessed();
        }
    }
}
