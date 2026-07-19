# Instruction Monitor — Implementation Plan

## Goal

Add a fixed, large "Instruction Monitor" UI panel (mounted on the wall behind the
scene, always visible, scoreboard-style) that shows progressively more detail about
whichever instruction brick the player currently holds, as that instruction advances
through the pipeline:

- **Before Decode completes** (brick has been ticked past Decode station, i.e.
  `PipelineStage.Decode` reached, but not yet processed to `Execute`): only **PC** field has a value.
- **After Decode completes** (`InstructionBrick.CurrentStage >= PipelineStage.Execute`):
  **PC** + **INST** (assembly text from `InstructionData.label`) fields are filled in.
- **After Execute completes** (`CurrentStage >= PipelineStage.Memory`): PC + INST +
  **RD** (destination register name and value) fields are filled in.
- **After Memory completes** (`CurrentStage >= PipelineStage.Writeback`): Fill in all fields: PC + INST +
  RD + **ADDR** (memory address and value), only if the instruction is a load/store.

Fixed fields: `PC`, `INST`, `RD`, `ADDR`. Fields not yet known are blanked/dashed, not hidden —
this keeps the scoreboard layout stable (reads like a real hardware debug console).

## Key existing pieces this plugs into

- `Assets/Interactables/InstructionBrick.cs:56-60` — `InstructionPc`, `InstructionHex`,
  `CurrentStage` (public getters already exist). `SetStage()` (`InstructionBrick.cs:139-143`)
  is called from `CPUStation.OnProcessingComplete()` (`CPUStation.cs:199-224`) once a
  station's processing timer finishes — this is the existing "stage completed" signal.
- `Assets/Interactables/PipelineStage.cs:1-9` — enum `Unprocessed, Fetch, Decode, Execute,
  Memory, Writeback`, used as ordinal comparisons throughout (see
  `PipelineValidator.cs:58-59` comparing stage ordinals).
- `Assets/Player/InstructionBrickHoldingSystem.cs:11,18-21` — `heldBrick` field and
  `GetHeldBrick()` — the authoritative source for "which brick is the player currently
  carrying."
- `Assets/Scripts/LevelData.cs:4-8` — `InstructionData.label` already contains the
  human-readable RISC-V assembly text (confirmed against
  `Assets/Levels/Resources/JSON/level_01.json`, e.g. `"add x1, x2, x3"`). **No disassembler
  needs to be written** — we only need to carry the `label` string from `InstructionData`
  onto the `InstructionBrick` instance, the same way `hex` is currently carried in
  `StartPlatform.SpawnNextInstruction()` (`StartPlatform.cs:44-55`).
- `Assets/CPUWrapper/CPU.cs:8-74` — `CPUState` struct: has `regs[32]` (register file),
  `aluOut` (ALU result — this is the computed memory address for loads/stores, since
  there is no separate "target address" field), `dmem_data_out`/`wb_data` (read-back
  values), `addr_rd`/`addr_rd_dx`/`addr_rd_xm`/`addr_rd_mw` (destination register index
  per stage), `mem_rw` (whether this is a memory op).
- `Assets/CPUWrapper/CPUController.cs:46` — `GetStateB()` is the only way to read live
  CPU register/ALU state; there is no per-instruction history, so the monitor must read
  register/ALU values off `CPUState` **at the moment a stage completes**, not later
  (register file mutates on every tick). **WARNING: A LATER INSTRUCTION MIGHT MODIFY THE SAME REGISTER OR ADDRESS AS AN EARLIER INSTRUCTION. THE UI MUST CAPTURE THE REGISTER/ADDRESS VALUES AT THE TIME EACH INSTRUCTION REACHES THE RIGHT STAGE AND NOT MODIFY THEM LATER. THIS WILL LEAD TO LATER INSTRUCTIONS POSSIBLY HAVING DIFFERENT VALUES FOR THE SAME REGISTERS/ADDRESSES THAN EARLIER INSTRUCTIONS (EXPECTED BEHAVIOUR)**.
- `Assets/UI/HUD/GameHUD.cs` and `Assets/UI/HUD/TickFeedbackUI.cs:6-20` — existing pattern
  for a `MonoBehaviour` that holds a `[SerializeField] UIDocument`, queries elements by
  name in `Awake()`, and exposes public methods to update text/visibility. The Instruction
  Monitor follows this exact pattern rather than introducing a new UI framework.
- `Assets/UI/HUD/HUD.uxml` — the shared UI Toolkit tree currently used for the HUD; the
  monitor lives in its own new `UIDocument`/UXML with its own **world-space**
  `PanelSettings`, rather than being squeezed into this file or reusing the screen-space
  `PanelSettings` under `Assets/UI/Shared/`.

## Design decision: world-space wall-mounted panel (confirmed)

The Instruction Monitor is a **World Space `UIDocument`** rendered onto a wall surface
behind the CPU stations, not a screen-space HUD overlay. This requires:

- A new `PanelSettings` asset (e.g. `Assets/UI/InstructionMonitor/InstructionMonitorPanelSettings.asset`)
  with `Scale Mode` set to `World Space` (Unity 6 UI Toolkit `PanelSettings.renderMode =
  RenderMode.WorldSpace`), distinct from whatever `PanelSettings` the screen-space HUD in
  `Assets/UI/Shared/` uses.
- A `GameObject` placed against/in front of a wall behind the CPU stations in the gameplay
  scene, holding the `UIDocument` component pointing at `InstructionMonitor.uxml` +
  the new world-space `PanelSettings`. Its `RectTransform`-equivalent sizing (the
  `UIDocument`'s panel size in world units) needs to be large enough to read at typical
  player-to-wall distance — exact scene position/scale is a **[USER ACTION REQUIRED]** step
  during Phase 2, since it depends on the physical layout of the CPU station scene which
  should be tuned visually in the Editor rather than guessed at from code.
- Because it's world-space, normal screen-space USS sizing (`px` anchored to screen edges)
  doesn't apply the same way — the panel's `width`/`height` in the UXML root should be set
  in a way that scales with the `UIDocument`'s world-space panel dimensions (Unity maps UI
  Toolkit layout units to world units via the `PanelSettings.scale` / reference resolution
  fields), so font size and row spacing will likely need visual iteration in Play Mode
  rather than being correct on the first pass.
- Legibility depends on camera framing — since the player moves around the CPU station
  floor, confirm during Phase 3 verification (screenshot from roughly where the player
  stands during normal play) that text is actually readable at that distance, not just
  present.

## Files to create

### 1. `Assets/UI/InstructionMonitor/InstructionMonitor.uxml` (new)
UI Toolkit tree for the scoreboard panel. Structure:
- Root container `instruction-monitor-panel`, positioned centered (or top-center) via USS,
  styled as a large dark panel with a border (reusing `Assets/DesignSystem/` classes per
  `sk-design` conventions — e.g. `ds-panel`/`ds-card` equivalents, confirm exact class
  names against the design system before writing USS).
- Four label rows, each with a static field-name label and a dynamic value label, named
  so C# can query them by `name`:
  - `pc-row` containing `pc-label` (static "PC") and `pc-value` (dynamic).
  - `inst-row` containing `inst-label` ("INST") and `inst-value`.
  - `rd-row` containing `rd-label` ("RD") and `rd-value`.
  - `addr-row` containing `addr-label` ("ADDR") and `addr-value`.
- All four rows always present in the tree (not conditionally added/removed) — visibility
  of value text is controlled by C# setting placeholder text (e.g. `"--"`) vs real values,
  per the "scoreboard" look requested (rows don't disappear, they show blank/dashes).

### 2. `Assets/UI/InstructionMonitor/InstructionMonitor.uss` (new)
Styling for the panel: large fixed size/position, monospace font (reuse `Assets/Resources/DsFonts/Poppins/`
per `.claude/status.md:11`, or check design system for a monospace alternative since a
"scoreboard" reads better in monospace — flag as an open styling question, not blocking),
row layout (flex-direction column, field-name + value flex-direction row), dashed/blank
state color treatment for unknown fields (e.g. dimmed grey `--value` text) vs revealed
state (bright/white).

### 3. `Assets/UI/InstructionMonitor/InstructionMonitorUI.cs` (new)
`MonoBehaviour`, same shape as `Assets/UI/HUD/TickFeedbackUI.cs:6-20`:
- `[SerializeField] private UIDocument uiDocument;`
- `[SerializeField] private InstructionBrickHoldingSystem holdingSystem;` — polled each
  frame in `Update()`, mirroring how `PlayerInteractionHandler.cs:70-87` polls
  `holdingSystem.IsHoldingBrick()`. No event needed since `InstructionBrickHoldingSystem`
  currently exposes no pickup/place events (`PickUpBrick`/`PlaceBrick` in
  `InstructionBrickHoldingSystem.cs:23-57,67-106` are plain method calls, not C# events) —
  polling matches the existing codebase pattern rather than introducing a new event system.
- `[SerializeField] private CPUController cpuController;` — needed to read live `CPUState`
  for register/ALU/memory values once a stage completes.
- Fields: `Label pcValue, instValue, rdValue, addrValue;` populated in `Awake()` via
  `root.Q<Label>(...)`, same as `GameHUD.cs:14-17`.
- `void Awake()` — query `uiDocument.rootVisualElement`, resolve the four value labels.
- `void Update()`:
  - Get `InstructionBrick brick = holdingSystem.GetHeldBrick();`
  - If `brick == null`: call a `Clear()` method (blank all four fields to `"--"`) and return.
  - Otherwise call `Refresh(brick)`.
- `private void Refresh(InstructionBrick brick)`:
  - Always set `pcValue.text = $"0x{brick.InstructionPc:X8}"` — PC is shown regardless of
    stage per the spec ("before decode stage has finished, they should just see the PC").
  - If `brick.CurrentStage >= PipelineStage.Execute` (i.e. Decode has completed — recall
    `SetStage()` is only called when a station's `OnProcessingComplete()` fires, so
    `CurrentStage == PipelineStage.Execute` means Decode's processing finished and the
    brick was advanced into Execute's station, matching the "after decode stage has
    finished" trigger condition described by the user, which is: press T after the
    instruction was on the decode station) — set `instValue.text` from the brick's stored
    assembly label (new field, see `InstructionBrick.cs` changes below). Otherwise leave
    blank/dashed.
  - If `brick.CurrentStage >= PipelineStage.Memory`: look up destination register name and
    value. Register **name** derives from the stored instruction's `addr_rd` — but
    `InstructionBrick` currently has no `addr_rd` field (see brick changes below). Register
    **value** comes from `cpuController.GetStateB().regs[rd]` — but this reads the *live*
    current register file, which will already reflect writeback timing quirks if multiple
    ticks have passed; document this as a **known approximation**: the monitor shows the
    live register file value at query time, which will be correct once writeback has
    happened for this instruction but could theoretically show a stale/updated-by-another-
    instruction value if the same register was reused as `rd` by a later instruction before
    this brick reaches Writeback. Given the small pipeline depth (5 stages) and single-issue
    RV32I core, this is very unlikely in practice but should be called out to the user as an
    edge case, not silently assumed correct.
  - If `brick.CurrentStage >= PipelineStage.Writeback` AND the instruction is a load/store
    (determined by checking whether `brick`'s stored assembly label starts with a memory
    mnemonic — `lw`, `lb`, `lh`, `lbu`, `lhu`, `sw`, `sb`, `sh` — a simple string-prefix
    check against the label text, since there is no opcode field cached on the brick and we
    already need the label string for `Inst`): set `addrValue.text` to the computed address.
    Address value: since `CPUState` has no explicit "target memory address" field (see
    `CPU.cs:8-74` — only `aluOut`, `dmem_data_in`, `dmem_data_out`, `mem` exist), the
    address must be captured from `aluOut` **at the moment Memory stage completes**, not
    read later, because `aluOut` reflects whatever the ALU most recently computed (any
    instruction), not this specific instruction's address, once more ticks occur. This
    means capturing the address requires a snapshot taken at `SetStage(Memory)` time (see
    `InstructionBrick.cs` change below), not a live read in `Refresh()`.
  - If not a load/store: leave `addrValue.text` at `"--"` even once Writeback completes,
    per spec ("if one is involved").

### 4. Wiring the new `UIDocument` into the scene
- Create a new `PanelSettings` asset (`InstructionMonitorPanelSettings.asset`) with
  `renderMode = World Space` (see Design Decision above) — separate from the screen-space
  `PanelSettings` under `Assets/UI/Shared/` used by `HUD.uxml`.
- Create a new GameObject (e.g. `InstructionMonitorWallPanel`) positioned against the wall
  behind the CPU stations in the gameplay scene (exact scene/position to be located and
  chosen during Phase 2 via `Unity_FindProjectAssets`/`Unity_ManageScene` — **[USER ACTION
  REQUIRED]** to confirm final placement/scale visually in the Editor). Attach a `UIDocument`
  component pointing at `InstructionMonitor.uxml` + the new world-space `PanelSettings`.
- Attach `InstructionMonitorUI.cs`, wire `uiDocument`, `holdingSystem` (find the player's
  `InstructionBrickHoldingSystem` instance in the scene), and `cpuController` (find the
  scene's `CPUController`, same instance `TickButtonHandler` in `Assets/UI/TickButtonHandler.cs:6`
  already references) via the Inspector.
  **[USER ACTION REQUIRED]**: since `Unity_ManageGameObject` cannot reliably wire
  `UnityEngine.Object` reference fields (`CLAUDE.md` constraint), the user will need to
  drag the `UIDocument`, `InstructionBrickHoldingSystem`, and `CPUController` references
  onto the new component in the Inspector manually if the automated wiring attempt fails.
  **[USER ACTION REQUIRED]**: position/scale/rotate the `InstructionMonitorWallPanel`
  GameObject against the wall geometry and tune the world-space panel size/font scale in
  Play Mode until legible from the player's normal vantage point — this is inherently a
  visual/Editor task, not something to guess correctly from code alone.

## Files to modify

### Register/address field mapping (confirmed against `../verilog/design/code/pd.v`)

`addr_rd_dx_r`, `addr_rd_xm_r`, `addr_rd_mw_r` (`pd.v:93,104,109`) are genuine pipeline
registers, each holding the destination register address of whichever instruction
currently occupies that stage, updated synchronously every clock edge. So the `CPUState`
field naming in `CPU.cs:56-73` is trustworthy as-is:
- **Rd** (shown once Execute completes): use `addr_rd_dx` — the rd of the instruction now
  sitting in Execute.
- **Addr** (shown once Memory completes, load/store only): the dmem address is `alu_xm_r`
  (`pd.v:434`, fed into `dmemory.address`), which `CPUState` exposes as `aluOut` when read
  at the right tick — NOT a separate "target address" field, confirming the earlier
  finding that `aluOut` doubles as the memory address for loads/stores.

### Capture timing (confirmed: Option B)

Captures happen in `TickButtonHandler.OnTickPressed()` (`TickButtonHandler.cs:26-65`),
immediately after `cpuController.AdvanceTick()` succeeds (line 57) — this is the only point
actually synchronized with a real hardware clock edge, unlike `CPUStation`'s flat
1-second real-time processing timer (`CPUStation.cs:166`), which has no fixed relationship
to tick boundaries. Concretely: after `AdvanceTick()`, iterate `stations` (already available
in `TickButtonHandler.cs:9,14`), read the fresh `CPUState` via `cpuController.GetStateB()`,
and for each station whose brick's `CurrentStage` is about to advance (Execute or Memory),
call the appropriate new brick setter (`SetDestRegAddr`/`CaptureMemAddr`, see below) with
the value read from that state — before or in the same pass as `CPUStation.OnProcessingComplete()`
later calls `SetStage()` on the same brick. This means `CPUStation.cs` does **not** need
changes to read `CPUState` at all — the capture logic lives entirely in `TickButtonHandler.cs`,
which already has direct access to `cpuController` and `stations`.

### `Assets/Interactables/InstructionBrick.cs`
- Add new serialized/private fields to carry data the monitor needs that isn't currently
  stored on the brick:
  - `private string instructionLabel;` + `public string InstructionLabel => instructionLabel;`
    + `public void SetInstructionLabel(string label) { instructionLabel = label; }` — mirrors
    the existing `SetInstructionPc`/`SetInstructionHex` pattern at `InstructionBrick.cs:56-59`.
  - `private byte destRegAddr;` + `public byte DestRegAddr => destRegAddr;` +
    `public void SetDestRegAddr(byte addr) { destRegAddr = addr; }` — needed so the monitor
    can show which register name (`x{n}`) is the destination without re-deriving it from
    the label string.
  - `private uint capturedMemAddr;` + `private bool hasCapturedMemAddr;` +
    `public uint CapturedMemAddr => capturedMemAddr;` + `public bool HasCapturedMemAddr => hasCapturedMemAddr;`
    + a method `public void CaptureMemAddr(uint addr) { capturedMemAddr = addr; hasCapturedMemAddr = true; }`
    — the "snapshot at stage-completion time" mechanism described above.
- `SetStage()` (`InstructionBrick.cs:139-143`) is **not modified**. Capture is entirely
  driven from `TickButtonHandler.OnTickPressed()` (see that section below), which calls
  the new `SetDestRegAddr`/`CaptureMemAddr` setters directly on the brick — `InstructionBrick`
  stays a passive data holder with no `CPUController`/`CPUState` reference of its own.

### `Assets/UI/TickButtonHandler.cs`
- Modify `OnTickPressed()` (`TickButtonHandler.cs:26-65`): after `cpuController.AdvanceTick()`
  (line 57), read `CPUState freshState = cpuController.GetStateB();` and loop over `stations`
  (`TickButtonHandler.cs:9,14`). For each station with a brick:
  - If the brick's `CurrentStage` is `Decode` (about to be pushed to `Execute` by
    `CPUStation.OnProcessingComplete()`), call `brick.SetDestRegAddr(freshState.addr_rd_dx)`.
  - If the brick's `CurrentStage` is `Execute` (about to be pushed to `Memory`) and the
    instruction is a load/store (via the shared classifier below), call
    `brick.CaptureMemAddr(freshState.aluOut)`.
- `CPUStation.cs` is **not modified** — `OnProcessingComplete()` (`CPUStation.cs:199-224`)
  keeps calling `SetStage()` exactly as it does today; the capture logic lives entirely in
  `TickButtonHandler.cs`, decoupled from `CPUStation`'s real-time processing timer.

**Worked example (capture-before-display-gate is intentional, not a bug):** a brick sits
at the Decode station with `CurrentStage == Decode`. Player presses T. `OnTickPressed()`
validates, calls `AdvanceTick()`, then immediately (same key-press, same frame) calls
`brick.SetDestRegAddr(freshState.addr_rd_dx)` — this happens *before* the brick's
`CurrentStage` has changed to `Execute`, because that transition only happens later, whenever
`CPUStation.OnProcessingComplete()`'s independent 1-second real-time timer fires and calls
`SetStage(Execute)`. So `DestRegAddr` is captured early and simply sits on the brick,
unused, until `InstructionMonitorUI.Refresh()`'s display gate (`CurrentStage >=
PipelineStage.Memory`) later becomes true and reveals it. This is safe as long as nothing
else overwrites `DestRegAddr` in between — which holds, since only `TickButtonHandler`
writes it and it only does so once per brick (guarded by the `CurrentStage == Decode`
check, which becomes false as soon as `SetStage(Execute)` runs). **Do not "simplify" this
by moving the capture to fire only when `CurrentStage` has already reached `Execute`/`Memory`**
— by then `CPUState` has advanced past the tick that held this instruction's values, and
the captured value would be wrong (see the WARNING in "Key existing pieces" above about
register/address reuse by later instructions).

### `Assets/Interactables/StartPlatform.cs`
- Modify `SpawnNextInstruction()` (`StartPlatform.cs:15-59`): after the existing
  `brick.SetInstructionHex(hexValue)` call at line 49, add
  `brick.SetInstructionLabel(next.label);` so the assembly text is available on the brick
  from the moment it spawns (needed later once Decode completes, per the display-timing
  rule) — mirrors the existing hex-copying pattern at lines 44-55 exactly.

## New shared helper (optional, avoids duplication)

### `Assets/Scripts/InstructionClassifier.cs` (new, static helper)
- `public static bool IsMemoryOp(string label)` — returns true if `label` (trimmed,
  lowercased) starts with one of `lw`, `lb`, `lh`, `lbu`, `lhu`, `sw`, `sb`, `sh` followed by
  whitespace. Used by both `CPUStation.cs` (to decide whether to capture a memory address)
  and `InstructionMonitorUI.cs` (to decide whether to render the Addr row). Avoids two
  independently-maintained mnemonic lists drifting out of sync.

## Edge cases to handle

- **Player picks up a brick mid-pipeline and holds it without placing it anywhere.**
  `holdingSystem.GetHeldBrick()` (`InstructionBrickHoldingSystem.cs:18-21`) still returns
  it, so the monitor keeps showing whatever fields were already captured/gated at pickup
  time — this is correct/desired behavior (matches "as users pick up different
  instructions, the monitor should show info about the instructions"), not a bug to guard
  against.
- **Player places a brick back at an earlier station than where it already reached**
  (e.g. picks a brick up from Memory and places it back at Execute). `DestRegAddr`/
  `CapturedMemAddr` are **not reset** when this happens — the brick keeps whatever it
  already captured. This is believed to be a non-issue in practice: `PipelineValidator.Validate()`
  (`PipelineValidator.cs:25-116`) runs on every T press and rejects ticks where a brick's
  station doesn't match its expected pipeline stage (`PipelineValidator.cs:53-68`), so the
  game should already prevent the player from advancing the tick while a brick sits at a
  stage inconsistent with the hardware's own `expected` map — meaning the "moved backward"
  scenario should normally show up as a validation error (via `TickFeedbackUI.ShowErrors()`)
  rather than silently produce stale monitor values. **Flag for Phase 3 verification**:
  confirm by testing that deliberately moving a brick backward either (a) gets blocked by
  the validator before `AdvanceTick()`/capture ever runs, or (b) if it's not blocked, that
  the monitor's stale captured value doesn't get displayed against the wrong stage. Do not
  add speculative reset-on-pickup logic to `InstructionBrick`/`InstructionBrickHoldingSystem`
  unless Phase 3 testing actually reproduces a visible wrong-value bug — the validator is
  the existing safety net for illegal brick placement and this plan should lean on it
  rather than duplicating that logic in the monitor.
- **Two bricks with the same destination register, one ahead of the other in the
  pipeline.** Already covered by the WARNING in "Key existing pieces" above — expected
  behavior, not a bug, given the validator prevents bricks from being out of pipeline
  order in the first place.
- **Level ends / all bricks cleared while a brick is still held.** No special handling
  needed — `InstructionMonitorUI.Update()`'s `holdingSystem.GetHeldBrick() == null` branch
  already covers "nothing held," and a held brick surviving a level transition is outside
  this feature's scope (existing level-transition code, not modified by this plan).

## Open questions to resolve with the user before Phase 2 implementation

1. ~~Screen-space vs world-space placement~~ — **resolved**: world-space wall-mounted
   panel (see Design Decision section above, confirmed).
2. ~~Which `addr_rd_*` field maps to which stage~~ — **resolved**: confirmed against
   `../verilog/design/code/pd.v` (see "Register/address field mapping" section above).
3. ~~Capture timing~~ — **resolved**: capture happens in `TickButtonHandler.OnTickPressed()`
   post-tick (Option B), not in `CPUStation`'s real-time timer.
4. **Monospace font choice** for the scoreboard look — check `Assets/DesignSystem/` (via
   `sk-design`) for an available monospace font before defaulting to Poppins.

## Post-implementation note: display-gate redesign (deviates from this plan)

The shipped `InstructionMonitorUI.Refresh()` does **not** gate fields on `brick.CurrentStage`
as originally planned above — that produced a one-stage display lag (INST/RD only appeared
after the brick was moved one station further and picked back up, not right after pressing T
at the station where the data was captured). Fixed by decoupling the display gate from
`CurrentStage` entirely:

- `InstructionBrick.cs` — added `HasBeenDecoded` (set via `MarkDecoded()`) and
  `HasDestRegAddr` (set alongside `DestRegAddr` in `SetDestRegAddr()`), mirroring the
  existing `HasCapturedMemAddr` flag pattern.
- `TickButtonHandler.CaptureMonitorData()` — now also calls `brick.MarkDecoded()` when a
  tick fires while the brick's station is `PipelineStage.Decode` (previously only handled
  `Execute`/`Memory`).
- `InstructionMonitorUI.Refresh()` — INST/RD/ADDR rows now check `HasBeenDecoded` /
  `HasDestRegAddr` / `HasCapturedMemAddr` instead of `CurrentStage >= X` comparisons.

None of these flags are ever reset once set (matches the "backward move" reasoning already
in the Edge Cases section above — no reset-on-pickup logic was added).

## Edge cases to test later

- **Pick up a brick immediately after pressing T at Decode, without moving it.** INST
  should already show — the whole point of this fix was removing the "must move to Execute
  and back" requirement. Confirm PC still shows too, and RD/ADDR stay blank.
- **Press T multiple times while a brick sits at Decode before moving it on.** `MarkDecoded()`
  is idempotent (just sets a bool true), so repeated ticks at the same station should be a
  no-op after the first — confirm no double-capture side effects (there shouldn't be any,
  since `MarkDecoded()` takes no value argument, but worth eyeballing).
- **Move a brick backward** (e.g. pick it up from Execute and place it back at Decode,
  if the validator allows it at all). Check whether `HasBeenDecoded`/`HasDestRegAddr`
  (already-set, never-reset flags) cause the monitor to show INST/RD fields for a brick
  that is now sitting at an earlier station than that data implies — this is the same
  "moved backward" scenario flagged unresolved in the Edge Cases section above, now sharper
  because the display gate no longer depends on `CurrentStage` at all, only on whether the
  flag was ever set.
- **Two bricks in flight where one reaches Decode/Execute and is picked up, then a
  different brick (never ticked) is picked up.** Confirm the monitor correctly blanks
  INST/RD/ADDR for the fresh brick — i.e. that these are genuinely per-brick instance
  flags and not accidentally shared/static state.
- **Pick up a brick, place it on the correct next station, but do not press T before
  picking it back up.** INST/RD should NOT appear yet (no tick means `CaptureMonitorData()`
  never ran for that brick at that station) — confirms the fields are tied to tick capture,
  not merely to station placement.
- **Rapid T presses across multiple stations in one session** — confirm `PipelineValidator`
  still blocks illegal ticks before `CaptureMonitorData()` runs (per the existing "moved
  backward" edge case above), so flags are never set for a brick sitting at the wrong stage
  for an invalid tick.
