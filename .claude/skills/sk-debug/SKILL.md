---
name: sk-debug
description: Structured debugging loop for Unity bugs. Covers systematic diagnosis (console logs, targeted prints, reproduction steps), root cause identification with file/line citations, architectural reflection before patching, option presentation with trade-offs, and verified fix confirmation.
---

When invoked, execute the five-phase loop below for every bug in this Unity project. Do not jump straight to a fix — the diagnosis phases exist because Unity bugs are frequently caused by serialized field wiring, execution order, or missing references that are invisible without targeted investigation.

---

## Phase 1 — Reproduce and Locate

Before forming any hypothesis, establish ground truth.

1. **Read the console first.** Use `Unity_GetConsoleLogs` / `Unity_ReadConsole`. Record:
   - The exact error message and stack trace (file name + line number).
   - Whether it is a compile error, a runtime exception, or a silent misbehaviour (wrong value, missing object, wrong timing).

2. **Read every script implicated by the stack trace.** Use `Unity_ManageScript` (action: read) or `Unity_FindInFile`. For each file:
   - Note the exact line number where the error originates.
   - Note the call chain one or two levels up from that line.

3. **Reproduce the bug explicitly.** If the error is not already visible in the console, give the user specific reproduction steps:
   > **[USER REPRODUCTION REQUIRED]** Enter Play Mode. Do X, then Y, then Z. Tell me what you see (error panel, wrong behaviour, freeze, etc.).

   Do not theorise about causes until you have a confirmed reproduction.

4. **State what you know vs. what you don't know.** Write a short list:
   - Confirmed facts (e.g. "NullReferenceException at `CPUStation.cs:47` inside `OnInteract()`").
   - Open questions (e.g. "Unknown whether `_cpu` reference is null at the time of the call or only later").

---

## Phase 2 — Diagnose with Targeted Logs

Do not guess at root cause. Add the minimum logging needed to resolve each open question, then observe.

1. **For each open question, identify exactly what value or event would answer it.** For example:
   - "Is `_cpu` null? → log `Debug.Log($"_cpu is {_cpu}")` in `Start()`."
   - "Is `OnInteract()` called before `Start()` finishes? → log timestamps or a boolean guard."

2. **Insert targeted `Debug.Log` or `Debug.LogError` statements** using `Unity_ScriptApplyEdits`. Place them:
   - At the earliest point that can confirm or rule out each hypothesis.
   - With enough context in the message (object name, value, frame count) to be unambiguous.

3. **Enter Play Mode and reproduce the bug.** Instruct the user if manual steps are required:
   > **[USER ACTION REQUIRED]** Enter Play Mode, do X, then open the Console window and paste the log output here.

4. **Read the output with `Unity_GetConsoleLogs`.** Update your confirmed facts / open questions list. Repeat this phase if new questions surface — do not move to Phase 3 until root cause is identified with a specific file name and line number.

5. **Remove all diagnostic logs before fixing** — they are noise in the final code.

---

## Phase 3 — Reflect Before Fixing

Before writing a fix, stop and consider whether a patch is the right response.

Answer these questions explicitly:

1. **Is this a symptom or the root cause?** Fixing a NullReferenceException by adding a null guard may silence the error while leaving the underlying wiring problem intact. Name the actual root cause.

2. **Does this bug reveal an architectural issue?** For example:
   - A frequently null reference might indicate that initialization order is wrong, not just that one null check is missing.
   - A timing bug might indicate that two systems are coupled through a shared mutable field instead of via events or a proper interface.
   - A missing reference might indicate that a GameObject should be found at runtime via tag/type, not wired through the Inspector.

3. **What are the fix options?** List at least two options. For each, note:
   - What it changes (file, line, pattern).
   - **Pros:** what it fixes, what it simplifies, what future bugs it prevents.
   - **Cons:** scope of change, risk of breaking other things, technical debt introduced.

   Use this format:

   > **Option A — [short name]**
   > Changes: `CPUStation.cs:47` — add null guard.
   > Pros: minimal change, safe.
   > Cons: masks the real wiring problem; the null reference will resurface elsewhere.
   >
   > **Option B — [short name]**
   > Changes: remove Inspector-wired reference; find `CPU` at runtime via `FindObjectOfType<CPU>()` in `Awake()`.
   > Pros: eliminates the entire class of "forgot to wire in Inspector" bugs for this field.
   > Cons: `FindObjectOfType` is slow if called frequently; only acceptable in `Awake`.

4. **Recommend one option and explain why**, but do not implement it yet.

5. **Present options to the user and wait for their choice** before proceeding to Phase 4.
   - If the user picks a different option than your recommendation, acknowledge and reason about the pros and cons of their suggestion as well
   - If the choice is non-obvious or the user seems uncertain, note the key trade-off in one sentence and ask which constraint matters more to them.

---

## Phase 4 — Fix

Implement only the option the user confirmed.

**For C# script changes:**
1. Draft the fix.
2. Run `Unity_ValidateScript`. Fix any compile errors before applying.
3. Apply with `Unity_ScriptApplyEdits` (preferred for targeted edits) or `Unity_ManageScript` (for full rewrites).
4. If the fix renames or removes a serialized field, immediately update every prefab/scene that referenced it, or flag it as **[USER ACTION REQUIRED]** with Inspector instructions.

**For scene/prefab wiring changes:**
- Use `Unity_ManageGameObject` to set references or add/remove components.
- Save the scene with `Unity_ManageScene` or `Unity_RunCommand` → `File/Save`.
- Never hand-edit `.unity` or `.prefab` YAML.

**For steps only possible in the Unity Editor UI:**
> **[USER ACTION REQUIRED]** Open the Inspector for [GameObject]. Set [field] to [value]. Tell me when done.

---

## Phase 5 — Verify the Fix

Do not close the loop until you have confirmed the bug is gone and nothing else broke.

1. **Console check:** `Unity_GetConsoleLogs` — confirm the original error is gone and no new errors appeared.

2. **Visual / behavioural check:** Enter Play Mode (`Unity_ManageEditor`), reproduce the exact steps that originally triggered the bug, capture with `Unity_Camera_Capture` or `Unity_SceneView_Capture2DScene`. Confirm correct behaviour.

3. **Regression check:** Briefly exercise any system the fix touched to confirm no adjacent behaviour broke.

4. Exit Play Mode.

5. **Summarise to the user:**
   - Root cause (file, line, why it was wrong).
   - What was changed and why that option was chosen.
   - Any **[USER ACTION REQUIRED]** steps still outstanding.
   - Any follow-up risks (e.g. "this fix works but the same pattern appears in `ALUStation.cs:82` — consider fixing there too").
   - Output a **[USER TEST REQUIRED]** block with explicit step-by-step instructions to confirm the fix in-game.

---

## MCP connection notes

- **Recompile disconnects:** After writing a script, Unity recompiles and the MCP connection drops briefly. If the next MCP call fails with "Unity not detected", wait ~8 seconds and retry once.
- **Stuck on the same error:** If the same MCP tool call fails 3 times in a row, stop and ask the user to do the step manually. Do not keep retrying.
- **Scene object references:** `Unity_ManageGameObject` cannot reliably set `UnityEngine.Object` reference fields via `{"find": ..., "method": ...}` syntax. After 2 failed attempts, mark **[USER ACTION REQUIRED]** and move on.

---

## Quick-reference: which MCP tool for what

| Need | Tool |
|---|---|
| Read console errors/logs | `Unity_GetConsoleLogs` / `Unity_ReadConsole` |
| Read a script | `Unity_ManageScript` (action: read) |
| Search file contents for a symbol | `Unity_FindInFile` |
| Find assets by name/type | `Unity_FindProjectAssets` |
| Apply targeted script edits (logs, guards) | `Unity_ScriptApplyEdits` / `Unity_ApplyTextEdits` |
| Validate a script before writing | `Unity_ValidateScript` |
| Write/overwrite a script | `Unity_ManageScript` |
| Add/remove/configure component | `Unity_ManageGameObject` |
| Open/save scene | `Unity_ManageScene` |
| Enter/exit Play Mode | `Unity_ManageEditor` |
| Screenshot (Game view) | `Unity_Camera_Capture` |
| Screenshot (Scene view) | `Unity_SceneView_Capture2DScene` |
| Run a menu command | `Unity_RunCommand` |