/*
 * AUTO-GENERATED from bridge/VerilatorClientSuperscalar.cs — DO NOT EDIT DIRECTLY.
 * Edit the source in bridge/ and re-run `make sync-unity` from the bridge/ directory.
 */
using UnityEngine;
using System;
using System.IO;
using System.Runtime.InteropServices;

public class VerilatorClientSuperscalar : IDisposable, ICPU
{
    private const string NativeLib = "design_wrapper_ss"; // updated to reference superscalar design
    // Imports the function from our compiled shared library
    // required for each external function
    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void init_design_wrapper();

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    
    public static extern void finish_reset();

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void tick();

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void eval();

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void get_cpu_state(out CPUState state, int way); // updated to take in way as an argument

    [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl)]
    public static extern void clear_imem();

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

    /*
     * Tick - Advances the simulated clock.
     */
    public void Tick()
    {
        tick();
        return;
    }

    /*
     * writeIMem — Load a hex-encoded program file into instruction memory.
     *
     * Reads each line of the file at `path`, strips comments and optional "0x"
     * prefixes, parses the remaining text as a 32-bit hex instruction, and writes
     * it to imem via set_imem() starting at the base address 0x01000000.
     * Blank lines and comment-only lines are skipped.
     *
     * @param path  Path to the hex file (one instruction word per line).
     */
    public void writeIMem(string path)
    {
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
            Debug.Log($"Error loading hex file: {ex.Message}");
        }
    }

        /*
     * writeIMem — Load already-assembled hex instructions directly into instruction memory.
     *
     * Writes each hex string (optionally "0x"-prefixed) to imem via set_imem(),
     * starting at the base address 0x01000000. No file I/O; callers assemble the
     * program themselves (e.g. from level JSON) and pass the hex words directly.
     *
     * @param hexInstructions  Array of 32-bit hex-encoded instruction words.
     */
    public void writeIMem(string[] hexInstructions)
    {
        clear_imem();
        uint currentAddr = 0x01000000; // Base addr of imem
        foreach (string hex in hexInstructions)
        {
            string cleanHex = hex.StartsWith("0x") ? hex.Substring(2) : hex;
            uint instruction = uint.Parse(cleanHex, System.Globalization.NumberStyles.HexNumber);
            set_imem(currentAddr, instruction);
            currentAddr += 4;
        }
        Debug.Log($"Successfully loaded {hexInstructions.Length} instructions into IMEM.");
    }

        /*
     * Reset — Re-initialize the CPU to a clean post-reset state.
     *
     * Calls finish_reset() to complete the reset sequence started by
     * init_design_wrapper(), leaving the design ready to run from imem.
     */
    public void Reset()
    {
        finish_reset();
    }

    /*
     * PrintState — Print a human-readable summary of key CPU signals to stdout.
     *

     * Calls GetState() internally to refresh state.
     * To access fields of full signal snapshot use GetState().
     */
    public void PrintState(int way = 0)
    {
        GetState(way);
        Debug.Log(this.state);
    }

    /*
     * GetState — Return a full snapshot of every observable CPU signal.
     *
     * Calls get_cpu_state() to refresh and returns the raw CPUState struct.
     * Prefer the typed stage helpers (GetFetch, GetFd, …) when only one
     * pipeline stage's signals are needed.
     *
     * @return  A CPUState struct populated with the current simulation state.
     */
    public CPUState GetState(int way = 0)
    {
        CPUState state;
        get_cpu_state(out state, way);
        this.state = state;
        return this.state;
    }

    /*
     * GetPC — Return the current program counter value.
     *
     * @return  The pc field from the refreshed CPUState.
     */
    public uint GetPC(int way = 0)
    {
        GetState(way);
        return this.state.pc;
    }

    /*
     * GetInstruction — Return the instruction word currently in the Fetch/Decode register.
     *
     * @return  The instruction field from the refreshed CPUState.
     */
    public uint GetInstruction(int way = 0)
    {
        GetState(way);
        return this.state.instruction;
    }

    /*
     * GetALUOut — Return the ALU result register value (alu_xm_r).
     *
     * @return  The alu_out field from the refreshed CPUState.
     */
    public uint GetALUOut(int way = 0)
    {
        GetState(way);
        return this.state.alu_out;
    }

    /*
     * GetFetch — Return a typed snapshot of the Fetch stage outputs.
     *
     * @return  A Fetch record containing the current PC.
     */
    public Fetch GetFetch(int way = 0)
    {
        GetState(way);
        return new Fetch
        {
            pc = this.state.pc
        };
    }

    /*
     * GetFd — Return a typed snapshot of the Fetch→Decode register contents.
     *
     * @return  An Fd record containing fd_pc, fd_pc4, and the instruction word.
     */
    public Fd GetFd(int way = 0)
    {
        GetState(way);
        return new Fd
        {
            fd_pc = this.state.fd_pc,
            fd_pc4 = this.state.fd_pc4,
            instruction = this.state.instruction
        };
    }

    /*
     * GetDx — Return a typed snapshot of the Decode→Execute register contents.
     *
     * @return  A Dx record containing the source register addresses (addr_rs1, addr_rs2).
     */
    public Dx GetDx(int way = 0)
    {
        GetState(way);
        return new Dx
        {
            addr_rs1 = this.state.addr_rs1,
            addr_rs2 = this.state.addr_rs2,
        };
    }

    /*
     * GetXm — Return a typed snapshot of the Execute→Memory register contents.
     *
     * @return  An Xm record containing alu_out and the data memory write value (dmem_data_in).
     */
    public Xm GetXm(int way = 0)
    {
        GetState(way);
        return new Xm
        {
            alu_out = this.state.alu_out,
            dmem_data_in = this.state.dmem_data_in
        };
    }

    /*
     * GetMw — Return a typed snapshot of the Memory→Writeback register contents.
     *
     * @return  An Mw record containing pc4, wb_in_alu, and the memory read value (mem).
     */
    public Mw GetMw(int way = 0)
    {
        GetState(way);
        return new Mw
        {
            pc4 = this.state.mw_pc4,
            wb_in_alu = this.state.wb_in_alu,
            mem = this.state.mem
        };
    }

    public CPUState state { get; private set; }
    private bool disposed = false;

    /*
     * VerilatorClient — Initialize the Verilated model and load a program into instruction memory.
     *
     * Calls init_design_wrapper() to reset the simulation, then writeIMem() to
     * populate imem from the hex file at `path`.
     *
     * @param path  Path to the hex program file (see writeIMem for format details).
     */
    public VerilatorClientSuperscalar(string path)
    {
        this.state = new CPUState();
        init_design_wrapper();
        writeIMem(path);
    }

    /*
     * VerilatorClient — Initialize the Verilated model and load already-assembled hex instructions.
     *
     * Calls init_design_wrapper() to reset the simulation, then writeIMem() to
     * populate imem directly from the given hex words.
     *
     * @param hexInstructions  Array of 32-bit hex-encoded instruction words.
     */
    public VerilatorClientSuperscalar(string[] hexInstructions)
    {
        this.state = new CPUState();
        init_design_wrapper();
        writeIMem(hexInstructions);
        Reset();
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
        if (!this.disposed)
        {
            // If disposing equals true, dispose all managed
            // and unmanaged resources.
            if (disposing)
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
    ~VerilatorClientSuperscalar()
    {
        // Do not re-create Dispose clean-up code here.
        // Calling Dispose(disposing: false) is optimal in terms of
        // readability and maintainability.
        Dispose(disposing: false);
    }
}
