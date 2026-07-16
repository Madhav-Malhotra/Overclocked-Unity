# Adding a new level

Levels are auto-discovered at runtime from `Assets/Levels/Resources/JSON/*.json`,
sorted by filename. This folder is deliberately named `JSON` (not just left at
the `Resources/` root) so `Resources.LoadAll<TextAsset>("JSON")` only matches
this specific subfolder — Unity merges every folder literally named `Resources`
project-wide, so an unscoped `Resources.LoadAll("")` would also pick up
unrelated TextAssets from other packages (e.g. TextMesh Pro's `Resources/`
folder ships its own text assets at its root).

1. Create `Assets/Levels/Resources/JSON/level_NN.json` (zero-padded, e.g. `level_02.json`)
   following this schema:

   ```json
   {
     "levelName": "Level 2",
     "timeLimit": 120,
     "instructions": [
       { "label": "add x1, x2, x3" },
       { "label": "sub x4, x5, x6" }
     ]
   }
   ```

   `label` must be valid RISC-V assembly (`x0`-`x31` ABI register names) that
   `riscv64-unknown-elf-as` accepts.

2. Run the hex enrichment script from `Assets/Levels/`:

   ```sh
   make hex
   # or: python3 add_hex.py
   ```

   This assembles each instruction's `label` and writes the result back into
   that instruction's `hex` field, in place. It's idempotent — instructions
   that already have `hex` are left untouched, and files with nothing to
   change are not rewritten.

3. Done. `LevelManager` picks up every `*.json` in `Resources/JSON/`
   automatically, ordered by filename, the next time the game runs.

## Requirements

`add_hex.py` needs `riscv64-unknown-elf-as` and `riscv64-unknown-elf-objcopy`
on `PATH`. These are already installed on this machine; teammates without
them can run the script inside the existing Docker toolchain image (see
`verilog/verif/scripts/docker/`).