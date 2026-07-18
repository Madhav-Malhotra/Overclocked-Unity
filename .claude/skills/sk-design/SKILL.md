---
name: sk-design
description: Reference for using the vendored unity-ui-toolkit-design-system (Assets/DesignSystem/) — common ds- component classes, where to look up a specific pattern, and pointers into the design system's own docs. Load this before building or editing any UI Toolkit screen (UXML/USS) in this project, instead of re-reading the full docs from scratch.
---

Quick-reference for `Assets/DesignSystem/` — the vendored, plain-copied (not submodule)
design system used for all UI Toolkit screens in this project. See `ui-migration.md` at the repo
root for the migration plan this system is being adopted for, and `Assets/UI/EndScreen/` for the
first fully-migrated screen (the template to follow).

## Where to look for what

| Need | File |
| --- | --- |
| One-line summary of every `ds-` class, grouped by component | `Assets/DesignSystem/docs/COMPONENTS.md` |
| Why the system is structured the way it is, file load order, theming mechanics | `Assets/DesignSystem/docs/ARCHITECTURE.md` |
| Importing Google Fonts, real bold/weights, multilingual fallback chains | `Assets/DesignSystem/docs/FONTS.md` |
| Adding/using one of the 120 SVG icons, tint variants | `Assets/DesignSystem/docs/ICONS.md` |
| `.mobile` responsive class, touch target sizing | `Assets/DesignSystem/docs/MOBILE.md` |
| Top-level pitch, installation options, architecture diagram, "why this exists" | `Assets/DesignSystem/README.md` |
| AI-assistant-specific usage notes (written for tools like this one) | `Assets/DesignSystem/AGENTS.md` |
| Every class name in one grep-able block (fastest lookup for "does a class named X exist") | `Assets/DesignSystem/llms.txt` |
| Live rendered demo of every component with its DOM structure | `Assets/Showcase/Resources/DesignSystemShowcase.uxml` — **not vendored into this project**, only exists if you clone the upstream repo separately. Treat COMPONENTS.md as the source of truth here instead. |

When in doubt about a specific class's exact behavior, `COMPONENTS.md` is almost always the
fastest answer — it's organized by component category with a DOM snippet per section. Reach for
`ARCHITECTURE.md` only for "why is this designed this way" questions, and `llms.txt` only for a
raw class-name grep.

## Project-specific setup (read this first — differs from upstream defaults)

- **Vendored as plain copied source**, not a submodule/symlink — see `ui-migration.md` Phase 0
  for why. This means `Assets/DesignSystem/` is just normal tracked files in this repo; no special
  update mechanism.
- **Theme asset**: use `Assets/DesignSystem/Themes/OverclockedDark.asset` (a project-owned copy
  of stock `Dark.asset`), not the stock theme directly — attach it via a `ThemeApplier` component
  on the same GameObject as each screen's `UIDocument`.
- **Font**: Poppins (Regular + Bold) is already imported at `Assets/Resources/DsFonts/Poppins/`.
  Every screen's UXML should `<Style>` `Poppins.uss` immediately after `DesignSystem.uss` (order
  matters — Poppins must load second).
- **Shared `PanelSettings`**: reuse `Assets/UI/Shared/DefaultPanelSettings.asset` for every
  screen's `UIDocument` rather than creating a new one per screen. Note: no MCP tool can create a
  `PanelSettings` asset — if a new one is genuinely needed, it's a manual Editor step
  (`Assets → Create → UI Toolkit → Panel Settings Asset`).
- **Per-screen folder layout**: `Assets/UI/<ScreenName>/` holds that screen's controller script +
  UXML together (e.g. `Assets/UI/EndScreen/EndScreenUI.cs` + `EndScreen.uxml`). Shared/reusable
  project-level UI config (like `DefaultPanelSettings.asset`) goes in `Assets/UI/Shared/`.
  **Never put project-specific instantiations inside `Assets/DesignSystem/`** — that folder is
  reserved for the reusable component system itself, mirroring upstream's own structure.

## Skeleton every screen starts from

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements" editor-extension-mode="False">
    <Style src="project://database/Assets/DesignSystem/Resources/UI/Styles/DesignSystem/DesignSystem.uss" />
    <Style src="project://database/Assets/Resources/DsFonts/Poppins/Poppins.uss" />
    <ui:VisualElement name="root" class="ds-root">
        <!-- screen content -->
    </ui:VisualElement>
</ui:UXML>
```

Use `class="ds-root ds-root--hud"` instead of plain `ds-root` for anything that must show the
game/gameplay world or a scene background through it (HUD, overlay, prompt) — plain `ds-root`
paints an opaque `--color-bg` background, which is right for a full menu/modal screen and wrong
for transparent overlay chrome.

## Common components (most-used first)

**Buttons** — `.ds-btn` base + one variant + optional size/modifier:
```xml
<ui:Button text="Save" class="ds-btn ds-btn--primary" />         <!-- green CTA -->
<ui:Button text="Cancel" class="ds-btn ds-btn--ghost" />          <!-- transparent, bordered -->
<ui:Button text="Delete" class="ds-btn ds-btn--danger" />         <!-- red destructive -->
```
Variants: `--primary` `--secondary` `--tertiary` `--ghost` `--danger` `--icon` `--icon-danger`.
Sizes: `--sm` (28px) / default / `--lg` (44px). Modifiers: `--block` (full width), `--pressed`
(force `:active` look).

**Typography** — fixed sizes, no "make it bigger" class; override `font-size` inline if a screen
genuinely needs larger text than the system ships (see `ui-migration.md`'s Phase 1 actual-result
notes — this project already needed to do this for EndScreen's oversized modal text):
```
.ds-h1        26px / bold
.ds-h2        20px / bold
.ds-h3        16px / semibold
.ds-body-1    14px / regular
.ds-body-2    12px / regular
.ds-caption   11px / medium / text-secondary
```
Add `.ds-nowrap` on any label sitting in a fixed-size row (prevents mid-word wrap from flexbox
shrink) or `.ds-truncate` for ellipsized unknown-length text.

**Modals / overlays** — `.ds-modal` gives header/body/actions structure for free:
```xml
<ui:VisualElement class="ds-modal">
    <ui:Label text="Title" class="ds-h1" />
    <ui:Label text="Body copy" class="ds-body-1" />
    <ui:VisualElement class="ds-modal__actions">
        <ui:Button text="Confirm" class="ds-btn ds-btn--primary" />
    </ui:VisualElement>
</ui:VisualElement>
```
`.ds-toast` (+ `--success`/`--info`/`--warning`/`--danger`) for non-blocking notifications —
likely fit for `TickFeedbackUI` in Phase 3. `.ds-dialog` for a slimmer confirm dialog. `.ds-sheet`
for a mobile bottom drawer.

**Two panels toggled by visibility must not share flex flow** — if only one of two siblings is
ever visible via `display:none` at a time, give both `position: absolute` with identical anchor
offsets so the visible one doesn't shift position depending on which sibling is hidden. (This bit
the EndScreen success/failure panel pair — see `ui-migration.md`.)

**Meters (health/timer/progress bars)** — NOT `.ds-progress` (that's 8px app chrome). Use
`.ds-meter` for anything that needs a number readable on the bar itself:
```xml
<ui:VisualElement class="ds-meter ds-meter--danger">
    <ui:VisualElement name="hp-fill" class="ds-meter__fill" style="width: 62%;"/>
    <ui:Label text="184 / 240" class="ds-meter__label"/>
</ui:VisualElement>
```
Sizes: `--sm` (10px, no room for a label) / default (20px, HUD standard) / `--lg` (24px, boss/cast
bar). Likely relevant for `GameHUD`'s timer/progress if it moves beyond plain text (currently
flagged optional in `ui-migration.md` Phase 3).

**Icons** — `.ds-icon` base + `.ds-icon--<name>` (120 available, full list in `ICONS.md`) + size
(`--xs` `--sm` default `--lg` `--xl` `--xxl`) + tint (`--primary` `--secondary` `--accent`
`--danger` etc). Icons auto-retint inside a hovered/active parent button — no manual `:hover`
rules needed.

**Adding a new icon (from Lucide or any other source)** — full workflow, white-fill rule, and the
Texture-vs-VectorImage import-type pitfall are all in `ICONS.md`'s "Adding a new icon" section.
Read that before importing anything — common mistakes (SVGs using `stroke="currentColor"` or
`stroke="black"` instead of white, or letting Unity import as VectorImage instead of Texture)
produce a silent failure: no console error, but the icon renders invisible or untintable.

**Tabs, inputs, toggles, badges, tooltips, drag & drop** — not yet needed by anything in this
project's migration scope, but fully documented in `COMPONENTS.md` if a future screen needs them.

## Controller-script pattern (C# side)

Every migrated screen's `MonoBehaviour` follows this shape (see `EndScreenUI.cs` for the full
worked example):

```csharp
[SerializeField] private UIDocument uiDocument;
private Label myLabel;
private Button myButton;

void Awake()
{
    var root = uiDocument.rootVisualElement;
    myLabel = root.Q<Label>("my-label-name");
    myButton = root.Q<Button>("my-button-name");
    myButton?.RegisterCallback<ClickEvent>(_ => OnClick());
}
```

Key differences from uGUI, easy to get wrong:
- Click handling: `button.RegisterCallback<ClickEvent>(_ => Fn())`, not `onClick.AddListener(Fn)`.
- Visibility toggle: `element.style.display = DisplayStyle.Flex / DisplayStyle.None`, not
  `gameObject.SetActive(bool)` — there's no GameObject per UI element, only `VisualElement`s in a
  tree under one `UIDocument`.
- Opacity toggle (e.g. a fading prompt): `element.style.opacity = 1f / 0f` directly — no
  `CanvasGroup` equivalent needed.
- Before deleting old uGUI GameObjects during a migration, check whether anything nested inside
  them (background art, decorative images) was an implicit dependency, not just the fields
  explicitly listed for migration — see `ui-migration.md`'s Phase 1 background-art incident.