---
title: Registers and memory
category: Computer Architecture
icon: save
order: 22
keywords: register, regfile, x0, memory, data, store, load, pc
---
A CPU keeps data in two places.

- **Registers** - 32 tiny, super-fast slots named `x0` to `x31`. `x0` is special: it always reads as 0.
- **Memory** - a huge, slower store of numbers, each at an **address**.

The **program counter (PC)** holds the address of the next instruction to fetch.

## Jargon
- **Register file** - the block of 32 registers.
- **Address** - the number that says *where* in memory something lives.
- **Program counter (PC)** - points at the instruction to run next.
