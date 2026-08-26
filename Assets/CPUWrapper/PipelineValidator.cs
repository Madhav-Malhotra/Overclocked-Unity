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

    // Single-issue (FiveStage/Blackbox) entry point — wraps the one CPUState as way 0.
    public static ValidateResult Validate(CPUState stateB, CPUStation[] stations, uint nextSpawnPc)
    {
        return Validate(new[] { stateB }, stations, nextSpawnPc);
    }

    // Superscalar entry point — statesB.Length == 1 for single-issue, 2 for two-way superscalar.
    public static ValidateResult Validate(CPUState[] statesB, CPUStation[] stations, uint nextSpawnPc)
    {
        var errors = new List<ValidationError>();

        // Single-issue scenes (FiveStage/Blackbox) never show "way=" in messages — there's only
        // ever one way, and surfacing way info there would confuse players who've never heard of
        // the concept. Superscalar scenes (statesB.Length > 1) do show it, since it's meaningful.
        bool showWay = statesB.Length > 1;

        var expectedByWay = new Dictionary<uint, PipelineStage>[statesB.Length];
        for (int way = 0; way < statesB.Length; way++)
            expectedByWay[way] = BuildExpectedMap(statesB[way]);

        foreach (var station in stations)
        {
            if (station == null || !station.HasBrick) continue;

            InstructionBrick brick = station.CurrentBrick;
            if (brick == null) continue;

            CheckStation(station, brick, statesB, expectedByWay, showWay, errors);
        }

        for (int way = 0; way < statesB.Length; way++)
            CheckReverseFetch(statesB[way], nextSpawnPc, stations, way, showWay, errors);

        if (statesB.Length > 1)
            CheckFetchOrdering(stations, errors);

        return new ValidateResult { isValid = errors.Count == 0, errors = errors };
    }

    // expected maps pc -> stage the hardware says the instruction occupies this cycle.
    // stateB is a pre-tick snapshot, so these register values reflect the current cycle.
    // Entries with zero pc or bubble opcodes (NOP to x0) are skipped — they're pipeline gaps.
    // A stall injects a bubble at DX (opcode_dx <- NOP, addr_rd = 0), which IsBubble already
    // filters out — so a stalled instruction simply has no expected slot that cycle. This makes
    // the map already stall-correct by construction; no stall-conditional branching is needed
    // anywhere below.
    private static Dictionary<uint, PipelineStage> BuildExpectedMap(CPUState stateB)
    {
        var expected = new Dictionary<uint, PipelineStage>();

        if (!IsBubble(stateB.opcode_fd, stateB.addr_rd_fd) && stateB.fd_pc != 0)
            expected[stateB.fd_pc] = PipelineStage.Decode;
        if (!IsBubble(stateB.opcode_dx, stateB.addr_rd_dx) && stateB.dx_pc != 0)
            expected[stateB.dx_pc] = PipelineStage.Execute;
        if (!IsBubble(stateB.opcode_xm, stateB.addr_rd_xm) && stateB.xm_pc != 0)
            expected[stateB.xm_pc] = PipelineStage.Memory;
        if (!IsBubble(stateB.opcode_mw, stateB.addr_rd_mw) && stateB.mw_pc != 0)
            expected[stateB.mw_pc] = PipelineStage.Writeback;

        return expected;
    }

    private static void CheckStation(CPUStation station, InstructionBrick brick, CPUState[] statesB,
        Dictionary<uint, PipelineStage>[] expectedByWay, bool showWay, List<ValidationError> errors)
    {
        uint pc = brick.InstructionPc;
        PipelineStage brickStage = station.AssignedStage;
        int stationWay = station.AssignedWay;
        string pcTag = showWay ? $"[pc=0x{PcHex(pc)}, way={stationWay}]" : $"[pc=0x{PcHex(pc)}]";

        CPUState wayState = statesB[stationWay];
        Dictionary<uint, PipelineStage> expected = expectedByWay[stationWay];

        // brick.Way is only assigned at Fetch (CPUStation.PlaceBrick) and starts at -1
        // (unset) until then, so a brick that was never fetched carries no meaningful way
        // info yet. Only trust it — ahead of the expected-stage check below — once it's
        // been assigned AND the PC is actually active in the pipeline for the way it was
        // fetched into; otherwise a brick dropped straight into a later stage without ever
        // visiting Fetch would get misdiagnosed as "belongs to way X" instead of the real
        // "not in the pipeline yet" or "wrong stage" error. Always -1 on single-way scenes,
        // so unreachable there.
        if (brickStage != PipelineStage.Fetch && brick.Way != -1 && brick.Way != stationWay &&
            expectedByWay[brick.Way].ContainsKey(pc))
        {
            errors.Add(new ValidationError(brickStage,
                $"Instruction belongs to way {brick.Way}, not way {stationWay}. {pcTag}"));
            return;
        }

        if (expected.TryGetValue(pc, out PipelineStage expectedStage))
        {
            // Instruction is active in the pipeline — must be at the right stage.
            if (brickStage == expectedStage) return;

            int brickOrdinal    = (int)brickStage;
            int expectedOrdinal = (int)expectedStage;

            string wayLabel = StallLabel(wayState, stationWay);
            string msg;
            if (expectedOrdinal > brickOrdinal)
                msg = $"Instruction should have advanced to {expectedStage} (currently at {brickStage}).{wayLabel} {pcTag}";
            else
                msg = $"Instruction moved too far — should be at {expectedStage}, not {brickStage}.{wayLabel} {pcTag}";

            errors.Add(new ValidationError(brickStage, msg));
        }
        else if (brickStage == PipelineStage.Fetch && pc == wayState.pc)
        {
            // wayState is pre-tick, so wayState.pc is exactly the address IMEM is reading
            // this cycle — the correct PC for a brick currently sitting at Fetch.
        }
        else if (brickStage == PipelineStage.Fetch && statesB.Length > 1 &&
                 pc == statesB[1 - stationWay].pc)
        {
            // Matches the OTHER way's next PC — wrong-way Fetch placement. Unreachable on
            // single-way scenes.
            errors.Add(new ValidationError(brickStage,
                $"Instruction should not be placed in way {stationWay}. {pcTag}"));
        }
        else
        {
            // Brick's PC is not in any active pipeline stage and not the next fetch.
            errors.Add(new ValidationError(brickStage,
                $"Instruction is not in the pipeline at this stage. {pcTag}"));
        }
    }

    // wayState.pc is the instruction the hardware is fetching this cycle for this way. If that
    // instruction has already been spawned (pc < nextSpawnPc), the player must have moved it to
    // this way's Fetch station. Skipped once the program is exhausted for this way. Also handles
    // stalls correctly: a stalled brick stays at Fetch, so atFetch stays true.
    private static void CheckReverseFetch(CPUState wayState, uint nextSpawnPc, CPUStation[] stations, int way,
        bool showWay, List<ValidationError> errors)
    {
        uint wayPc = wayState.pc;
        if (wayPc >= nextSpawnPc) return;

        foreach (var station in stations)
        {
            if (station != null && station.HasBrick &&
                station.AssignedStage == PipelineStage.Fetch &&
                station.AssignedWay == way &&
                station.CurrentBrick != null &&
                station.CurrentBrick.InstructionPc == wayPc)
                return;
        }

        string pcTag = showWay ? $"[pc=0x{PcHex(wayPc)}, way={way}]" : $"[pc=0x{PcHex(wayPc)}]";
        errors.Add(new ValidationError(PipelineStage.Fetch,
            $"Instruction should be at Fetch.{StallLabel(wayState, way)} {pcTag}"));
    }

    // Way 0 always fetches even-indexed instruction words, way 1 the odd-indexed ones that
    // follow, in fixed program order — so way 1's Fetch brick must never carry an older PC than
    // way 0's, and neither Fetch station should sit empty while that way still has work to fetch.
    private static void CheckFetchOrdering(CPUStation[] stations, List<ValidationError> errors)
    {
        CPUStation fetch0 = null, fetch1 = null;
        foreach (var station in stations)
        {
            if (station == null || station.AssignedStage != PipelineStage.Fetch) continue;
            if (station.AssignedWay == 0) fetch0 = station;
            else if (station.AssignedWay == 1) fetch1 = station;
        }

        if (fetch0 != null && fetch0.HasBrick && fetch1 != null && fetch1.HasBrick &&
            fetch1.CurrentBrick.InstructionPc < fetch0.CurrentBrick.InstructionPc)
        {
            errors.Add(new ValidationError(PipelineStage.Fetch,
                $"Way 1 Fetch holds an older instruction than way 0. [way0_pc=0x{PcHex(fetch0.CurrentBrick.InstructionPc)}, way1_pc=0x{PcHex(fetch1.CurrentBrick.InstructionPc)}]"));
        }
    }

    // Cosmetic-only: appends " (way <n> stalled)" to a message when this way's stall bit is set,
    // so a misdiagnosed error is easier to explain — never used to decide whether an error fires.
    private static string StallLabel(CPUState wayState, int way)
    {
        bool stalled = way == 0 ? wayState.stall_0 != 0 : wayState.stall_1 != 0;
        return stalled ? $" (way {way} stalled)" : string.Empty;
    }

    private static bool IsBubble(byte opcode, byte addrRd)
    {
        return opcode == 0 || (opcode == NopOpcode && addrRd == 0);
    }

    // Error messages show only the rightmost 4 hex digits of a PC — full 32-bit addresses are
    // more than players need to disambiguate instructions in these small programs.
    private static string PcHex(uint pc) => (pc & 0xFFFF).ToString("X4");
}
