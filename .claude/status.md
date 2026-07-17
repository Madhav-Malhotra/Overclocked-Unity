# Project Status

**Player** (`Assets/Player/`): `PlayerController.cs` (Rigidbody WASD movement), `InteractableDetector.cs` (OverlapSphere highlight), `PlayerInteractionHandler.cs` (E key interact), `InstructionBrickHoldingSystem.cs` (carry system), `InputActionsInitializer.cs`, `PlayerMovementAudio.cs`.

**Interactables** (`Assets/Interactables/`): `Interactable.cs` (URP emission highlight base), `Table.cs` + `TableProcessingTimer.cs` (processing stations), `CPUStation.cs` + `PipelineStage.cs` (CPU pipeline components), `InstructionBrick.cs` (data item players carry), `StartPlatform.cs` (spawns bricks from the current level's instruction queue), `BrickMeshBuilder.cs` (procedural brick mesh), `CPUStationScreenSpaceOutlineRendererFeature.cs` (URP outline pass); 3D models in `Models/` (ALU, Decoder, Memory, Multiplexer, Regfile, Pedestal).

**CPUWrapper** (`Assets/CPUWrapper/`): `CPU.cs` (wraps the native Verilator plugin, `design_wrapper.dll`) + `CPUController.cs` (owns the `CPU` instance, re-inits IMEM per level) drive the simulated CPU; `PipelineValidator.cs` cross-checks each tick's pre/post `CPUState` against expected pipeline-stage occupancy (used to catch validator/tick-model bugs, see `bugs.todo`).

**Levels** (`Assets/Levels/`): levels are JSON files auto-discovered from `Assets/Levels/Resources/JSON/level_NN.json` via `LevelManager.cs` (`Assets/Scripts/`, also owns `LevelData.cs`/`LevelTransferData.cs`); `add_hex.py` (`make hex`) assembles each instruction's RISC-V `label` into a `hex` field with `riscv64-unknown-elf-as`/`objcopy` — see `Assets/Levels/README.md` for the new-level workflow.

**UI** (`Assets/UI/`): `InteractionUIManager.cs` (HUD prompt), `GameHUD.cs` (top-left countdown + m/n progress badges), `EndScreenUI.cs` (success/failure overlay), `TickButtonHandler.cs` + `TickFeedbackUI.cs` (manual tick-advance button and feedback); `Assets/UI/RoundedBadge.png` (sliced sprite for badge backgrounds, border=26); font: `Assets/Fonts/Fredoka new.asset`.

**Scripts** (`Assets/Scripts/`): `CircuitTrace.cs` + `CircuitTraceSpawner.cs` (decorative circuit line visuals), `LevelManager.cs` / `LevelData.cs` / `LevelTransferData.cs` (level loading, see Levels above).

**Scenes** (`Assets/Scenes/`): game scenes; **Prefabs** (`Assets/Prefabs/`): reusable prefabs; **Settings** (`Assets/Settings/`): URP renderer/pipeline assets.

**FPGA backend (outside `unity/`)**: `bridge/` (top-level) provides `ICPU`/`CPUFactory` so `CPU.cs`-equivalent code can target either the Verilator plugin or a PYNQ-Z2 FPGA via `webserver/`'s REST API — copy `bridge/CPU.cs`, `VerilatorClient.cs`, `FPGAClient.cs` into Unity's Assets to switch backends.
