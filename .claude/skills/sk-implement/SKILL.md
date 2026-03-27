---
name: sk-implement
description: Safe implementation loop for Unity changes that could break existing behaviour. Covers planning, MCP-driven implementation, verification via console logs and screenshots, and summary.
---

When invoked, execute the four-phase loop below for every implementation task in this Unity project. Do not skip or compress phases — the verification step exists precisely because silent breakage is common in Unity (missing references, serialized field renames, shader/URP mismatches).

---

## Phase 1 — Plan

1. Gather context using MCP tools before writing anything:
   - Read every script the change will touch (`Unity_ManageScript` or `Unity_FindInFile`).
   - Identify all scene GameObjects or prefabs that reference those scripts (`Unity_FindProjectAssets`, `Unity_ManageScene`).
   - Note every serialized field (`[SerializeField]` or `public`) that will be added, removed, or renamed — these break prefab/scene references silently if changed without updating all usages.

2. Write a numbered plan:
   - List each file or asset that changes, and what the change is.
   - Mark steps that need Editor-only actions (NavMesh bake, Inspector slider, RenderFeature config) with **[USER ACTION REQUIRED]**.
   - Mark steps that change a public API or serialized field with **[BREAKING RISK]**.

3. **Present the plan to the user. Do not proceed until they confirm.**
   - If the user modifies the plan, update your list before moving to Phase 2.

---

## Phase 2 — Implement

Work through the confirmed plan one logical unit at a time. Never batch multiple unrelated changes into a single apply.

**For C# script changes:**
1. Draft the new content.
2. Run `Unity_ValidateScript` with the draft. If it reports errors, fix them before proceeding.
3. Apply with `Unity_ScriptApplyEdits` (preferred for targeted edits) or `Unity_ManageScript` (for full rewrites).
4. If the change renames or removes a serialized field, immediately update every prefab/scene that referenced it using `Unity_ManageGameObject` or ask the user to rewire in the Inspector.

**For scene/prefab changes:**
- Use `Unity_ManageGameObject` to add, remove, or configure components.
- Use `Unity_ManageScene` → save after structural changes.
- Never hand-edit `.unity` or `.prefab` YAML — use MCP tools or ask the user to make the change in the Editor.

**For steps marked [USER ACTION REQUIRED]:**
- Describe exactly what the user must do in the Editor (panel name, menu path, field name).
- Wait for the user to confirm they have completed the step before continuing.

---

## Phase 3 — Verify

Run these checks after **each logical unit** — not just at the end.

1. **Console check:** `Unity_GetConsoleLogs` — scan for new errors or warnings.
   - If any new errors appear, fix them now. Do not continue to the next change with a broken compile state.
   - Warnings about missing references or null components count as blockers; fix or explicitly acknowledge them.

2. **Visual check:** Enter Play Mode (`Unity_ManageEditor`), then capture:
   - `Unity_Camera_Capture` for runtime Game view.
   - `Unity_SceneView_Capture2DScene` or `Unity_SceneView_CaptureMultiAngleSceneView` for scene layout.
   - Confirm the screenshot matches the intended behaviour. If it does not, return to Phase 2 for that unit.

3. Exit Play Mode before proceeding to the next change.

---

## Phase 4 — Summarise

When all planned changes are verified:

1. List every change made (file, what changed, why).
2. List any **[USER ACTION REQUIRED]** steps that still need to be done manually in the Editor.
3. List any **[BREAKING RISK]** items and confirm they were resolved or explicitly deferred.
4. Note any non-obvious follow-up work (e.g. "the new serialized field `stationId` needs to be set on each CPUStation prefab instance in the scene").

---

## Quick-reference: which MCP tool for what

| Need | Tool |
|---|---|
| Read a script | `Unity_ManageScript` (action: read) |
| Write/overwrite a script | `Unity_ValidateScript` then `Unity_ManageScript` |
| Targeted script diff | `Unity_ScriptApplyEdits` / `Unity_ApplyTextEdits` |
| Create a new script file | `Unity_CreateScript` (creates with `.meta`), then write content |
| Delete a script | `Unity_DeleteScript` |
| Find assets by name/type | `Unity_FindProjectAssets` |
| Add/remove component on GameObject | `Unity_ManageGameObject` |
| Open/save scene | `Unity_ManageScene` |
| Enter/exit Play Mode | `Unity_ManageEditor` |
| Read console logs | `Unity_GetConsoleLogs` / `Unity_ReadConsole` |
| Screenshot (Game view) | `Unity_Camera_Capture` |
| Screenshot (Scene view) | `Unity_SceneView_Capture2DScene` |
| Run a menu command | `Unity_RunCommand` |
