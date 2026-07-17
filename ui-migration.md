# UI Migration Plan: uGUI → unity-ui-toolkit-design-system

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

## Phase 0 — Install the design system (one-time, blocks everything else)

1. ~~Git submodule~~ — tried first, reverted. The upstream repo's root is a full host Unity
   project (`ProjectSettings/`, `Packages/`, `Showcase/`, `Tools/`), not just the package folder;
   mounting the submodule directly under `Assets/` made Unity try to import that host project's
   `ProjectSettings/XRSettings.asset` etc. as real assets, causing import errors. The README's own
   recommended submodule setup (Option B) requires an OS-level symlink/junction outside `Assets/`,
   which doesn't round-trip across the team's Windows/WSL/Mac/Linux split and would need re-creating
   per clone — rejected for the same reason as before.

   **Decision: plain copy (README's Option A), done.** Cloned the repo to scratch, copied only
   `Assets/DesignSystem/` (the actual package, `.meta` files included) into this project's
   `Assets/DesignSystem/`, discarded the rest of the clone. This is now plain tracked source in
   our repo — no submodule, no link, no per-OS setup step for teammates. Tradeoff accepted:
   updating to a newer upstream version means re-cloning and manually diffing/re-copying,
   rather than `git submodule update --remote`. Worth it here since we already needed to
   hand-patch a bug in the package (see below) — plain ownership of the source is actually
   the better fit, not just the fallback.
2. **Known local patch, already applied:** `Runtime/Behaviour/DesignSystemBehaviourBase.cs` line
   ~932 called `FindObjectsByType<TComponent>()` with no arguments; this overload doesn't exist in
   any Unity version (it always requires a `FindObjectsSortMode` argument) — a genuine upstream
   bug, not a version-compatibility issue. Fixed locally to
   `FindObjectsByType<TComponent>(FindObjectsSortMode.None)`. Since we now own a plain copy (not
   a submodule), this fix is a normal tracked change in our repo — no reapplication needed after
   updates unless we re-copy over it. Worth filing upstream at some point (not done yet).
3. Confirm the package imports cleanly (console check via `Unity_GetConsoleLogs` after Unity
   picks up the new `Assets/DesignSystem` folder).
4. **[USER ACTION REQUIRED]** Import a Google Font family via `Design System > Google Fonts`
   menu (needed for `.ds-h1`/`.ds-body-1`/etc. typography classes to resolve — otherwise text
   falls back silently to a default font).
5. Duplicate `Assets/DesignSystem/Resources/UI/Themes/Dark` → a project-specific `ThemeData`
   asset (e.g. `Assets/DesignSystem/Themes/OverclockedDark.asset`) via the Theme Configurator,
   even if it's identical to stock Dark for now. This gives us one place to retint later without
   touching any screen.

**Verify:** open any scene, confirm no console errors from the new package, confirm the font
asset shows up in the Design System menu.

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
  ```xml
  <ui:UXML>
    <Style src="project://database/Assets/DesignSystem/Resources/UI/Styles/DesignSystem/DesignSystem.uss" />
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
5. Attach the `ThemeData` asset from Phase 0 via `ThemeApplier` component on the same GameObject
   (or call `ThemeRuntime.Apply(...)` in `Awake()` before resolving elements — decide based on
   whether other scenes will reuse this pattern; recommend `ThemeApplier` component for
   consistency across scenes).

### 1.5 Verify
1. `Unity_GetConsoleLogs` — zero new errors after script + scene changes.
2. Claude may enter Play Mode briefly / screenshot Game view for a quick visual check that the
   success/failure panel renders with `ds-` styling (no gameplay to drive here, so this is fine
   per CLAUDE.md's exception for UI-only checks).
3. **[USER TEST REQUIRED]** User verifies both success and failure paths look right and buttons
   still trigger `SceneLoader.LoadGame` — full instructions given in Phase 4 summary once built.

---

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