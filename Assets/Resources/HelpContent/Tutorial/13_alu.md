---
title: The ALU
category: Computer Architecture
icon: sparkle
order: 23
keywords: alu, arithmetic, logic, add, subtract, and, or, execute
---
The **ALU** (arithmetic logic unit) is the calculator inside the CPU. In the Execute stage it adds, subtracts, compares and does bitwise logic on the numbers it is given.

For example `addi x5, x0, 1` asks the ALU to add 1 to the value of `x0` (which is 0), then Writeback stores the answer, 1, into `x5`.

## Jargon
- **Bitwise** - operating on each binary digit (bit) separately.
- **Immediate** - a number written directly inside the instruction (the `1` above).
