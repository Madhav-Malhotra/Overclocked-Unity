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

        Debug.Log($"Temp latest instruction loaded: instr_fd_w={stateB.instruction}");
        foreach (var station in stations)
        {
            if (station == null || !station.HasBrick) continue;
            var brick = station.CurrentBrick;

            if (station.AssignedStage == PipelineStage.Decode)
            {
                brick.MarkDecoded();
            }
            else if (station.AssignedStage == PipelineStage.Memory)
            {
                bool isMemOp = InstructionClassifier.IsMemoryOp(brick.InstructionLabel);
                if (isMemOp)
                {
                    brick.CaptureMemAddr(stateB.alu_out);
                }
            }
            else if (station.AssignedStage == PipelineStage.Writeback)
            {
                bool isStoreOp = InstructionClassifier.IsStoreOp(brick.InstructionLabel);
                Debug.Log($"[Monitor Capture] Writeback station: pc=0x{brick.InstructionPc:X8} label='{brick.InstructionLabel}' isStoreOp={isStoreOp} addr_rd_mw={stateB.addr_rd_mw}, wb_data={stateB.wb_data}");

                if (!isStoreOp)
                {
                    brick.SetDestReg(stateB.addr_rd_mw, stateB.wb_data);
                }
            }
        }
    }
}
