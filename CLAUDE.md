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

## Unity MCP Tools

A Unity MCP server is connected and available. Prefer MCP tools over manual file edits for anything that touches the live Unity Editor state. Key tools and when to use them:

| Tool | Use for |
|---|---|
| `Unity_ManageScript` | Read, create, or overwrite C# script files |
| `Unity_CreateScript` / `Unity_DeleteScript` | Create/delete scripts via the Editor (ensures `.meta` files are created) |
| `Unity_ValidateScript` | Check a script compiles before applying — always run this before `Unity_ManageScript` writes |
| `Unity_ScriptApplyEdits` / `Unity_ApplyTextEdits` | Apply targeted diffs to scripts |
| `Unity_ManageGameObject` | Add/remove/configure GameObjects and their components in a scene |
| `Unity_ManageScene` | Open, save, or query the active scene |
| `Unity_ManageAsset` | Create, move, or delete assets (respects `.meta` files) |
| `Unity_FindProjectAssets` | Search the Project for assets by name or type |
| `Unity_GetConsoleLogs` / `Unity_ReadConsole` | Read Unity Editor console for errors/warnings after a change |
| `Unity_Camera_Capture` / `Unity_SceneView_Capture2DScene` | Take a screenshot of Game or Scene view to visually verify a change |
| `Unity_ManageEditor` | Enter/exit Play mode, trigger recompilation |
| `Unity_RunCommand` | Run arbitrary Unity Editor menu commands |
| `Unity_FindInFile` | Search file contents without leaving Unity context |

**Important constraints:**
- `Unity_CreateScript` creates an empty file with a `.meta` — then use `Unity_ManageScript` to write the content. Do not create `.cs` files with the `Write` tool if the Editor is open, as this can cause `.meta` desync.
- Always call `Unity_ValidateScript` before writing a script that touches existing MonoBehaviour fields; a compile error can break Enter Play Mode for all scripts.
- After any structural scene change (adding/removing components, changing serialized references), save the scene explicitly with `Unity_ManageScene` or `Unity_RunCommand` → `File/Save`.

## Implementation Loop

When making changes that could break existing behaviour, follow this loop. The `/implement` skill (`sk-implement`) codifies this as a runnable workflow.

### Step 1 — Plan

1. Read all scripts and scene state that the change touches (`Unity_ManageScript`, `Unity_FindProjectAssets`, `Unity_ManageScene`).
2. Write out a concise, numbered plan of every change to be made (files, components, scene wiring).
3. **Present the plan to the user and wait for explicit confirmation before writing a single line of code.**
   - Flag any step that removes or renames a serialized field (this breaks prefab/scene references silently).
   - Flag any step that changes a public API used by other scripts.

### Step 2 — Implement

- Apply changes one logical unit at a time (one script or one scene change), not all at once.
- For script changes: `Unity_ValidateScript` → if clean, apply with `Unity_ScriptApplyEdits` or `Unity_ManageScript`.
- For scene/prefab wiring: use `Unity_ManageGameObject` rather than hand-editing YAML.
- If a step requires an action only possible inside the Unity Editor UI (e.g. baking NavMesh, configuring a RenderFeature asset via Inspector sliders), **stop and ask the user** to perform that step, then confirm when done before continuing.

### Step 3 — Verify

After each logical unit of change:

1. `Unity_GetConsoleLogs` — confirm zero new errors or warnings introduced by the change.
2. `Unity_ManageEditor` → Enter Play Mode → `Unity_Camera_Capture` or `Unity_SceneView_Capture2DScene` — visually confirm the intended behaviour.
3. Exit Play Mode. If the console has new errors, **fix before moving to the next change**. Do not accumulate broken state.

### Step 4 — Continue or summarise

- If more changes remain in the plan, return to Step 2.
- When all changes are done, summarise to the user:
  - What was changed and why.
  - Any remaining manual steps the user must do in the Editor.
  - Any non-obvious risks or follow-up tasks flagged during implementation.
