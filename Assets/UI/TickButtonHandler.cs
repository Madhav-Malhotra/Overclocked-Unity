using UnityEngine;
using UnityEngine.InputSystem;

public class TickButtonHandler : MonoBehaviour
{
    public CPUController cpuController;
    public TickFeedbackUI feedbackUI;

    private CPUStation[] stations;
    private StartPlatform startPlatform;

    void Start()
    {
        stations = FindObjectsByType<CPUStation>(FindObjectsSortMode.None);
        startPlatform = FindFirstObjectByType<StartPlatform>();
        if (startPlatform == null)
            Debug.LogWarning("TickButtonHandler: no StartPlatform found in scene.");
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

        CPUState stateB = cpuController.GetStateB();

        uint nextSpawnPc = startPlatform != null ? startPlatform.NextSpawnPc : uint.MaxValue;
        ValidateResult result = PipelineValidator.Validate(stateB, stations, nextSpawnPc);

        if (result.isValid)
        {
            feedbackUI?.Hide();
            CaptureMonitorData(stateB);
            MarkRetiredWritebackBrick(stateB);
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
    // (stateB as of the START of this tick), before this tick's AdvanceTick() shifts
    // the pipeline and addr_rd_mw/wb_data start describing a different instruction.
    //   - Memory station -> alu_out (dmem address of the instruction currently in Memory)
    //   - Writeback station -> addr_rd_mw + wb_data (rd/value of the instruction currently
    //     in Writeback, captured once the brick has reached this station). Skipped for
    //     stores, which have no destination register.
    private void CaptureMonitorData(CPUState stateB)
    {
        foreach (var station in stations)
        {
            if (station == null || !station.HasBrick) continue;
            InstructionMonitorCapture.CaptureAtStage(station.CurrentBrick, stateB, station.AssignedStage);
        }
    }

    // A brick only counts as fully retired once a validated tick confirms it's actually sitting
    // at Writeback with the hardware's mw_pc for this cycle — not just because it's physically
    // placed on the Writeback station (placement alone used to mark it processed via
    // CPUStation's processing timer, which let players skip pressing T entirely).
    // stateB here is the pre-tick snapshot PipelineValidator just validated against, so mw_pc
    // still describes the instruction retiring this cycle.
    private void MarkRetiredWritebackBrick(CPUState stateB)
    {
        foreach (var station in stations)
        {
            if (station == null || !station.HasBrick) continue;
            if (station.AssignedStage != PipelineStage.Writeback) continue;
            if (stateB.mw_pc == station.CurrentBrick.InstructionPc)
                station.CurrentBrick.MarkProcessed();
        }
    }
}
