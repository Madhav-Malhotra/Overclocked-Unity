// Single source of truth for feeding InstructionMonitorUI off a brick reaching a given
// pipeline stage. Callers (TickButtonHandler for FiveStage, BlackboxStation for Blackbox,
// future architecture stations) are responsible for figuring out *which brick is at which
// stage this tick* — that detection differs per architecture — but the resulting mutation
// on the brick must stay identical everywhere, so it lives here once.
public static class InstructionMonitorCapture
{
    // stateB must be the CPUState snapshot from the SAME tick the brick reached `stage`
    // (see TickButtonHandler's original comment on pre-advance vs post-advance timing:
    // alu_out/addr_rd_mw/wb_data describe whichever instruction is at that stage right now,
    // so capture before AdvanceTick() shifts the pipeline again).
    public static void CaptureAtStage(InstructionBrick brick, CPUState stateB, PipelineStage stage)
    {
        if (stage == PipelineStage.Decode)
        {
            brick.MarkDecoded();
        }
        else if (stage == PipelineStage.Memory)
        {
            if (InstructionClassifier.IsMemoryOp(brick.InstructionLabel))
                brick.CaptureMemAddr(stateB.alu_out);
        }
        else if (stage == PipelineStage.Writeback)
        {
            if (!InstructionClassifier.IsStoreOp(brick.InstructionLabel))
                brick.SetDestReg(stateB.addr_rd_mw, stateB.wb_data);
        }
    }
}
