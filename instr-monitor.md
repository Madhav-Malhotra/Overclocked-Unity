# Instruction Monitor — Open Bug

## Feature summary

A world-space UI panel (`Assets/UI/InstructionMonitor/InstructionMonitorUI.cs`) shows
PC / INST / RD / ADDR fields for whichever `InstructionBrick` the player is holding, as it
advances through the pipeline (`Assets/Interactables/PipelineStage.cs`: `Unprocessed, Fetch,
Decode, Execute, Memory, Writeback`).

## Symptoms observed

Level `Assets/Levels/Resources/JSON/level_01.json` contains a single instruction:
`addi x1, x2, 1`. Expected: after the instruction completes, the monitor's RD field should
read `x1 = 1` (x2 is initialised to a nonzero stack-pointer value, see
`../verilog/design/code/register_file.v:46`).

Actual, across iterations of this fix:

1. RD field showed `x0 = 0` instead of `x1 = 1`.
2. After changing which pipeline-register field is read, RD field still showed
   `x0 = 0` instead of `x1 = 1`.
3. Separately, the RD field only becomes populated once T is pressed while the brick is at
   the **Memory** station — the user expects/observes that this is one T-press earlier than
   when the actual register writeback happens in hardware, i.e. the data shown appears to
   not yet reflect a completed writeback at the moment it becomes visible.

## Key files

- `Assets/UI/TickButtonHandler.cs` — `OnTickPressed()` drives `PipelineValidator.Validate()`,
  then `CaptureMonitorData()`, then `cpuController.AdvanceTick()`. `CaptureMonitorData()`
  is where RD name/value are currently captured, called with a `CPUState` obtained from
  `cpuController.GetStateB()` (`TickButtonHandler.cs:34`) taken **before** `AdvanceTick()`
  is called on the same key-press.
- `Assets/CPUWrapper/CPUController.cs` — `GetStateB()` returns a cached `CPUState` field
  (`stateB`) that is only refreshed inside `AdvanceTick()` (`CPUController.cs:48-58`), via
  `CPU.tick()` followed by `CPU.get_cpu_state()`.
- `Assets/CPUWrapper/CPU.cs` — P/Invoke declarations for `tick()` / `get_cpu_state()`, and
  the `CPUState` struct layout (must stay byte-layout-synced with the C++ struct in
  `bridge.cpp`).
- `../verilator/bridge.cpp` — native bridge. `tick()` (`bridge.cpp:191-196`) drives one
  full rising+falling clock edge on the Verilated model. `get_cpu_state()`
  (`bridge.cpp:114+`) copies internal signals into the flat `CPUState` struct, including
  `addr_rd_mw` (`bridge.cpp:179`, sourced from `design_wrapper__DOT__core__DOT__addr_rd_mw_r`)
  and `wb_data` (`bridge.cpp:157`, sourced from `design_wrapper__DOT__core__DOT__data_rd_w`).
- `Assets/Interactables/InstructionBrick.cs` — `SetDestReg(addr, value)` /
  `HasDestReg` / `DestRegAddr` / `DestRegValue` store the captured RD data on the brick
  instance (captured once, not re-read live).
- `../verilog/design/code/pd.v` — RTL source for `addr_rd_mw_r` and `data_rd_w`, if
  signal-timing semantics need to be checked against the Verilog directly rather than
  inferred from the bridge/C# layers.
- `Assets/Scripts/InstructionClassifier.cs` — `IsMemoryOp()` / `IsStoreOp()`, string-prefix
  classifiers on `InstructionData.label` used to decide whether ADDR/RD should be
  populated for a given instruction.

## Reproduction steps

1. Load `level_01.json` (single instruction: `addi x1, x2, 1`).
2. Pick up the brick at `StartPlatform`, carry it through Fetch → Decode → Execute →
   Memory, pressing T at each station per normal tick flow.
3. Observe the Instruction Monitor's RD row value and the T-press at which it first
   becomes non-blank.