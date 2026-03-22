# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Overclocked** is a Unity 6 game project (version `6000.3.6f1`) using the Universal Render Pipeline (URP). The game is meant to be like Overcooked - a time management game where players manage tasks going on inside a CPU. The aim of the game is to eventually be a fun way to learn about computer architecture, with players interacting with different CPU components (e.g. instruction memory, register file, ALU, etc.), moving data through the pipeline to understand how a CPU works.

## Setup Requirements

- **Unity Hub** with Unity Editor `6000.3.6f1` installed
- **Git LFS** must be initialized before cloning (`git lfs install`), then `git lfs pull` after cloning
- **Unity SmartMerge** must be configured locally for scene/prefab merge conflict resolution (see README.md for platform-specific commands)
- This repo may be a git submodule — run `git submodule update --init --recursive` from the parent repo if needed

## Current Architecture

The project is currently just a rudimentary player controller and interactable boxes. It needs to be developed into a full game.

### Scripts (`Assets/`)

All gameplay scripts are plain C# `MonoBehaviour` classes — no custom base classes or managers yet.

**Player system** (`Assets/Player/`):
- `PlayerController.cs` — Rigidbody-based movement using raw `Keyboard.current` polling (WASD). Applies velocity in `FixedUpdate`, handles rotation via `Quaternion.Slerp` in `Update`. Exposes `StopMovement()` for UI freeze.
- `InteractableDetector.cs` — Each `Update`, casts `Physics.OverlapSphere` within `interactionRadius`, filters candidates by a forward-facing dot product threshold (`detectionAngle`), and highlights the nearest qualifying `Interactable`. Exposes `GetCurrentHighlighted()`.
- `PlayerInteractionHandler.cs` — Listens for the `Interact` input action (E key / gamepad Y). On press, calls `OnInteract()` on whatever `InteractableDetector` currently highlights. Also drives `InteractionUIManager` prompt visibility each frame.
- `DiskHoldingSystem.cs` — Manages the single disk the player can carry. `PickUpDisk` parents the disk to a `holdPosition` transform and scales it down; `PlaceDisk(Table, float)` re-parents to the table's `diskSlot` and hands off to `Table.StartProcessing`.
- `InputActionsInitializer.cs` — On `Start`, disables all action maps then enables only the `Player` map from `InputSystem.actions`.

**Interactable system** (`Assets/Interactables/`):
- `Interactable.cs` — Base class. Requires a `Renderer`. Uses URP emission (`_EmissionColor` / `_EMISSION` keyword) to highlight objects. Virtual API: `CanBeHighlighted()`, `CanInteract()`, `OnInteract()`, `SetHighlighted(bool)`.
- `Disk.cs` — Extends `Interactable`. Always returns `false` from `CanBeHighlighted()` and `CanInteract()` — the player never interacts with disks directly. Provides `EnablePhysics(bool)` and `SetHighlightColor(Color)` for use by `Table` and `DiskHoldingSystem`.
- `Table.cs` — Extends `Interactable`. Manages pick-up/place logic by querying `DiskHoldingSystem`. On place, opens `TimerSelectionUI` to choose a processing duration, then calls `StartProcessing(float)` which spawns a `TableProcessingTimer` prefab and sets a red/orange highlight. Interaction is blocked while processing.
- `TableProcessingTimer.cs` — World-space billboard UI (fill bar + text) that counts down above a table. Destroyed by `Table` when processing completes.

**UI system** (`Assets/UI/`):
- `InteractionUIManager.cs` — HUD prompt ("E - Pick Up" / "E - Place") shown via `CanvasGroup` alpha. Updated every frame by `PlayerInteractionHandler`.
- `TimerSelectionUI.cs` — Modal popup with 1s / 3s / 5s / 10s buttons. Freezes player (`PlayerController.enabled = false` + `PlayerInput.DeactivateInput()`) while open; ESC cancels. Invokes a callback with the selected duration.

### Input

`InputSystem_Actions.inputactions` exists but only the `Player` action map is enabled at runtime (via `InputActionsInitializer`). Movement uses `Keyboard.current` directly; interaction uses `PlayerInput`/`InputAction` from the asset. New inputs should use the `Player` action map and be accessed via `PlayerInput` component.

### Key Packages

- `com.unity.inputsystem` 1.18.0 — Input System
- `com.unity.render-pipelines.universal` 17.3.0 — URP
- `com.unity.ai.navigation` — NavMesh support available
- `com.unity.textmeshpro` — Used for timer and UI labels

## Unity-Specific Conventions

- **Never rename or move assets outside the Unity Editor** — always use the Project window to keep `.meta` files in sync.
- `.unity`, `.prefab`, and `.asset` files use Unity YAML Merge (`unityyamlmerge`) — avoid manual text-editor merges on these files.
- Binary assets (textures, audio, models) are tracked via Git LFS.
- The `Library/` folder is local Unity cache and is gitignored — it is rebuilt on first open after cloning.
