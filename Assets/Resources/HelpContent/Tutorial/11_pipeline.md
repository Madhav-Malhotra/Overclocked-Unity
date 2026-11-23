---
title: The five-stage pipeline
category: Computer Architecture
icon: list
order: 21
keywords: pipeline, fetch, decode, execute, memory, writeback, stage
---
Instead of finishing one instruction before starting the next, a CPU splits the work into **stages** - like an assembly line - so several instructions are in progress at once.

1. **Fetch** - read the next instruction from instruction memory.
2. **Decode** - work out what it means and read the registers it needs.
3. **Execute** - do the maths in the ALU.
4. **Memory** - read or write data memory (only some instructions do).
5. **Writeback** - save the result into a register.

Each press of **T** moves every brick one stage forward.

## Jargon
- **Pipeline** - an assembly line for instructions.
- **Stage** - one step of that assembly line.

[Registers and memory](topic:registers-memory)
[The ALU](topic:alu)
