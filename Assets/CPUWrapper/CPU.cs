using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CPUState {
    // MUST stay sync'ed with CPUState in C++, since we pass the struct ptr
    // order must match the CPUState definition
    public uint pc;          // Maps to uint32_t
    public uint instruction; // Maps to uint32_t

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    public uint[] regs;
    public uint aluOut;      // Maps to uint32_t
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
}

enum Operation : byte {
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

class CPU : IDisposable
{
    private const string NativeLib = "design_wrapper";

    // Imports the function from our compiled shared library
    // required for each external function
    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void init_design_wrapper();

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tick();

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void eval();

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void get_cpu_state(out CPUState state);

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void set_imem(uint addr, uint instruction);

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint peek_imem(uint addr);

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void dump_imem(uint count);

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void dump_dmem(uint count);

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void cleanup_design_wrapper();

    // WriteIMem puts the file's (located at `path`) contents (assembled instructions) into imem
    public static void writeIMem(string path) {
        try
        {
            string[] lines = File.ReadAllLines(path);
            uint currentAddr = 0x01000000; // Base addr of imem
            foreach (string line in lines)
            {
                // Clean the line (remove comments, whitespace, or '0x' prefix)
                string cleanLine = line.Split("//")[0].Trim(); 
                if (string.IsNullOrEmpty(cleanLine)) continue;
                if (cleanLine.StartsWith("0x")) cleanLine = cleanLine.Substring(2); // cut off 0x
                                                                                    // Parse hex string to 32-bit uint
                uint instruction = uint.Parse(cleanLine, System.Globalization.NumberStyles.HexNumber);
                set_imem(currentAddr, instruction);
                currentAddr += 4;
            }
            Debug.Log($"Successfully loaded {lines.Length} lines into IMEM.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading hex file: {ex.Message}");
        }
    }

    public uint GetALUOut() {
        get_cpu_state(out this.state);
        return this.state.aluOut;
    }

    // PrintState prints the state. Only select fields are shown, but more can be added. This is mostly a debugging function for development
    // use get_cpu_state for the full struct data
    public void PrintState() {
        get_cpu_state(out this.state);
        Debug.Log(
            $"PC: 0x{state.pc,-8:X} | " +
            $"Instr: 0x{state.instruction,-8:X} | " +
            $"ALU: {state.aluOut,-10} | " +
            $"x0: {state.regs[0],-3} | " +
            $"x5: {state.regs[5],-3} | " +
            $"x6: 0x{state.regs[6],-8:X} | " +
            $"x7: {state.regs[7],-3}"
        );
    }

    private CPUState state;
    private bool disposed = false;

    // Constructor (initialized with path to level file)
    public CPU (string path) {
        this.state = new CPUState();
        init_design_wrapper();
        writeIMem(path);
    }

    // Diposable interface from https://learn.microsoft.com/en-us/dotnet/api/system.idisposable?view=net-10.0
    // Implement IDisposable.
    // Do not make this method virtual.
    // A derived class should not be able to override this method.
    public void Dispose()
    {
        Dispose(disposing: true);
        // This object will be cleaned up by the Dispose method.
        // Therefore, you should call GC.SuppressFinalize to
        // take this object off the finalization queue
        // and prevent finalization code for this object
        // from executing a second time.
        GC.SuppressFinalize(this);
    }

    // Dispose(bool disposing) executes in two distinct scenarios.
    // If disposing equals true, the method has been called directly
    // or indirectly by a user's code. Managed and unmanaged resources
    // can be disposed.
    // If disposing equals false, the method has been called by the
    // runtime from inside the finalizer and you should not reference
    // other objects. Only unmanaged resources can be disposed.
    protected virtual void Dispose(bool disposing)
    {
        // Check to see if Dispose has already been called.
        if(!this.disposed)
        {
            // If disposing equals true, dispose all managed
            // and unmanaged resources.
            if(disposing)
            {
                // Dispose managed resources.
                // nothing in this class is currently managed, just the unmanaged bridge memory below
            }
            cleanup_design_wrapper();
            // state stuct will be cleaned up after this function exits, don't set to null

            // Note disposing has been done.
            disposed = true;
        }
    }

    // Use C# finalizer syntax for finalization code.
    // This finalizer will run only if the Dispose method
    // does not get called.
    // It gives your base class the opportunity to finalize.
    // Do not provide finalizer in types derived from this class.
    ~CPU()
    {
        // Do not re-create Dispose clean-up code here.
        // Calling Dispose(disposing: false) is optimal in terms of
        // readability and maintainability.
        Dispose(disposing: false);
    }
}
