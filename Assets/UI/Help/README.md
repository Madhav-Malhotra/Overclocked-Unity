# Help system: tutorial, level tips, settings, about

Everything here is content-driven. You normally only edit text files; no code or scene changes.

## 1. Tutorial panes (Markdown)

One file per pane in `Assets/Resources/HelpContent/Tutorial/*.md`. Drop a file in and it appears in
the menu. Format:

```
---
title: The ALU                  # shown in the list + as the heading
category: Computer Architecture # panes are grouped under this heading in the sidebar
icon: sparkle                   # any ds-icon name, see Assets/DesignSystem/docs/ICONS.md (default: book)
order: 23                       # sorting; categories are ordered by their lowest-order pane
keywords: alu, add, execute     # extra search terms (title, keywords and body are all searched; title matches are listed first)
---
Markdown body...
```

- **Topic id** = file name minus any leading `NN_` and with `_` → `-`
  (`13_alu.md` → `alu`, `01_instruction_bricks.md` → `instruction-bricks`).
- Supported markdown: `#`/`##`/`###` headings, paragraphs, `- ` bullets, `1. ` lists, `> ` callouts,
  ``` fenced code, `---` dividers, `**bold**`, `*italic*`, `` `code` ``.
- **Jargon**: by convention end each pane with a `## Jargon` list of `- **Term** - definition`.
- **Links**: a line containing only `[Label](topic:other-topic-id)` becomes a clickable button.
- **Images / diagrams**: `![caption](diagrams/pipeline.png)` or `![caption](x.png){width=300}`.
  Paths are relative to `Assets/Resources/HelpContent/Images/` (extension optional).
- `Assets/Resources/HelpContent/about.md` is the main-menu About screen.

## 2. Level tips (in the level JSON)

Add an optional `tips` array to `Assets/Levels/Resources/JSON/level_NN.json`. No `tips` = no tour for that
level. Any number of steps; the card shows "n / N".

```json
"tips": [
  { "title": "Welcome", "text": "Use W A S D to move." },
  { "title": "Spawner", "text": "Grab a brick here.", "target": "start-platform", "placement": "right" }
]
```

| field | meaning |
| --- | --- |
| `text` | body text (required) |
| `title` | optional heading |
| `target` | the `id` of a `TipTarget` component in the scene (see below). A pulsing ring is drawn around that object. Empty = no ring. A missing id logs a warning. |
| `placement` | `above` / `below` / `left` / `right` = put the card next to the target. Empty (or no target) = fixed spot at the top right of the screen. |

The tour starts automatically on level load (unless disabled in Settings) and can be replayed from
the **Tips** button. Each card has Back / Next and an X to dismiss. The **?** button / **F1** opens
the tutorial, which pauses the game.

### Tip targets

Add a `TipTarget` component to any scene object and give it a unique `id`. Tips refer to that id, so
there is no name/type guessing. Duplicate ids are logged as errors. Ids currently in use:
`start-platform`, `end-platform` (FiveStage, Blackbox); `fetch`, `decode`, `execute`, `memory`,
`writeback` (FiveStage); `blackbox-cpu` (Blackbox). Superscalar has none yet.

## 3. Wiring a new screen

Put `HelpOverlay` on the GameObject that owns the screen's `UIDocument`. For a gameplay HUD also add
`LevelTipsController` and the buttons in `HUD.uxml` (`help-btn`, `tips-btn`).

Look and feel: `Resources/HelpOverlay.uss` (cyan accent is the single `--help-accent` variable).
