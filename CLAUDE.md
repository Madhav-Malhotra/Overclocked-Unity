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

The game has a working core loop: a JSON-defined level (`sceneName` field) spawns instruction bricks at the `StartPlatform`, and loads one of two game scenes depending on architecture. In **FiveStage**, the player carries bricks into `CPUStation`s representing pipeline stages and presses T to tick; a `CPUController`-driven `ICPU` (backed by the Verilator-generated `design_wrapper` native plugin) advances the real RV32I pipeline in lockstep, validated each tick by `PipelineValidator`. In **Blackbox**, there are no per-stage stations — a single `BlackboxStation` places the brick and auto-advances the same `CPUController` in a coroutine until the instruction retires. Both scenes feed the instruction monitor UI through the shared `InstructionMonitorCapture` helper, and both signal level completion through `InstructionBrick.IsProcessed`, checked by `EndPlatform`. See `unity/.claude/status.md` for the current file-by-file map of what lives where.

### Scripts (`Assets/`)

All gameplay scripts are plain C# `MonoBehaviour` classes — no custom base classes or managers yet, aside from the `LevelManager` singleton.

### Input

`InputSystem_Actions.inputactions` exists but only the `Player` action map is enabled at runtime (via `InputActionsInitializer`). Movement uses `Keyboard.current` directly; interaction uses `PlayerInput`/`InputAction` from the asset. New inputs should use the `Player` action map and be accessed via `PlayerInput` component.

### Key Packages

- `com.unity.inputsystem` 1.18.0 — Input System
- `com.unity.render-pipelines.universal` 17.3.0 — URP
- `com.unity.ai.navigation` — NavMesh support available
- `com.unity.textmeshpro` — Used for timer and UI labels

## Transparency

These rules exist so the user can verify Claude's work as it happens, not just trust a summary after the fact.

- **Never launch subagents (the `Agent` tool) in this project.** Subagents run out of the user's view, which defeats the ability to check whether Claude is on the right track. Do all research, planning, and implementation directly in the main conversation.
- **Always cite file name and line number when describing anything that currently exists in the codebase** — while planning, debugging, or summarising what was implemented (e.g. `CPUStation.cs:47`, not "the interact handler"). This applies to root causes, existing behaviour being changed, and code just written.
- **Prefer plain `Read`/`Edit`/`Write` over Unity MCP script tools** for `.cs` files (see Unity MCP Tools below) so changes are visible as normal file diffs.
- **Play Mode verification is the user's job, not Claude's** (see Unity MCP Tools below) — Claude hands off with specific steps rather than claiming success unverified.

## Unity-Specific Conventions

- **Never rename or move assets outside the Unity Editor** — always use the Project window to keep `.meta` files in sync.
- `.unity`, `.prefab`, and `.asset` files use Unity YAML Merge (`unityyamlmerge`) — avoid manual text-editor merges on these files.
- Binary assets (textures, audio, models) are tracked via Git LFS.
- The `Library/` folder is local Unity cache and is gitignored — it is rebuilt on first open after cloning.
- `Assets/CPUWrapper/CPU.cs`, `VerilatorClient.cs`, and `FPGAClient.cs` are auto-generated from `bridge/` via `make sync-unity` — never hand-edit them; edit the `bridge/` source and re-sync (see `bridge/README.md`). Unity-only logic lives in the hand-maintained `CPUUnityExtensions.cs` alongside them.
- When setting coordinates, keep in mind that the origin (0,0) is the top left of the screen in 2D mode. Increasing X moves right, increasing Y moves down.

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
- **Prefer plain `Read`/`Edit`/`Write` over Unity MCP script tools for reading and editing `.cs` files**, so the user can monitor changes through normal file diffs. Only use `Unity_CreateScript` for genuinely new files (so the `.meta` gets created), and `Unity_ValidateScript` afterward if you want to confirm Unity picked up the change — don't use `Unity_ManageScript`/`Unity_ScriptApplyEdits` for routine reads/writes.
- `Unity_CreateScript` creates an empty file with a `.meta` — then use plain `Write` to fill in the content. Do not create `.cs` files with the `Write` tool if the Editor is open and you skip `Unity_CreateScript`, as this can cause `.meta` desync.
- Always call `Unity_ValidateScript` before writing a script that touches existing MonoBehaviour fields; a compile error can break Enter Play Mode for all scripts.
- After any structural scene change (adding/removing components, changing serialized references), save the scene explicitly with `Unity_ManageScene` or `Unity_RunCommand` → `File/Save`.
- **Play Mode verification is the user's job, not Claude's.** Once a change is applied and compiles cleanly, hand Play Mode over to the user with specific instructions on what to do and what to look for/verify (e.g. "Enter Play Mode, walk to the Start platform, pick up the brick, place it on Fetch — expect no console errors and the brick to show stage-1 material"). Only enter Play Mode or take a screenshot yourself for a quick look at UI/frontend state, not to drive gameplay or exercise player-controlled interactions — the user has controls the agent doesn't.
- **Recompile disconnects:** Writing a script triggers Unity recompilation which drops the MCP connection. If the next MCP call fails with "Unity not detected", wait ~8 seconds and retry once.
- **Stuck on the same error:** If the same MCP tool call fails 3 times in a row, stop and ask the user to perform the step manually instead of continuing to retry.
- **Scene object references:** `Unity_ManageGameObject` cannot reliably set `UnityEngine.Object` reference fields via `{"find": ..., "method": ...}` syntax (JSON deserialisation bug). After 2 failed attempts, mark as **[USER ACTION REQUIRED]** and move on.

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
