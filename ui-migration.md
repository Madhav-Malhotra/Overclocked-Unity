# UI Migration Plan: uGUI → unity-ui-toolkit-design-system

## Status (read this first)

- **Phase 0: DONE.** Committed as `3a7ab4d` ("feat: vendor unity-ui-toolkit-design-system for
  UI Toolkit migration") on `main`. See "Phase 0" section below for exactly what was installed
  and why, including a rejected submodule approach and a real upstream bug that was patched.
- **Phase 1 (EndScreenUI pilot): DONE.** Verified end-to-end by the user (real gameplay from
  `MainMenu` through both success and failure paths). See "Phase 1 — actual result" below for
  what shipped — the original Phase 1 plan in this doc predates implementation and diverges from
  it in a few ways (folder layout, background-image handling), so treat that section's code
  snippets as historical intent, not current state.
- **Phase 2 (MainMenuUI): NOT STARTED.** This is the next work to do. Before starting, load the
  `sk-design` skill for design-system class/component reference, and follow `sk-implement`'s
  plan → confirm → implement → verify loop. Reuse `Assets/UI/Shared/DefaultPanelSettings.asset`
  rather than creating a new PanelSettings asset — see Phase 1's actual result for why.
- If you are a fresh session with no memory of this conversation: this file is written to be
  self-contained. You do not need any other context to continue. Start by reading
  `Assets/UI/EndScreen/EndScreenUI.cs` (the Phase 1 pilot, now the template for later phases),
  `Assets/DesignSystem/docs/COMPONENTS.md`, and `Assets/DesignSystem/README.md` before touching
  anything. Also load `sk-design` for a faster reference than re-reading the full docs.

## Goal

Replace hand-positioned uGUI (sprite backgrounds + separately-placed `TextMeshProUGUI`) with
[sinanata/unity-ui-toolkit-design-system](https://github.com/sinanata/unity-ui-toolkit-design-system)
(UI Toolkit, `ds-` prefixed BEM classes, token-driven theme). Migrate one screen at a time,
verify each before moving on.

## Scope

**In scope** (screen-space UI, currently uGUI):
- `Assets/UI/EndScreenUI.cs` — success/failure overlay (`Assets/Scenes/EndScreen.unity`)
- `Assets/UI/MainMenuUI.cs` — start button (`Assets/Scenes/MainMenu.unity`)
- `Assets/UI/GameHUD.cs` — timer + progress badges (`Assets/Scenes/Playground.unity`)
- `Assets/UI/InteractionUIManager.cs` — "E - Pick Up/Place" prompt (`Playground.unity`)
- `Assets/UI/TickFeedbackUI.cs` — validation error toast (`Playground.unity`)

**Out of scope** (do not touch in this migration):
- `Assets/Interactables/TableProcessingTimer.cs` — world-space billboard timer above tables,
  not screen-space UI. Stays uGUI/`Image.fillAmount` for now; revisit only if we later want a
  `ds-meter` world-space treatment (would need `PanelRenderer`/render-texture approach, separate task).
- Camera/Cinemachine and 3D scene layout (CPU stations, circuit traces) — unrelated system,
  already flagged as a separate follow-up in an earlier conversation.

## Why one-screen-at-a-time

Every migrated screen requires: a `UIDocument` + `.uxml` file, a rewritten controller script
(field access changes from serialized `TextMeshProUGUI`/`Button` refs to `VisualElement` queries),
and scene rewiring. None of this is mechanical/scriptable — each screen's controller logic differs
enough that batching risks silent breakage. `EndScreenUI` is the smallest and most self-contained,
so it's the pilot; its pattern becomes the template for the rest.

---

## Phase 0 — Install the design system — **DONE, committed as `3a7ab4d`**

Source: [sinanata/unity-ui-toolkit-design-system](https://github.com/sinanata/unity-ui-toolkit-design-system)
(MIT license). What actually happened, in order, including two rejected approaches — kept here
so nobody re-tries them:

1. ~~**Git submodule directly under `Assets/`**~~ — tried first, reverted. The upstream repo's
   root is a full host Unity project (`ProjectSettings/`, `Packages/`, `Showcase/`, `Tools/`), not
   just the package folder. Mounting the submodule at `Assets/DesignSystemVendor` made Unity try
   to import that host project's `ProjectSettings/XRSettings.asset` etc. as real assets, throwing
   compile/import errors serious enough to trigger Unity's "Safe Mode" prompt on project open.
2. ~~**Submodule outside `Assets/` + OS-level symlink/junction into `Assets/DesignSystem`**~~ —
   this is the README's own recommended "Option B" for keeping the system updatable. Rejected
   before even trying it: the team is split across Windows, WSL, and Mac/Linux, and
   `mklink /J` (Windows) vs `ln -s` (Mac/Linux) don't round-trip through git — the README itself
   says to `.gitignore` the link and have each contributor recreate it after cloning. That's a
   manual, easy-to-forget, per-machine setup step across 3+ OSes — rejected as too fragile.
3. **Decision made: plain copy — the README's "Option A".** Cloned the upstream repo to a scratch
   directory, copied **only** `Assets/DesignSystem/` (the actual package folder, `.meta` files
   included so GUIDs are preserved) into this project at `Assets/DesignSystem/`, discarded the
   rest of the scratch clone. This is now plain tracked source in our repo: no submodule, no
   symlink, zero per-OS setup step for any teammate. Tradeoff knowingly accepted: pulling a newer
   upstream version means re-cloning upstream and manually diffing/re-copying over any local
   edits, rather than `git submodule update --remote`. Considered acceptable because:
   - We already had to hand-patch a real bug in the package (next point) — plain ownership of
     the source turned out to be the better fit anyway, not just the fallback.
   - Additionally copied in (not part of the original package folder, fetched separately from the
     repo root/`docs/`): `README.md`, `AGENTS.md`, `llms.txt`, and `docs/{ARCHITECTURE.md,
     COMPONENTS.md, FONTS.md, ICONS.md, MOBILE.md}` — these live inside `Assets/DesignSystem/`
     alongside the code so the docs travel with it. Deliberately did **not** copy
     `CHANGELOG.md`/`CONTRIBUTING.md`/`SECURITY.md`/`CITATION.cff` (upstream-contribution-focused,
     not useful for consuming the package).
4. **Known upstream bug, patched locally — already applied and committed.**
   `Assets/DesignSystem/Runtime/Behaviour/DesignSystemBehaviourBase.cs`, in `AttachToAll()`,
   originally called `FindObjectsByType<TComponent>()` with **zero arguments**. This overload
   does not exist in *any* Unity version — the API always requires a `FindObjectsSortMode`
   argument — so this is a genuine bug in the vendored package's own code (not a Unity-version
   compatibility issue; confirmed it's the only occurrence of this pattern in the whole package).
   It surfaced as a hard compile error (`CS1501`) that forced Unity into Safe Mode on first open
   after adding the files. Fixed to:
   `var docs = FindObjectsByType<TComponent>(FindObjectsSortMode.None);`
   This fix has **not** been reported upstream yet (worth doing at some point, low priority —
   file an issue/PR against the GitHub repo above if picking this up).
5. **Confirmed via `Unity_GetConsoleLogs`:** package imports with **zero errors**. One harmless
   pre-existing warning remains from the package's own Editor tooling (unrelated to anything we
   did): `ThemeConfiguratorWindow.cs(68,17): warning CS0618: 'EditorUtility.InstanceIDToObject(int)'
   is obsolete`. Safe to ignore — Editor-only tool code, not runtime, not blocking.
6. **Google Font imported:** used Unity menu **Design System → Google Fonts**, searched and
   imported **Poppins** (Regular + Bold weights). Generated at
   `Assets/Resources/DsFonts/Poppins/`: `Poppins-Regular.ttf`, `Poppins-Bold.ttf`, their `SDF.asset`
   FontAssets, `Poppins.asset`, and `Poppins.uss` (the stylesheet to `<Style src>` after
   `DesignSystem.uss` on any screen that wants Poppins typography — see README's Fonts section).
   Two non-fatal warnings appeared during import ("Unable to load font face for [] font asset")
   but file sizes/console errors were checked and are consistent with a working import; **not yet
   visually confirmed by rendering actual text** — deliberately deferred to Phase 1, since Phase 1
   is the first screen that will render real `.ds-h1`/`.ds-body-1` text. **When building Phase 1,
   treat "does Poppins actually render" as part of that phase's verification, not a settled fact.**
7. **Theme asset created:** duplicated `Assets/DesignSystem/Resources/UI/Themes/Dark.asset` (via
   `Unity_ManageAsset` Duplicate action, so it got a fresh GUID rather than colliding) into a
   project-owned copy at **`Assets/DesignSystem/Themes/OverclockedDark.asset`**. Currently
   byte-for-byte identical to stock Dark — this exists purely so future retinting has one place
   to edit without touching any screen. **Not yet attached to anything** — no scene uses a
   `ThemeApplier` pointing at it yet; that starts in Phase 1 (see Phase 1's scene wiring step).

**Net result of Phase 0:** `Assets/DesignSystem/` exists in this repo as plain committed source,
imports cleanly, has docs alongside the code, has a font (Poppins) and a project theme asset
(`OverclockedDark`) ready to use. Nothing has been wired into any actual scene yet — that is
entirely Phase 1's job.

---

## Phase 1 — Pilot: `EndScreenUI` (`Assets/Scenes/EndScreen.unity`)

### 1.1 Current state
- `successPanel`/`failurePanel` are separate `GameObject`s (each: background `Image` + 2×
  `TextMeshProUGUI` + `Button`(s)), toggled via `SetActive`.
- Controller (`EndScreenUI.cs`) reads `LevelTransferData` in `Awake()`, branches success/failure,
  sets text, wires 3 buttons (`retryButtonSuccess`, `retryButtonFailure`, `nextLevelButton`).

### 1.2 New structure
- One `UXML` file, e.g. `Assets/UI/EndScreen.uxml`, with both panels as sibling
  `VisualElement`s under one `.ds-root`, visibility toggled via `display`/`style.display`
  instead of separate GameObjects:
  Attach both `DesignSystem.uss` and `Poppins.uss` (Poppins second, per README's Fonts section —
  order matters, font styles must load after the base system):
  ```xml
  <ui:UXML>
    <Style src="project://database/Assets/DesignSystem/Resources/UI/Styles/DesignSystem/DesignSystem.uss" />
    <Style src="project://database/Assets/Resources/DsFonts/Poppins/Poppins.uss" />
    <ui:VisualElement class="ds-root">
      <ui:VisualElement name="success-panel" class="ds-modal">
        <ui:Label name="success-header" class="ds-h1" />
        <ui:Label name="success-stat" class="ds-body-1" />
        <ui:VisualElement class="ds-modal__actions">
          <ui:Button name="next-level-btn" text="Next Level" class="ds-btn ds-btn--primary" />
          <ui:Button name="retry-btn-success" text="Retry" class="ds-btn ds-btn--ghost" />
        </ui:VisualElement>
      </ui:VisualElement>
      <ui:VisualElement name="failure-panel" class="ds-modal">
        <ui:Label name="failure-header" class="ds-h1" />
        <ui:Label name="failure-stat" class="ds-body-1" />
        <ui:VisualElement class="ds-modal__actions">
          <ui:Button name="retry-btn-failure" text="Retry" class="ds-btn ds-btn--primary" />
        </ui:VisualElement>
      </ui:VisualElement>
    </ui:VisualElement>
  </ui:UXML>
  ```
  (`.ds-modal` gives header/body/actions structure for free — matches COMPONENTS.md's Modal spec.)
- A `UIDocument` component on a GameObject in `EndScreen.unity` referencing this UXML + a
  `PanelSettings` asset (create one if the scene doesn't already have one for UI Toolkit).

### 1.3 Controller rewrite — **[BREAKING RISK]**
`EndScreenUI.cs` public/serialized surface changes completely:
- Remove: `successPanel`, `failurePanel`, `successHeaderText`, `successStatText`,
  `nextLevelButton`, `retryButtonSuccess`, `failureHeaderText`, `failureStatText`,
  `retryButtonFailure` (`[SerializeField]` `GameObject`/`TextMeshProUGUI`/`Button` fields).
- Add: `[SerializeField] private UIDocument uiDocument;` and resolve everything else via
  `uiDocument.rootVisualElement.Q<...>("name")` in `Awake()`/`OnEnable()`.
- Logic (`ShowSuccess`, `ShowFailure`, `OnRetry`, `OnNextLevel`, the `LevelTransferData` branch)
  stays structurally identical — only the field types and lookup mechanism change.
- Since this removes every existing serialized field, the current `EndScreenUI` component
  instance in `EndScreen.unity` will lose all its Inspector wiring. This is expected and
  contained to one scene, one GameObject — no prefab fan-out.

### 1.4 Scene wiring — **[USER ACTION REQUIRED many steps]**
1. Add `UIDocument` component to the GameObject holding `EndScreenUI` (or a new dedicated one).
2. Assign the new `EndScreen.uxml` as its Source Asset, assign/create a `PanelSettings` asset.
3. Remove the old `Canvas`/panel GameObjects (or leave disabled until verified, then delete).
4. Assign `uiDocument` field on `EndScreenUI` in the Inspector.
5. Attach `Assets/DesignSystem/Themes/OverclockedDark.asset` (created in Phase 0, currently
   identical to stock Dark) via a `ThemeApplier` component (`UIDocument` variant — see
   `Assets/DesignSystem/Runtime/Theme/Applier/UIDocument/ThemeApplier.cs`) on the same GameObject
   as the `UIDocument` (or call `ThemeRuntime.Apply(...)` in `Awake()` before resolving elements —
   recommend the `ThemeApplier` component for consistency, since every future screen will need
   the same theme attached and a component is more discoverable in the Inspector than a C# call
   buried in `Awake()`). This is the **first** scene to actually use `OverclockedDark` — if this
   step reveals the theme needs adjusting, that's expected and fine, just note it here for the
   next screen.

### 1.5 Verify
1. `Unity_GetConsoleLogs` — zero new errors after script + scene changes.
2. Claude may enter Play Mode briefly / screenshot Game view for a quick visual check that the
   success/failure panel renders with `ds-` styling (no gameplay to drive here, so this is fine
   per CLAUDE.md's exception for UI-only checks).
3. **[USER TEST REQUIRED]** User verifies both success and failure paths look right and buttons
   still trigger `SceneLoader.LoadGame` — full instructions given in Phase 4 summary once built.

---

### 1.6 Phase 1 — actual result (read this instead of trusting 1.1–1.5 above)

What actually shipped diverges from the original plan in several ways worth knowing before
starting Phase 2:

- **Folder layout changed mid-implementation.** The user asked for per-screen subfolders instead
  of a flat `Assets/UI/`. Final layout:
  ```
  Assets/UI/
    Shared/
      DefaultPanelSettings.asset   ← reusable across all screens, NOT per-screen
    EndScreen/
      EndScreenUI.cs               ← moved from Assets/UI/EndScreenUI.cs, same GUID preserved
      EndScreen.uxml
    MainMenuUI.cs, GameHUD.cs, InteractionUIManager.cs, TickFeedbackUI.cs, TickButtonHandler.cs,
    RoundedBadge.png, SceneBackgrounds/   ← still flat, not yet migrated (Phase 2/3's job)
  ```
  Follow this same per-screen subfolder pattern for Phase 2 (`Assets/UI/MainMenu/`) and Phase 3
  (`Assets/UI/HUD/` — the doc's original plan to share one `UIDocument`/UXML tree across HUD +
  prompt + toast still holds; put it under one `HUD/` folder).
- **`DefaultPanelSettings.asset` was NOT put in `Assets/DesignSystem/`.** The user flagged that
  `DesignSystem/` must stay reusable-components-only, not project-specific config. It lives at
  `Assets/UI/Shared/DefaultPanelSettings.asset` instead. **Reuse this same asset for every future
  screen's `UIDocument`** — do not create a new PanelSettings per screen unless a screen genuinely
  needs different settings (e.g. HUD wanting a different sort order).
- **No MCP tool can create a `PanelSettings` asset** (`Unity_ManageAsset` Create only supports
  Folder/Material/ScriptableObject, and `PanelSettings` isn't creatable via the generic
  ScriptableObject path either — tried and failed). This one is **[USER ACTION REQUIRED]** via
  the Editor's `Assets → Create → UI Toolkit → Panel Settings Asset` menu, every time a new one
  is genuinely needed (should be rare, see previous point).
- **Design tokens don't cover "2x bigger" out of the box.** `.ds-h1`/`.ds-body-1`/button classes
  are fixed-size (26px/14px/etc, see COMPONENTS.md's Typography table) — there's no built-in
  "large" typography variant. Achieved via inline `style="font-size: ...px;"` overrides on top of
  the `ds-` classes. Not wrong, just note that going bigger than the system's defaults is always
  a manual per-element override, not a class switch.
- **Two panels toggled by `display:none` must not share flex flow**, or the visible one's position
  shifts depending on which sibling is hidden. Fix: both `success-panel`/`failure-panel` use
  `position: absolute` with identical anchor offsets (`right`/`top`/`translate`), so they occupy
  the exact same screen position regardless of which is displayed. If Phase 3's HUD trio (timer +
  prompt + toast) ever need mutually-exclusive states in the same screen region, apply the same
  pattern.
- **The old uGUI background art (success-image vs failure-image) was almost lost.** The original
  field list for migration (`successPanel`, `failurePanel`, text, buttons) didn't include the
  background `Image`s nested inside those panels — disabling the old panels silently killed the
  background swap too. Fixed by adding `successBackground`/`failureBackground` `GameObject`
  fields back to `EndScreenUI.cs`, moving those two background GameObjects out to be direct
  children of `Canvas` (so they're independent of the now-deleted old panels), and toggling them
  in `ShowSuccess()`/`ShowFailure()` alongside the UXML panel visibility. **When migrating
  GameHUD/InteractionUIManager/TickFeedbackUI in Phase 3, check for any similar "visual element
  nested inside the old uGUI hierarchy that isn't an explicit serialized field" before deleting
  old GameObjects** — the compiler won't catch a dropped visual dependency like this, only manual
  testing will.
- **Click handling**: uGUI's `button.onClick.AddListener(Fn)` becomes
  `button.RegisterCallback<ClickEvent>(_ => Fn())` in UI Toolkit — different event API, not a
  drop-in method rename.
- **Old `SuccessPanel`/`FailurePanel` GameObjects (and their now-orphaned children — old
  `HeaderText`, `StatText`, buttons) were deleted** after the background art was safely moved out
  and the new UI Toolkit panels were verified working. Don't delete old uGUI GameObjects before
  confirming nothing else (like background art) is silently depending on them.

## Phase 2 — Second screen: `MainMenuUI` (`Assets/Scenes/MainMenu.unity`)

Smallest possible next step (single button) — good for confirming the Phase 1 pattern
generalizes before tackling anything with dynamic per-frame text (HUD).

1. New `MainMenu.uxml`: `.ds-root` + single `ui:Button` with `.ds-btn .ds-btn--primary .ds-btn--lg`.
2. Rewrite `MainMenuUI.cs`: replace `[SerializeField] private Button startButton;` (uGUI) with
   `UIDocument` + `Q<Button>("start-btn")`. — **[BREAKING RISK]**, contained to one field, one scene.
3. Scene wiring: same `UIDocument`/`PanelSettings`/`ThemeApplier` pattern as Phase 1.
4. Verify: console check, brief screenshot check, **[USER TEST REQUIRED]** click start → confirms
   `SceneLoader.LoadGame(0)` still fires.

---

## Phase 3 — HUD + prompt + toast (`Assets/Scenes/Playground.unity`)

These three share one scene and likely should land as one `UIDocument`/UXML tree (one HUD root
with sub-sections) rather than three separate documents, since they're all always-present
overlay chrome during gameplay. Still implement/verify as three logical units per sk-implement's
"one logical unit at a time" rule.

### 3.1 `GameHUD.cs` (timer + progress)
- UXML: `.ds-root .ds-root--hud` (transparent-over-gameplay variant per COMPONENTS.md) containing
  two `ui:Label` elements (`.ds-h2` or similar) for timer/progress text — plain labels, no need
  for a `ds-meter` unless we want a visual progress bar instead of "3 / 7" text (optional
  enhancement, not required for parity — flag as a nice-to-have, not part of this migration pass).
- Controller: replace two `TextMeshProUGUI` fields with `Label` refs via `Q<Label>`. Logic in
  `Update()` unchanged. `SetVisible` becomes `root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None`
  instead of `gameObject.SetActive`. — **[BREAKING RISK]**, small.

### 3.2 `InteractionUIManager.cs` (interact prompt)
- UXML: a `ui:Label` with `.ds-body-1` (or `.ds-chip`-style pill if we want it to look like a
  keycap hint) inside the same HUD root, toggled via opacity.
- Controller: replace `TextMeshProUGUI` + `CanvasGroup` fields with a `Label` ref; opacity toggle
  becomes `label.style.opacity = 1f` / `0f` (UI Toolkit elements support `style.opacity`
  directly — no `CanvasGroup` equivalent needed). — **[BREAKING RISK]**, small, logic unchanged.

### 3.3 `TickFeedbackUI.cs` (validation error toast)
- Maps directly onto `.ds-toast--danger` from COMPONENTS.md (icon + message + close, or just
  message for parity) — this is a better fit than the current generic `panel` GameObject.
- Controller: replace `GameObject panel` + `TextMeshProUGUI messageText` with a `VisualElement`
  (toast root) + `Label` ref, toggle via `style.display`. Auto-hide coroutine logic unchanged.
  — **[BREAKING RISK]**, small, `public` fields become private + `UIDocument`-resolved.

### Verify (per sub-step, not just at the end)
1. Console check after each of 3.1/3.2/3.3.
2. Brief screenshot check for visual placement (timer top-left, progress badge, prompt, toast).
3. **[USER TEST REQUIRED]**, listed fully in Phase 4 summary: walk around, pick up/place a brick
   (prompt text swap), trigger a validation error (toast appears + auto-hides), let timer run
   (confirms `Update()` still ticks correctly through the new `Label` refs).

---

## Explicitly deferred (not part of this migration)

- `TableProcessingTimer.cs` (world-space, out of scope — see above).
- Any new component types not needed for parity (e.g. swapping GameHUD's progress text for a
  `.ds-progress`/`.ds-meter` bar) — flagged as optional follow-up, not required to ship this migration.
- Cinemachine/camera and CPU-station scene layout — separate, unrelated task.
- Mobile-responsive (`mobile` class) work — not relevant until/unless this ships to a touch platform.

## Order of execution

1. Phase 0 (install) — blocks everything, do first, confirm with user before Phase 1.
2. Phase 1 (`EndScreenUI` pilot) — present as its own plan-confirm-implement-verify cycle before Phase 2.
3. Phase 2 (`MainMenuUI`) — same cycle.
4. Phase 3 (`Playground` HUD trio) — same cycle, one sub-screen at a time (3.1 → 3.2 → 3.3).

Each phase gets its own confirmation checkpoint per sk-implement's Phase 1 rule — this document
is the overall roadmap, not a substitute for per-phase plan approval.