using System.Collections.Generic;

public struct ValidationError
{
    public PipelineStage stage;
    public string message;

    public ValidationError(PipelineStage stage, string message)
    {
        this.stage = stage;
        this.message = message;
    }
}

public struct ValidateResult
{
    public bool isValid;
    public List<ValidationError> errors;
}

public static class PipelineValidator
{
    private const byte NopOpcode = 0x13;

    public static ValidateResult Validate(CPUState stateB, CPUStation[] stations, uint nextSpawnPc)
    {
        var errors = new List<ValidationError>();

        // expected maps pc -> stage the hardware says each instruction occupies this cycle.
        // stateB is a pre-tick snapshot, so these register values reflect the current cycle.
        // Entries with zero pc or bubble opcodes (NOP to x0) are skipped — they're pipeline gaps.
        var expected = new Dictionary<uint, PipelineStage>();

        if (!IsBubble(stateB.opcode_fd, stateB.addr_rd_fd) && stateB.fd_pc != 0)
            expected[stateB.fd_pc] = PipelineStage.Decode;
        if (!IsBubble(stateB.opcode_dx, stateB.addr_rd_dx) && stateB.dx_pc != 0)
            expected[stateB.dx_pc] = PipelineStage.Execute;
        if (!IsBubble(stateB.opcode_xm, stateB.addr_rd_xm) && stateB.xm_pc != 0)
            expected[stateB.xm_pc] = PipelineStage.Memory;
        if (!IsBubble(stateB.opcode_mw, stateB.addr_rd_mw) && stateB.mw_pc != 0)
            expected[stateB.mw_pc] = PipelineStage.Writeback;

        foreach (var station in stations)
        {
            if (station == null || !station.HasBrick) continue;

            InstructionBrick brick = station.CurrentBrick;
            if (brick == null) continue;

            uint pc = brick.InstructionPc;
            PipelineStage brickStage = station.AssignedStage;

            if (expected.TryGetValue(pc, out PipelineStage expectedStage))
            {
                // Instruction is active in the pipeline — must be at the right stage.
                if (brickStage == expectedStage) continue;

                int brickOrdinal    = (int)brickStage;
                int expectedOrdinal = (int)expectedStage;

                string msg;
                if (expectedOrdinal > brickOrdinal)
                    msg = $"Instruction should have advanced to {expectedStage} (currently at {brickStage}). [pc=0x{pc:X8}]";
                else
                    msg = $"Instruction moved too far — should be at {expectedStage}, not {brickStage}. [pc=0x{pc:X8}]";

                errors.Add(new ValidationError(brickStage, msg));
            }
            else if (brickStage == PipelineStage.Fetch && pc == stateB.pc)
            {
                // stateB is pre-tick, so stateB.pc is exactly the address IMEM is reading
                // this cycle — the correct PC for a brick currently sitting at Fetch.
                continue;
            }
            else
            {
                // Brick's PC is not in any active pipeline stage and not the next fetch.
                // The player put it somewhere it doesn't belong yet.
                errors.Add(new ValidationError(brickStage,
                    $"Instruction is not in the pipeline at this stage. [pc=0x{pc:X8}]"));
            }
        }

        // Reverse Fetch check: stateB.pc is the instruction the hardware is fetching this cycle.
        // If that brick has already been spawned (pc < nextSpawnPc), the player must have moved
        // it to Fetch. Skipped when pc >= nextSpawnPc — program is exhausted, hardware fetches NOPs.
        // Also handles stalls correctly: a stalled brick stays at Fetch, so atFetch stays true.
        bool fetchRequired = stateB.pc < nextSpawnPc;
        if (fetchRequired)
        {
            bool atFetch = false;
            foreach (var station in stations)
            {
                if (station != null && station.HasBrick &&
                    station.AssignedStage == PipelineStage.Fetch &&
                    station.CurrentBrick != null &&
                    station.CurrentBrick.InstructionPc == stateB.pc)
                {
                    atFetch = true;
                    break;
                }
            }

            if (!atFetch)
                errors.Add(new ValidationError(PipelineStage.Fetch,
                    $"Instruction should be at Fetch. [pc=0x{stateB.pc:X8}]"));
        }

        return new ValidateResult { isValid = errors.Count == 0, errors = errors };
    }

    private static bool IsBubble(byte opcode, byte addrRd)
    {
        return opcode == 0 || (opcode == NopOpcode && addrRd == 0);
    }
}
