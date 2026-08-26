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

BRANCH_MNEMONICS = {"beq", "bne", "blt", "bge", "bltu", "bgeu", "jal"}


def normalize_asm(mnemonic: str, operands: list[str]) -> tuple[str, list[str]]:
    mnemonic = mnemonic.lower()
    operands = [op.strip().lower() for op in operands]
    if mnemonic in BRANCH_MNEMONICS and operands:
        addr = int(operands[-1], 16)
        if addr >= 1 << 31:
            addr -= 1 << 32
        operands[-1] = str(addr)
    return mnemonic, operands


def parse_asm_line(line: str) -> tuple[str, list[str]]:
    # objdump instruction lines look like: "   0:\tXXXXXXXX \tmnemonic\top1,op2,op3"
    text = line.split("\t", 2)[-1].strip()
    parts = text.split(None, 1)
    mnemonic = parts[0]
    operands = parts[1].split(",") if len(parts) > 1 else []
    return normalize_asm(mnemonic, operands)


def disassemble(hex_word: str, tmpdir: pathlib.Path) -> tuple[str, list[str]]:
    bin_path = tmpdir / "check.bin"
    word = int(hex_word, 16)
    bin_path.write_bytes(word.to_bytes(4, byteorder="little"))

    result = subprocess.run(
        ["riscv64-unknown-elf-objdump", "-D", "-b", "binary", "-m", "riscv:rv32",
         "-M", "no-aliases,numeric", str(bin_path)],
        check=True, capture_output=True, text=True,
    )
    for line in result.stdout.splitlines():
        line = line.strip()
        if line.startswith("0:"):
            return parse_asm_line(line)
    raise ValueError(f"could not disassemble {hex_word!r}:\n{result.stdout}")


def verify_roundtrip(label: str, hex_word: str, tmpdir: pathlib.Path) -> None:
    parts = label.strip().split(None, 1)
    label_mnemonic = parts[0]
    label_operands = parts[1].split(",") if len(parts) > 1 else []
    expected = normalize_asm(label_mnemonic, label_operands)

    actual = disassemble(hex_word, tmpdir)

    if expected != actual:
        print(
            f"WARNING: roundtrip mismatch for label {label!r} -> {hex_word}\n"
            f"  expected: {expected}\n"
            f"  decoded:  {actual}\n"
            f"  Fix manually using https://luplab.gitlab.io/rvcodecjs/.",
            file=sys.stderr,
        )


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
            label = instr.get("label") or instr.get("id")
            if not instr.get("hex"):
                if not label:
                    continue
                instr["hex"] = assemble(label, tmpdir)
                changed = True

            if label:
                verify_roundtrip(label, instr["hex"], tmpdir)

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
