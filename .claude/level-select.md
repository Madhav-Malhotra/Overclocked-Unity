# Level System Implementation Plan

## Overview

Convert the single-instruction demo into a full multi-level game with:
- JSON-configured levels (instructions list, time limit)
- Start platform that spawns the next instruction brick when the current one is removed
- End platform (Writeback output) where completed bricks are deposited, incrementing a progress counter
- HUD: top-left countdown timer + `m/n` instruction progress
- End-of-level screen: success (all done) or failure (time ran out), with replay / next-level buttons
- Remove the `TimerSelectionUI` duration picker; all stations process at 1s by default

---

## Files to Create

### 1. `Assets/Scripts/LevelData.cs` (new)
Serializable data classes for JSON deserialization:
- `LevelData` — holds `string levelName`, `float timeLimit`, `InstructionData[] instructions`
- `InstructionData` — holds whatever per-instruction fields are needed (e.g. `string id`, `string label`; expandable later)

### 2. `Assets/Scripts/LevelManager.cs` (new, MonoBehaviour singleton)
Central coordinator for one play session:
- `[SerializeField] TextAsset[] levelJsonFiles` — ordered array of level JSON files assigned in Inspector
- `LoadLevel(int index)` — parses the JSON for that level, stores the instruction queue, resets all runtime state, starts the countdown
- `GetNextInstruction() → InstructionData` — pops the next instruction from the queue; returns null when exhausted
- `OnInstructionCompleted()` — increments completed count, checks win condition, notifies `GameHUD`
- `Update()` — counts down the timer; calls `OnTimeLimitReached()` when it hits zero
- `OnTimeLimitReached()` — triggers failure end screen
- `OnLevelSuccess()` — triggers success end screen
- Exposes `CurrentLevelIndex`, `TimeRemaining`, `CompletedCount`, `TotalCount` for HUD polling

### 3. `Assets/UI/GameHUD.cs` (new, MonoBehaviour)
Top-left HUD panel:
- `[SerializeField] TextMeshProUGUI timerText` — displays countdown as `MM:SS` or raw seconds
- `[SerializeField] TextMeshProUGUI progressText` — displays `m/n`
- `Update()` — polls `LevelManager` for `TimeRemaining` and progress each frame, updates both labels
- `SetVisible(bool)` — hides/shows the HUD panel (used on end screen)

### 4. `Assets/UI/EndScreenUI.cs` (new, MonoBehaviour)
End-of-level overlay:
- `[SerializeField] GameObject successPanel`, `failurePanel`
- `[SerializeField] Button replayButton`, `nextLevelButton` (next only shown on success)
- `ShowSuccess()` / `ShowFailure()` — activates correct panel, freezes player, shows cursor
- Replay button → calls `LevelManager.LoadLevel(currentIndex)`
- Next Level button → calls `LevelManager.LoadLevel(currentIndex + 1)`
- `Hide()` — deactivates panels, unfreezes player

### 5. `Assets/Levels/level_01.json` (new data file, example)
```json
{
  "levelName": "Level 1",
  "timeLimit": 120,
  "instructions": [
    { "id": "ADD R1 R2 R3" },
    { "id": "SUB R4 R5 R6" },
    { "id": "LW R7 0(R8)" }
  ]
}
```

---

## Files to Modify

### 6. `Assets/Interactables/Table.cs`
- Add `virtual void OnBrickRemoved()` — called from `RemoveBrick()` so subclasses can react

### 7. `Assets/Interactables/CPUStation.cs`
- **Remove** the `timerSelectionUI` serialized field and all `TimerSelectionUI` references **[BREAKING RISK — serialized field removed]**
- In `OnInteract()` when placing: skip the timer popup, call `StartProcessing(1f)` directly
- The `processingTimerPrefab` world-space billboard can stay (shows the 1s countdown above the station)

### 8. `Assets/Interactables/` — new `StartPlatform.cs` (extends `Table`)
New script for the Start platform:
- `OnBrickRemoved()` override — asks `LevelManager.GetNextInstruction()`; if one exists, instantiates a new `InstructionBrick` prefab (from a `[SerializeField] GameObject instructionBrickPrefab`) and places it on this table; if none remain, leaves the table empty
- Does NOT allow the player to pick up a brick while one is being processed (same as current `Table` behaviour)

### 9. `Assets/Interactables/` — new `EndPlatform.cs` (extends `Table`)
New script for the End platform (placed after Writeback in the scene):
- `CanInteract()` override — only accepts a brick whose `CurrentStage == PipelineStage.Writeback`; never allows pickup (one-way deposit)
- `PlaceBrick()` override — calls `base.PlaceBrick()`, then immediately calls `LevelManager.OnInstructionCompleted()`, then destroys the deposited brick after a brief visual confirmation delay (0.5s coroutine)
- Displays a "deposit" highlight colour different from normal stations

### 10. `Assets/UI/TimerSelectionUI.cs`
- **Delete** (or disable in scene). The entire duration-picker modal is removed since all stations use 1s.
- **[BREAKING RISK — GameObject in scene and serialized field references in CPUStation will need cleanup]**

---

## Scene Changes Required (`Playground.unity`)

### A. Remove `TimerSelectionUI` GameObject from scene **[USER ACTION REQUIRED]**

### B. Add `End` platform GameObject after `Writeback` station **[USER ACTION REQUIRED]**
- Duplicate the `Start` GameObject structure (table mesh + `InstructionBrickSlot`)
- Attach `EndPlatform` component instead of `Table`

### C. Update `Start` GameObject **[USER ACTION REQUIRED]**
- Replace the `Table` component with `StartPlatform`
- Assign `instructionBrickPrefab` in Inspector (drag the existing `InstructionBrick` prefab)
- Remove the pre-placed `InstructionBrick` child — `StartPlatform` will spawn the first brick from `LevelManager` on load

### D. Add Canvas UI for GameHUD **[USER ACTION REQUIRED]**
- In the existing `Canvas`, add a child panel anchored top-left
- Add two `TextMeshProUGUI` children: one for timer, one for progress
- Attach `GameHUD` component and wire the text references

### E. Add Canvas UI for EndScreen **[USER ACTION REQUIRED]**
- Add a full-screen panel to `Canvas` (starts inactive)
- Add success/failure sub-panels each with label text and buttons
- Attach `EndScreenUI` component and wire all references

### F. Add `LevelManager` GameObject to scene **[USER ACTION REQUIRED]**
- Create empty GameObject named `LevelManager`
- Attach `LevelManager` component
- Assign the ordered `levelJsonFiles` array with `level_01.json` (and future levels)

### G. Remove `timerSelectionUI` reference from each CPUStation Inspector slot **[USER ACTION REQUIRED]**
- After the script change, Unity will null-clear this automatically on recompile, but verify no missing-reference warnings remain

---

## Execution Order Summary

1. Create `LevelData.cs` data classes
2. Create `LevelManager.cs`
3. Modify `Table.cs` to add `OnBrickRemoved()` hook
4. Modify `CPUStation.cs` to remove timer popup, hardcode 1s processing
5. Create `StartPlatform.cs`
6. Create `EndPlatform.cs`
7. Create `GameHUD.cs`
8. Create `EndScreenUI.cs`
9. Create `Assets/Levels/level_01.json`
10. **[USER ACTION REQUIRED]** Scene wiring: remove old UI, add new platforms, add new HUD/EndScreen canvas objects, add LevelManager, wire all Inspector refs
11. Delete `TimerSelectionUI.cs` after scene cleanup is confirmed
