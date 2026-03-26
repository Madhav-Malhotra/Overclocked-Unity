# Improved UI & Visual Overhaul Plan

This document outlines the step-by-step plan to transform the prototype into a visually coherent, Overcooked-style game representing a 5-stage pipelined CPU. It covers the player character, CPU component station models, instruction brick representation, scene layout, and camera setup.

---

## Phase 1 — Player Character (Floating Computer)

### Step 1.1 — Generate the model with Rodin AI

Use the following prompt in Rodin AI (hyper3d.ai or similar):

> "A cute, cartoonish floating desktop computer. The monitor displays a cheerful pixelated face. The body is compact and rounded like a chunky retro PC tower or all-in-one. It floats slightly above the ground with a soft cyan glowing underside. No arms or legs. Style: vibrant, low-poly-friendly, game-ready, similar to the visual style of Overcooked or Job Simulator. Flat-shaded with bold outlines."

Export format: **FBX** with embedded textures, or **GLB**. Request LODs if available.

### Step 1.2 — Import into Unity

1. Place the exported FBX/GLB into `Assets/Player/Models/PlayerComputer/`.
2. In the Unity Project window, select the imported model. In the Inspector under **Model**:
   - Set **Scale Factor** to match scene scale (try 1.0, adjust visually).
   - Enable **Read/Write** if mesh manipulation is needed later.
3. Under **Rig**: set Animation Type to **None** (no skeletal animation needed for this character).
4. Under **Materials**: extract materials into `Assets/Player/Materials/`. Assign the URP Lit or URP Simple Lit shader. Re-link the texture atlas.
5. Create a prefab at `Assets/Player/Prefabs/PlayerComputer.prefab`.

### Step 1.3 — Set up the player GameObject

1. Replace the current primitive player object in the scene with the new prefab.
2. Ensure the prefab root has: `Rigidbody`, `CapsuleCollider` (sized to the visual bounds), `PlayerController`, `InteractableDetector`, `PlayerInteractionHandler`, `DiskHoldingSystem`, `InputActionsInitializer`, `PlayerInput`.
3. Add a child `HoverBob` empty GameObject at the visual root of the mesh. In a new script `HoverAnimation.cs` (place in `Assets/Player/`), implement a gentle sine-wave Y offset each `Update` using `Time.time` and a configurable `amplitude` and `frequency` — apply this as `transform.localPosition` on the mesh child, not on the Rigidbody root.
4. Add a `holdPosition` empty child GameObject positioned in front of and slightly above the computer body. Wire this into `DiskHoldingSystem.holdPosition`.

### Step 1.4 — Floating glow effect

1. Add a `Light` component (Point Light, URP) as a child directly under the computer, pointing downward. Set color to soft cyan/white, intensity low (0.3–0.5), range ~1 unit. This simulates the glowing underside.
2. Optionally add a transparent circle mesh ("blob shadow") under the player using a URP Unlit material with an alpha-blended white-to-transparent radial gradient texture.

---

## Phase 2 — CPU Component Station Models

Each station represents one functional unit of a 5-stage pipeline. The player walks between them: **Fetch → Decode → Execute → Memory → Writeback**. Each station consists of two parts:

1. **The decorative component model** — a purely visual 3D prop generated in Rodin AI that represents the CPU component. It has no functional slots; its purpose is visual identity only.
2. **A shared scanner pedestal** — a single Rodin AI model used for every station. The instruction brick is placed on and taken off this pedestal. This pedestal is the actual interactive `Table` object in Unity; the component model next to it is decoration only.

### Rodin AI Generation Strategy

For all component models and the pedestal, preface every prompt with:
> "Game-ready 3D model, low-poly, cartoon/stylized aesthetic matching Overcooked. Bold colors, chunky forms, clearly readable silhouette. Single mesh, single texture atlas. No fine details like text that won't read at game distance. Plain white background."

Then append the component-specific or pedestal description below.

---

### Step 2.0 — Scanner Pedestal (shared across all stations)

This is the only interactive object at each station. It is a short pedestal with a flat glowing top surface where the instruction brick sits while being processed.

**Prompt addition:**
> "A short, chunky pedestal with a flat top platform. The top surface glows with a soft white light, like a scanner bed or data reader. The body is a rounded rectangular column. Looks like a futuristic item scanner or check-in terminal. Color: white/light grey with cyan accent lighting."

Generate **one** model. It will be reused at every station. Import it as `Assets/Interactables/Models/ScannerPedestal/` and create a prefab at `Assets/Interactables/Prefabs/ScannerPedestal.prefab`. This prefab is the object that carries the `Table` (or `CPUStation`) component and contains the `diskSlot` child transform on its top surface.

---

### Step 2.1 — Instruction Memory (Fetch Stage)

**Prompt addition:**
> "A chunky retro ROM chip or cartridge module standing upright on its own base. Rectangular block body with rows of pin connectors along the bottom edge. Has a subtle blinking LED array on the front face and a bold label area. Purely decorative — no functional slots or openings needed. Color: deep blue with gold accents."

---

### Step 2.2 — Instruction Decoder

**Prompt addition:**
> "A chunky industrial control panel or switchboard box. The front face is covered with a grid of small toggle switches, rotary knobs, and indicator lights. Has a bold embossed label area. Purely decorative — no functional slots or openings needed. Color: dark grey with orange accents."

---

### Step 2.3 — Register File

**Prompt addition:**
> "A tall rectangular cabinet resembling a miniature server rack or filing cabinet. The front face has rows of small glowing drawer handles or memory cell indicators. Has a subtle ventilation grille on the sides. Purely decorative — no functional slots or openings needed. Color: green with silver hardware."

---

### Step 2.4 — ALU (Arithmetic Logic Unit)

**Prompt addition:**
> "A futuristic calculator (but just a small cyan LED without numbers and three keys with plus, minus, and multiply signs). Purely decorative — no functional numbers, keys, or other details needed. Color: dark grey with cyan accents."

---

### Step 2.5 — Branch Comparator

**Prompt addition:**
> "A compact machine that resembles a set of balance scales. Has a large dial or needle display on the front face indicating a comparison result. Has two symmetrical side panels. Purely decorative — no functional slots or openings needed. Color: purple with white accents."

---

### Step 2.6 — Data Memory

**Prompt addition:**
> "A chunky retro ROM chip or cartridge module standing upright on its own base. Rectangular block body with rows of pin connectors along the bottom edge. Has a subtle blinking LED array on the front face and a bold label area. Purely decorative — no functional slots or openings needed. Color: deep blue with gold accents."

---

### Step 2.7 — Multiplexer (Mux)

**Prompt addition:**
> "A trapezoidal prism shape — wide at the back, tapering to a narrower front face, like a wedge or funnel. Has decorative groove lines running from the wide back face converging toward the narrow front, suggesting signals merging. A small selector dial or switch sits on top. Purely decorative — grooves are surface detail only, not actual slots. Color: warm yellow with black accents."

---

### Step 2.8 — Demultiplexer (Demux)

**Prompt addition:**
> "A 3D multiplexer created with a 2D trapezoid being extruded to have height. Has three lines fanning from the wide face to the narrow face, like a signal merging. A small selector dial or switch sits on top. Purely decorative. Color: dark grey with cyan accents."

---

### Step 2.9 — Pipeline Registers

**Prompt addition:**
> "A flat, wide horizontal slab resembling a data latch or memory buffer strip. The top face has rows of small LED indicators or data cell squares running along its length. Has a clean, minimal look like a circuit board section. Purely decorative — no functional slots or openings needed. Color: light grey/silver with blue LED accents."

Four instances are needed (IF/ID, ID/EX, EX/MEM, MEM/WB). All four use the same model. Distinguish them in the scene by placing a small text label (using a Unity TextMeshPro world-space canvas) above each one showing the register name.

---

### Step 2.10 — Import all models

For each component model and the scanner pedestal:
1. Place FBX/GLB into `Assets/Interactables/Models/<ComponentName>/`.
2. Import following the same pipeline as Step 1.2 (scale, materials, URP shader).
3. Extract materials to `Assets/Interactables/Materials/`.
4. Create a prefab at `Assets/Interactables/Prefabs/<ComponentName>.prefab`.

For the **component models** (Steps 2.1–2.9): these prefabs need only a `MeshCollider` or `BoxCollider` (for blocking navigation) and no scripts. They are pure decoration.

For the **scanner pedestal** (Step 2.0): the prefab root needs a `BoxCollider`, a child `diskSlot` empty GameObject on the top surface, and the `CPUStation` component (see Phase 4). Wire the `diskSlot` into `CPUStation` in the Inspector.

In the scene, place each component model prefab adjacent to its corresponding scanner pedestal. Group both under an empty parent GameObject named after the station (e.g., `Station_Fetch`).

---

## Phase 3 — Instruction Brick (In-Engine, No Rodin AI)

This replaces the current `Disk` sphere with a styled rectangular prism representing a binary instruction moving through the CPU pipeline.

### Step 3.1 — Create the Brick prefab

1. In the Unity Editor, create a new GameObject with a `Cube` primitive. Scale it to roughly `(0.4, 0.15, 0.6)` to give a flat rectangular prism shape (like a chunky card or LEGO brick).
2. Create a prefab at `Assets/Interactables/Prefabs/InstructionBrick.prefab`.
3. Add a `Rigidbody` and `BoxCollider` to match existing `Disk` setup.

### Step 3.2 — Create materials

Create 5 + 1 URP Lit materials in `Assets/Interactables/Materials/Brick/`:
- `Brick_Fetch.mat` — blue
- `Brick_Decode.mat` — purple
- `Brick_Execute.mat` — red/orange
- `Brick_Memory.mat` — green
- `Brick_Writeback.mat` — gold/yellow
- `Brick_Default.mat` — grey (unprocessed)

All materials should have emission enabled so they can glow when highlighted.

### Step 3.3 — Create the binary face texture

1. In `Assets/Interactables/Textures/`, create a simple 128×128 PNG (or generate via script) with white `0` and `1` characters on a black background arranged in rows — resembling a binary instruction word. This is applied to one face of the brick.
2. To apply to only one face: either use a multi-material setup on the cube (6 material slots) where 5 sides use the stage color material and 1 side (the top face, index 2) uses a separate `Brick_Binary.mat` with the texture, or use UV unwrapping in a DCC tool to split the face.
3. `Brick_Binary.mat` uses URP Unlit shader with the binary texture, additive or alpha blend if needed.

### Step 3.4 — Script: `InstructionBrick.cs`

Create `Assets/Interactables/InstructionBrick.cs`. This replaces/extends `Disk.cs`:
- Inherit from `Disk` (which inherits `Interactable`), or replace `Disk` entirely if the team agrees.
- Add a `PipelineStage` enum: `Unprocessed, Fetch, Decode, Execute, Memory, Writeback`.
- Add a `SetStage(PipelineStage stage)` method that swaps the non-binary-face materials to the appropriate stage material from a serialized `stageMaterials` array.
- The binary face material slot remains constant — only the 5 side materials change.
- Wire the `SetStage` call into `Table.OnProcessingComplete()` (or a new override in a CPU-station subclass) so each station advances the brick to the correct next stage on completion.

---

## Phase 4 — CPU Station Script Subclasses

Rather than modifying `Table.cs` directly, subclass it for each CPU component so each station knows its pipeline stage and can advance the brick correctly.

### Step 4.1 — Create `CPUStation.cs`

Place in `Assets/Interactables/CPUStation.cs`. Extend `Table`:
- Add a serialized `assignedStage` field of type `PipelineStage` (from Step 3.4).
- Override `OnProcessingComplete()`: call `base.OnProcessingComplete()`, then call `SetStage(assignedStage)` on the `InstructionBrick` currently on the table.
- Each station in the scene uses `CPUStation` instead of `Table`, with `assignedStage` set appropriately in the Inspector.

For the 4 pipeline registers (IF/ID, ID/EX, EX/MEM, MEM/WB), assign the stage that corresponds to what they output (e.g., IF/ID outputs to Decode, so assign `Decode`). These are pass-through stations with a shorter processing time — consider a separate `processingDuration` default.

---

## Phase 5 — Scene Layout

### Step 5.1 — Design the layout

Arrange stations in a left-to-right linear flow (world X axis) with slight depth variation so the player has to physically walk between them. Suggested world positions (approximate, tune in editor):

```
[Instruction Memory]  →  [IF/ID Register]  →  [Decoder]  →  [ID/EX Register]
                                                                        ↓
                                                              [Register File]  [Branch Comparator]
                                                                        ↓
                                                              [ALU]  ←  [Mux (src A)]  [Mux (src B)]
                                                                        ↓
                                                              [EX/MEM Register]
                                                                        ↓
                                                              [Data Memory]
                                                                        ↓
                                                              [MEM/WB Register]
                                                                        ↓
                                                              [Writeback / Register File write port]
```

In practice, simplify to a single winding path (like Overcooked kitchen layouts) so the player always knows which direction to walk. An S-curve or U-shape works well.

### Step 5.2 — Build the scene

1. Delete existing cube placeholder stations from the scene.
2. Place each CPU station prefab (from Phase 2) at the designated world positions.
3. Set each station's `CPUStation.assignedStage` in the Inspector.
4. Add floor tiles and walls using Unity primitive cubes or a simple modular kit. Scale tiles ~1×0.1×1, arrange to form the walking path.
5. Add low walls or railing primitives (thin cubes) at path edges to block the player from walking off. Give them `BoxCollider` components with no `Rigidbody`.
6. Add a few `InstructionBrick` prefabs as starting pickups, placed at or near the Instruction Memory station.

### Step 5.3 — Lighting

1. Set the scene's directional light to a soft warm white at ~45° angle (top-down-ish).
2. Add a low-intensity ambient light via the Lighting window (`Window > Rendering > Lighting`). Use a warm skybox or flat color ambient.
3. Ensure all station materials have `_EMISSION` enabled (already handled by `Interactable.Start()`).

---

## Phase 6 — Fixed Camera Setup

### Step 6.1 — Position the camera

1. Remove any existing camera follow scripts.
2. Place the `Main Camera` at a fixed world position above and behind the scene (isometric-style), looking down at ~45–55° angle. For an Overcooked feel, a slight perspective (FOV ~45–60°) works better than full orthographic.
3. Parent the camera to nothing (leave it as a root object in the scene hierarchy) so it never moves.
4. Adjust position and rotation in the editor until the entire pipeline path is visible in one shot, or at least the majority of it.

### Step 6.2 — Remove camera follow behavior

If any camera follow script currently exists, delete it. No new camera follow script is needed — the camera is purely static.

### Step 6.3 — Update PlayerController rotation

Currently `PlayerController` rotates the player to face the movement direction in world space. With a fixed camera, movement axes should feel relative to the camera angle, not world axes.

In `PlayerController.Update()`:
- Project `moveDirection` through the camera's forward/right vectors before applying it. Specifically: compute `cameraForward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized` and `cameraRight = Camera.main.transform.right`. Then rebuild `moveDirection` as `inputValue.y * cameraForward + inputValue.x * cameraRight`.
- This makes WASD feel natural relative to the fixed camera angle (W = away from camera, S = toward camera).

---

## Phase 7 — Polish & Integration Pass

### Step 7.1 — TimerSelectionUI update

The current 1s/3s/5s/10s timer popup is functional but the durations should map to meaningful processing times per station type. Consider:
- Instruction Memory (Fetch): 3s
- Decoder: 5s
- ALU: 7s
- Data Memory: 5s
- Pipeline Registers: 2s

Consider removing the player-choice popup entirely for registers (auto-process on placement) by overriding `OnInteract()` in `CPUStation` to call `OnTimerSelected` with a hardcoded duration when the station is a register type.

### Step 7.2 — Highlight colors by stage

Update highlight colors on each `CPUStation` Inspector entry to match the brick stage colors (fetch = blue, decode = purple, etc.) so the station visually matches the brick it produces. This uses `Table.processingHighlightColor`.

### Step 7.3 — HoverAnimation on bricks

Apply a gentler version of the `HoverAnimation` script from Step 1.3 to `InstructionBrick` while held by the player, to make it feel "alive". Disable the animation while the brick is placed on a table.

### Step 7.4 — Scene cleanup

- Remove any leftover `Disk` prefab instances and replace with `InstructionBrick`.
- Confirm all prefab references in the scene are wired (`diskSlot`, `holdPosition`, `timerSelectionUI`, `processingTimerPrefab`, `uiManager`).
- Test the full pick-up → walk → place → timer → stage-advance → pick-up loop from Fetch to Writeback.