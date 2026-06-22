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
        Debug.Log(
            $"[Tick] stateB before advance:\n" +
            $"  pc=0x{stateB.pc:X8}\n" +
            $"  fd_pc=0x{stateB.fd_pc:X8} opcode_fd=0x{stateB.opcode_fd:X2} rd_fd={stateB.addr_rd_fd}\n" +
            $"  dx_pc=0x{stateB.dx_pc:X8} opcode_dx=0x{stateB.opcode_dx:X2}\n" +
            $"  xm_pc=0x{stateB.xm_pc:X8} opcode_xm=0x{stateB.opcode_xm:X2}\n" +
            $"  mw_pc=0x{stateB.mw_pc:X8} opcode_mw=0x{stateB.opcode_mw:X2}"
        );

        foreach (var station in stations)
        {
            if (station == null || !station.HasBrick) continue;
            var brick = station.CurrentBrick;
            Debug.Log($"[Tick] Brick at {station.AssignedStage}: pc=0x{brick.InstructionPc:X8}");
        }

        uint nextSpawnPc = startPlatform != null ? startPlatform.NextSpawnPc : uint.MaxValue;
        ValidateResult result = PipelineValidator.Validate(stateB, stations, nextSpawnPc);

        if (result.isValid)
        {
            feedbackUI?.Hide();
            cpuController.AdvanceTick();
        }
        else
        {
            feedbackUI?.ShowErrors(result.errors);
            foreach (var error in result.errors)
                Debug.LogWarning($"[Tick validation] {error.stage}: {error.message}");
        }
    }
}
