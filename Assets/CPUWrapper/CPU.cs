/*
 * AUTO-GENERATED from bridge/CPU.cs — DO NOT EDIT DIRECTLY.
 * Edit the source in bridge/ and re-run `make sync-unity` from the bridge/ directory.
 */
/*
 * V0.1.0
 * CPU.cs — C# wrapper around the Verilated RISC-V processor shared library.
 *
 * Provides a managed, object-oriented interface to the flat C API exported by
 * bridge.cpp.  P/Invoke is used to call into the compiled shared library
 * (libdesign_wrapper.so / design_wrapper.dll).
 *
 * Key responsibilities:
 *   - Declare [DllImport] bindings for every extern "C" function in bridge.cpp.
 *   - Expose per-stage helper structs (Fetch, Fd, Dx, Xm, Mw) so callers get
 *     strongly-typed snapshots of each pipeline register.
 *   - Implement IDisposable so cleanup_design_wrapper() is always called even
 *     if an exception occurs.
 *
 * ⚠️ Keep this file in sync with bridge.cpp: whenever a new signal is added to
 * CPUState in C++ or a new extern "C" function is added, mirror the change here.
 *
 * See verilator/README.md for build instructions.
 */

using UnityEngine;
using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CPUState
{
    // MUST stay sync'ed with CPUState in C++, since we pass the struct ptr
    // order must match the CPUState definition
    public uint pc;          // Maps to uint32_t
    public uint instruction; // Maps to uint32_t

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    public uint[] regs;
    public uint alu_out;     // Maps to uint32_t
                             // branch_comp.v output ports
    public byte br_eq;
    public byte br_lt;
    public byte branch_comp_data1_sel; //
    public byte branch_comp_data2_sel; //
    public byte br_taken;              //
    public byte pc_sel;                //
    public byte br_un;                 //
    public byte a_sel;                 // [1:0]
    public byte b_sel;                 // [1:0]
    public byte alu_sel;               // [3:0]
    public byte mem_rw;                //
    public byte reg_wen;               //
    public byte wb_sel;                // [1:0]
                                       // decoder.v output ports
    public byte opcode;         // [6:0]
    public byte addr_rd;        // [ADDRW-1:0]
    public byte addr_rs1;       // [ADDRW-1:0]
    public byte addr_rs2;       // [ADDRW-1:0]
    public byte funct3;         // [2:0]
    public byte funct7;         // [6:0]
    public uint imm;           // [DATAW-1:0] (32-bit)
    public byte shamt;          // [N_BITS-1:0] (Shift amount, typically 5 bits)
    public byte is_u_type_w;    // 1-bit
    public byte is_j_type_w;    // 1-bit
    public byte is_i_type_w;    // 1-bit
                                // dmemory.v output ports
    public uint dmem_data_out; // [31:0]
                               // imemory.v output ports
    public uint imem_data_out; // [31:0]
                               // register_file.v output ports
    public uint data_rs1;       // [DATAW-1:0]
    public uint data_rs2;       // [DATAW-1:0]
                                // writeback.v output ports
    public uint wb_data; // dependent on DATAW=32

    public uint fd_pc;
    public uint fd_pc4;
    public byte opcode_fd;
    public byte addr_rd_fd;

    public byte opcode_dx;
    public byte addr_rd_dx;
    public uint dx_pc;

    public uint dmem_data_in;
    public byte opcode_xm;
    public byte addr_rd_xm;
    public uint xm_pc;

    public uint wb_in_alu;
    public uint mem;
    public uint mw_pc4;
    public byte opcode_mw;
    public byte addr_rd_mw;
    public uint mw_pc;
    // overriding the ToString() function so the string representation is easier to read during debugging
    public override string ToString()
    {
        string regHex = regs != null
        ? string.Join(", ", Array.ConvertAll(regs, r => $"0x{r:X8}"))
        : string.Empty;

        return $"PC: 0x{pc:X8} | Instr: 0x{instruction:X8} | ALU: 0x{alu_out:X8}\n" +
            $"Regs: [{regHex}]\n" +
            $"Reg Data-> rs1_val: 0x{data_rs1:X8}, rs2_val: 0x{data_rs2:X8}\n" +
            $"Signals -> eq: 0x{br_eq:X}, lt: 0x{br_lt:X}, un: 0x{br_un:X}, taken: 0x{br_taken:X} | bc_sel1: 0x{branch_comp_data1_sel:X}, bc_sel2: 0x{branch_comp_data2_sel:X} | pc_sel: 0x{pc_sel:X}, a_sel: 0x{a_sel:X}, b_sel: 0x{b_sel:X}, alu_sel: 0x{alu_sel:X1}, mem_rw: 0x{mem_rw:X}, reg_wen: 0x{reg_wen:X}, wb_sel: 0x{wb_sel:X}\n" +
            $"Decode  -> rd: 0x{addr_rd:X2}, rs1: 0x{addr_rs1:X2}, rs2: 0x{addr_rs2:X2}, f3: 0x{funct3:X1}, f7: 0x{funct7:X2}, imm: 0x{imm:X8}, shamt: 0x{shamt:X2} | types(u/j/i): {is_u_type_w}/{is_j_type_w}/{is_i_type_w}\n" +
            $"Memory  -> imem_out: 0x{imem_data_out:X8}, dmem_in: 0x{dmem_data_in:X8}, dmem_out: 0x{dmem_data_out:X8}\n" +
            $"Pipeline-> wb_data: 0x{wb_data:X8}, wb_in_alu: 0x{wb_in_alu:X8}, mem: 0x{mem:X8}, pc4: 0x{mw_pc4:X8}";
    }
}

// output structs after each corresponding enable signal is high
// returned in the get_${stage} functions
public struct Fetch
{
    public uint pc;

    public Fetch(uint pc)
    {
        this.pc = pc;
    }

    public override string ToString()
    {
        return $"Fetch {{ pc = 0x{pc:X8} }}";
    }
}

public struct Fd
{
    public uint fd_pc;
    public uint fd_pc4;
    public uint instruction;

    public Fd(uint fd_pc, uint fd_pc4, uint instruction)
    {
        this.fd_pc = fd_pc;
        this.fd_pc4 = fd_pc4;
        this.instruction = instruction;
    }

    public override string ToString()
    {
        return $"Fd {{ fd_pc = 0x{fd_pc:X8}, fd_pc = 0x{fd_pc4:X8}, instruction = 0x{instruction:X8} }}";
    }
}

public struct Dx
{
    public uint addr_rs1;
    public uint addr_rs2;

    public Dx(uint addr_rs1, uint addr_rs2)
    {
        this.addr_rs1 = addr_rs1;
        this.addr_rs2 = addr_rs2;
    }
}

public struct Xm
{
    public uint alu_out;
    public uint dmem_data_in;

    public Xm(uint alu_out, uint dmem_data_in)
    {
        this.alu_out = alu_out;
        this.dmem_data_in = dmem_data_in;
    }
}

public struct Mw
{
    public uint pc4;
    public uint wb_in_alu;
    public uint mem;

    public Mw(uint pc4, uint wb_in_alu, uint mem)
    {
        this.pc4 = pc4;
        this.wb_in_alu = wb_in_alu;
        this.mem = mem;
    }
}
enum Operation : byte
{
    ADD = 0,
    SUB = 1,
    SLL = 2,
    SRL = 4,
    SLT = 5,
    SLTU = 6,
    XOR = 7,
    OR = 8,
    AND = 9,
    NOP = 10,
}

// Bridge CPU interface to abstract FPGA/Verilator backend from Unity.
public interface ICPU
{
    // CPUState struct representing all module outputs.
    CPUState state { get; } // TODO make this var name uppercase
    // Tick advances the clock to mark the end of a CPU cycle.
    void Tick();
    // writeIMem loads a hex-encoded program file into instruction memory.
    void writeIMem(string path);
    // writeIMem loads already-assembled hex instructions directly into instruction memory.
    void writeIMem(string[] hexInstructions);
    // Reset re-initializes the CPU to a clean post-reset state, ready to run from address 0x01000000.
    void Reset();
    // PrintState outputs the `state` for debugging.
    void PrintState();
    // GetState() retrieves `state` from CPU.
    CPUState GetState();
    // DON'T USE (deprecated):  retrieve the PC from the appropriate pipeline stage struct
    uint GetPC();
    // DON'T USE (deprecated):  retrieve the Instruction from the appropriate pipeline stage struct
    uint GetInstruction();
    // DON'T USE (deprecated):  retrieve the ALU output from the appropriate pipeline stage struct
    uint GetALUOut();
    // GetFetch retrieves a snapshot of Fetch stage outputs.
    Fetch GetFetch();
    // GetFd retrieves a snapshot of Fd stage outputs.
    Fd GetFd();
    // GetDx retrieves a snapshot of Dx stage outputs.
    Dx GetDx();
    // GetXm retrieves a snapshot of Xm stage outputs.
    Xm GetXm();
    // GetMw retrieves a snapshot of Mw stage outputs.
    Mw GetMw();
}

public static class CPUFactory
{
    // We use an Enum instead of a string to avoid typos.
    public enum ImplementationType { Verilator, FPGA }

    public static ICPU Create(string levelPath, ImplementationType type = ImplementationType.Verilator)
    {
        return type switch
        {
            ImplementationType.Verilator => new VerilatorClient(levelPath),
            ImplementationType.FPGA => new FPGAClient(levelPath),
            _ => throw new System.ArgumentException("Invalid Implementation Type")
        };
    }

    // Create an ICPU from already-assembled hex instructions rather than a file path.
    public static ICPU Create(string[] hexInstructions, ImplementationType type = ImplementationType.Verilator)
    {
        return type switch
        {
            ImplementationType.Verilator => new VerilatorClient(hexInstructions),
            ImplementationType.FPGA => new FPGAClient(hexInstructions),
            _ => throw new System.ArgumentException("Invalid Implementation Type")
        };
    }
}
