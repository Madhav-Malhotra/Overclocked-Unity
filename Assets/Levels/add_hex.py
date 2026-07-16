#!/usr/bin/env python3
"""Assembles each level instruction's `label` (RISC-V asm) into a `hex` field.

Walks Assets/Levels/*.json, assembles any instruction missing `hex` via
riscv64-unknown-elf-as + objcopy, and writes the result back into that
instruction's `hex` field. Instructions that already have `hex` are left
untouched. A JSON file is only rewritten if something actually changed.
"""
import json
import pathlib
import subprocess
import sys
import tempfile

LEVELS_DIR = pathlib.Path(__file__).resolve().parent / "Resources" / "JSON"


def assemble(label: str, tmpdir: pathlib.Path) -> str:
    asm_path = tmpdir / "instr.s"
    obj_path = tmpdir / "instr.o"
    bin_path = tmpdir / "instr.bin"

    asm_path.write_text(f".text\n{label}\n")

    subprocess.run(
        ["riscv64-unknown-elf-as", "-march=rv32im", "-mabi=ilp32",
         "-o", str(obj_path), str(asm_path)],
        check=True, capture_output=True, text=True,
    )
    subprocess.run(
        ["riscv64-unknown-elf-objcopy", "-O", "binary", "-j", ".text",
         str(obj_path), str(bin_path)],
        check=True, capture_output=True, text=True,
    )

    raw = bin_path.read_bytes()[:4]
    word = int.from_bytes(raw, byteorder="little")
    return f"0x{word:08X}"


def process_file(path: pathlib.Path) -> bool:
    data = json.loads(path.read_text())
    instructions = data.get("instructions", [])
    changed = False

    with tempfile.TemporaryDirectory() as tmp:
        tmpdir = pathlib.Path(tmp)
        for instr in instructions:
            if instr.get("hex"):
                continue
            label = instr.get("label") or instr.get("id")
            if not label:
                continue
            instr["hex"] = assemble(label, tmpdir)
            changed = True

    if changed:
        path.write_text(json.dumps(data, indent=2) + "\n")

    return changed


def main() -> int:
    any_changed = False
    for path in sorted(LEVELS_DIR.glob("*.json")):
        if process_file(path):
            print(f"updated {path.name}")
            any_changed = True
        else:
            print(f"no change {path.name}")
    return 0 if True else 1


if __name__ == "__main__":
    sys.exit(main())