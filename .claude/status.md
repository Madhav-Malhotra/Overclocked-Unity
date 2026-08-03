# Project Status

**Player** (`Assets/Player/`): `PlayerController.cs` (Rigidbody WASD movement), `InteractableDetector.cs` (OverlapSphere highlight), `PlayerInteractionHandler.cs` (E key interact), `InstructionBrickHoldingSystem.cs` (carry system), `InputActionsInitializer.cs`, `PlayerMovementAudio.cs`.

**Interactables** (`Assets/Interactables/`): `Interactable.cs` (URP emission highlight base), `Table.cs` + `TableProcessingTimer.cs` (processing stations), `CPUStation.cs` + `PipelineStage.cs` (CPU pipeline components), `InstructionBrick.cs` (data item players carry), `StartPlatform.cs` (spawns bricks from the current level's instruction queue), `BrickMeshBuilder.cs` (procedural brick mesh), `CPUStationScreenSpaceOutlineRendererFeature.cs` (URP outline pass); 3D models in `Models/` (ALU, Decoder, Memory, Multiplexer, Regfile, Pedestal).

**CPUWrapper** (`Assets/CPUWrapper/`): `CPU.cs`/`VerilatorClient.cs`/`FPGAClient.cs` are AUTO-GENERATED from `bridge/` via `make sync-unity` — never hand-edit them, edit the `bridge/` source and re-sync (see `bridge/README.md`). `CPUUnityExtensions.cs` is the hand-maintained, non-generated companion for Unity-only needs (e.g. `InstructionData[]` adaptation). `CPUController.cs` (owns the `ICPU` instance via `CPUUnityExtensions.Create`, re-inits IMEM per level) drives the simulated CPU; `PipelineValidator.cs` cross-checks each tick's pre/post `CPUState` against expected pipeline-stage occupancy (used to catch validator/tick-model bugs, see `bugs.todo`).

**Levels** (`Assets/Levels/`): levels are JSON files auto-discovered from `Assets/Levels/Resources/JSON/level_NN.json` via `LevelManager.cs` (`Assets/Scripts/`, also owns `LevelData.cs`/`LevelTransferData.cs`); `add_hex.py` (`make hex`) assembles each instruction's RISC-V `label` into a `hex` field with `riscv64-unknown-elf-as`/`objcopy` — see `Assets/Levels/README.md` for the new-level workflow.

**UI** (`Assets/UI/`): migrated to UI Toolkit on the vendored [unity-ui-toolkit-design-system](https://github.com/sinanata/unity-ui-toolkit-design-system) (`Assets/DesignSystem/`, see `ui-migration.md`). `Assets/UI/HUD/`: `HUD.uxml` (one shared tree — timer/progress badges top-left, toast bottom-left, prompt bottom-right), `GameHUD.cs`, `InteractionUIManager.cs`, `TickFeedbackUI.cs`, `TickButtonHandler.cs`. `Assets/UI/EndScreen/`: `EndScreenUI.cs` + `EndScreen.uxml` (success/failure overlay, `EndScreen.unity`). `Assets/UI/MainMenu/`: main menu screen. `Assets/UI/Shared/`: shared `PanelSettings`/theme assets. Font: Poppins (`Assets/Resources/DsFonts/Poppins/`).

**Scripts** (`Assets/Scripts/`): `CircuitTrace.cs` + `CircuitTraceSpawner.cs` (decorative circuit line visuals), `LevelManager.cs` / `LevelData.cs` / `LevelTransferData.cs` (level loading, see Levels above).

**Scenes** (`Assets/Scenes/`): game scenes; **Prefabs** (`Assets/Prefabs/`): reusable prefabs; **Settings** (`Assets/Settings/`): URP renderer/pipeline assets.

**FPGA backend (outside `unity/`)**: `bridge/` (top-level) provides `ICPU`/`CPUFactory` so `CPU.cs`-equivalent code can target either the Verilator plugin or a PYNQ-Z2 FPGA via `webserver/`'s REST API — see `bridge/README.md`'s `make sync-unity` for how this reaches Unity. Unity currently only ever constructs `CPUFactory.ImplementationType.Verilator`; the FPGA path is unwired in gameplay code.
