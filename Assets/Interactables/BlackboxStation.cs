using System.Collections;
using UnityEngine;

public class BlackboxStation : Table
{
    [Header("CPU")]
    [SerializeField] private CPUController cpuController;

    [Header("Processing Settings")]
    [SerializeField] private float tickInterval = 0.3f;

    private bool isProcessing;

    public override bool CanBeHighlighted()
    {
        return CanInteract();
    }

    public override bool CanInteract()
    {
        if (isProcessing) return false;
        return base.CanInteract();
    }

    public override void OnInteract()
    {
        if (HoldingSystem == null) return;

        if (HasBrick)
        {
            InstructionBrick brickToPickup = RemoveBrick();
            HoldingSystem.PickUpBrick(brickToPickup);
            return;
        }

        if (!TryPlaceHeldBrick())
        {
            Debug.LogWarning("BlackboxStation: Failed to place brick");
            return;
        }

        if (cpuController == null)
        {
            Debug.LogError("BlackboxStation: cpuController not assigned");
            return;
        }

        StartCoroutine(RunUntilRetired(CurrentBrick));
    }

    private IEnumerator RunUntilRetired(InstructionBrick brick)
    {
        isProcessing = true;

        bool decodedCaptured = false;
        bool memCaptured = false;

        while (brick != null && cpuController.GetStateB().mw_pc != brick.InstructionPc)
        {
            cpuController.AdvanceTick();
            CPUState stateB = cpuController.GetStateB();

            if (!decodedCaptured && stateB.dx_pc == brick.InstructionPc)
            {
                InstructionMonitorCapture.CaptureAtStage(brick, stateB, PipelineStage.Decode);
                decodedCaptured = true;
            }
            if (!memCaptured && stateB.xm_pc == brick.InstructionPc)
            {
                InstructionMonitorCapture.CaptureAtStage(brick, stateB, PipelineStage.Memory);
                memCaptured = true;
            }

            yield return new WaitForSeconds(tickInterval);
        }

        if (brick != null)
        {
            InstructionMonitorCapture.CaptureAtStage(brick, cpuController.GetStateB(), PipelineStage.Writeback);
            brick.MarkProcessed();
        }

        isProcessing = false;
    }
}
