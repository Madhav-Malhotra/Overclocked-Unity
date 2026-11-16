/*
 * AUTO-GENERATED from bridge/FPGAClient.cs — DO NOT EDIT DIRECTLY.
 * Edit the source in bridge/ and re-run `make sync-unity` from the bridge/ directory.
 */
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;

public class FPGAClient : IDisposable, ICPU
{
    public CPUState state { get; private set; }
    private bool disposed = false;
    private static readonly HttpClient client;
    /*
     * makeRequest - utility function to make an HTTP request to the webserver.
     *
     * The mapping of endpoint to GET/POST request is hardcoded in the switch statement.
     *
     * @param endpoint  API endpoint to call
     * @param json      JSON payload used in POST requests
     * @return          JSON string containing the webserver's response
     */
    private string makeRequest(string endpoint, string json = "")
    {
        HttpRequestMessage req;
        HttpResponseMessage res = new HttpResponseMessage();
        try
        {
            switch (endpoint)
            {
                // GET endpoints
                case "/outputs/status":
                    req = new HttpRequestMessage(HttpMethod.Get, endpoint);
                    res = client.SendAsync(req).GetAwaiter().GetResult();
                    break;
                // POST endpoints
                case "/tick":
                case "/imem/update":
                    req = new HttpRequestMessage(HttpMethod.Post, endpoint);
                    req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    res = client.SendAsync(req).GetAwaiter().GetResult();
                    break;
                default:
                    Debug.Log($"unsupported endpoint: {endpoint}");
                    break;
            }
            res.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Debug.Log($"API Error on {endpoint}: {ex.Message}");
            return string.Empty;
        }
        return res.Content.ReadAsStringAsync().Result;
    }


    /*
     * Tick - Advances the clock.
     *
     */
    public void Tick()
    {
        makeRequest("/tick");
        return;
    }

    /*
     * writeIMem - Load a hex-encoded program file into instruction memory.
     *
     * Reads each line of the file at `path`, strips comments and optional "0x"
     * prefixes, parses the remaining text as a 32-bit hex instruction.
     * Calls the /imem/update endpoint with the list of 32-bit hex instructions as JSON content.
     * Blank lines and comment-only lines are skipped.
     *
     * @param path  Path to the hex file (one instruction word per line).
     */
    public void writeIMem(string path)
    {
        List<uint> instructions = new List<uint>();
        try
        {
            string[] lines = File.ReadAllLines(path);
            foreach (string line in lines)
            {
                string cleanLine = line.Split("//")[0].Trim();
                if (string.IsNullOrEmpty(cleanLine)) continue;
                if (cleanLine.StartsWith("0x")) cleanLine = cleanLine.Substring(2); // cut off 0x
                // Parse hex string to 32-bit uint
                uint instruction = uint.Parse(cleanLine, System.Globalization.NumberStyles.HexNumber);
                instructions.Add(instruction);
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"Error loading hex file: {ex.Message}");

        }
        Dictionary<string, List<uint>> args = new() { ["mem"] = instructions };
        string argsJson = JsonConvert.SerializeObject(args);
        //makeRequest("/imem/update", argsJson);
        makeRequest("/imem/update"); // TODO (diana) temp. hardcoding the imem start up configuration
        return;
    }

    /*
     * writeIMem - Load already-assembled hex instructions directly into instruction memory.
     *
     * TODO (diana): the /imem/update endpoint used by writeIMem(string path) assumes a file was
     * read locally and its hex words assembled into a List<uint> before this call. It's unconfirmed
     * whether the same endpoint accepts pre-assembled hex directly from the caller, or whether a new
     * endpoint is needed. Confirm with Diana; until then this overload is a stub to satisfy ICPU.
     *
     * @param hexInstructions  Array of 32-bit hex-encoded instruction words.
     */
    public void writeIMem(string[] hexInstructions)
    {
        makeRequest("/imem/update"); // TODO (diana) temp. hardcoding the imem start up configuration
        return;
    }

    /*
     * Reset - Re-initialize the CPU to a clean post-reset state.
     *
     * TODO (diana): the webserver protocol has no known reset endpoint today
     * (unlike Verilator's finish_reset()/clear_imem() P/Invoke calls). Confirm
     * with Diana whether one exists or needs to be added; until then this is a
     * stub to satisfy ICPU.
     */
    public void Reset()
    {
        Debug.Log("NOT IMPLEMENTED");
        return;
    }

    /*
     * PrintState — Print a human-readable summary of key CPU signals to stdout.
     *
     * Calls GetState() internally to refresh state.
     */
    public void PrintState(int way = 0)
    {
        GetState();
        Debug.Log(this.state);
        return;
    }

    /*
     * GetState — Return a full snapshot of CPU signals in CPUState.
     *
     * Makes /outputs/status request to refresh and returns the raw CPUState struct.
     * Prefer the typed stage helpers (GetFetch, GetFd, …) when only one
     * pipeline stage's signals are needed.
     *
     * @return  A CPUState struct populated with the current simulation state.
     */
    public CPUState GetState(int way = 0)
    {
        string res = makeRequest("/outputs/status");
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new JsonBooleanToByteConverter());
        this.state = JsonConvert.DeserializeObject<CPUState>(res, settings);
        return this.state;
    }

    /*
     * GetPC — Return the current program counter value.
     *
     * @return  The pc field from the refreshed CPUState.
     */
    public uint GetPC(int way = 0)
    {
        return GetState().pc;
    }

    /*
     * GetInstruction — Return the instruction word currently in the Fetch/Decode register.
     *
     * @return  The instruction field from the refreshed CPUState.
     */
    public uint GetInstruction(int way = 0)
    {
        return GetState().instruction;
    }

    /*
     * GetALUOut — Return the ALU result register value (alu_xm_r).
     *
     * @return  The alu_out field from the refreshed CPUState.
     */
    public uint GetALUOut(int way = 0)
    {
        return GetState().alu_out;
    }

    /*
     * GetFetch — Return a typed snapshot of the Fetch stage outputs.
     *
     * @return  A Fetch record containing the current PC.
     */
    public Fetch GetFetch(int way = 0)
    {
        return new Fetch
        {
            pc = this.state.pc,
        };
    }
    /*
     * GetFd — Return a typed snapshot of the Fetch→Decode register contents.
     *
     * @return  An Fd record containing fd_pc, fd_pc4, and the instruction word.
     */
    public Fd GetFd(int way = 0)
    {
        return new Fd
        {
            fd_pc = this.state.fd_pc,
            fd_pc4 = this.state.fd_pc4,
            instruction = this.state.instruction,
        };
    }

    /*
     * GetDx — Return a typed snapshot of the Decode→Execute register contents.
     *
     * @return  A Dx record containing the source register addresses (addr_rs1, addr_rs2).
     */
    public Dx GetDx(int way = 0)
    {
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
        return new Xm
        {
            alu_out = this.state.alu_out,
            dmem_data_in = this.state.dmem_data_in,
        };
    }

    /*
     * GetMw — Return a typed snapshot of the Memory→Writeback register contents.
     *
     * @return  An Mw record containing pc4, wb_in_alu, and the memory read value (mem).
     */
    public Mw GetMw(int way = 0)
    {
        return new Mw
        {
            pc4 = this.state.mw_pc4,
            wb_in_alu = this.state.wb_in_alu,
        };
    }

    /*
     * FPGAClient - static constructor to initialize the HTTPClient reused for all web requests
     */
    static FPGAClient()
    {
        // Hardcoded for simplicity for now. Should be set via in-game config in future
        string host = "localhost";
        string port = "8080";
        client = new HttpClient
        {
            BaseAddress = new Uri($"http://{host}:{port}")
        };
    }

    /*
     * FPGAClient - Load a program into instruction memory with an /imem/update API call and initialize CPU state.
     *
     * @param path Path to the hex program file.
     */
    public FPGAClient(string path)
    {
        this.state = new CPUState();
        writeIMem(path);
        this.state = GetState();
        Debug.Log(this.state);
    }

    /*
     * FPGAClient - Load already-assembled hex instructions into instruction memory and initialize CPU state.
     *
     * @param hexInstructions  Array of 32-bit hex-encoded instruction words.
     */
    public FPGAClient(string[] hexInstructions)
    {
        this.state = new CPUState();
        writeIMem(hexInstructions);
        this.state = GetState();
        Debug.Log(this.state);
    }

    // Implement IDisposable.
    // Diposable interface from https://learn.microsoft.com/en-us/dotnet/api/system.idisposable?view=net-10.0
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            disposed = true;
        }
    }
    ~FPGAClient()
    {
        Dispose(disposing: false);
    }

    /* JsonBooleanToByteConverter for conversion because the webserver stores JSON types (bool) but CPUState is explicitly defined in bytes and uint32 for alignment.
     * Without this conversion, we will encounter
     * > The JSON value could not be converted to System.Byte.
     * > Cannot get the value of a token type 'True' as a number.
     */
    private class JsonBooleanToByteConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(byte);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // If the JSON value is boolean 'true'/'false', map to 1/0
            if (reader.TokenType == JsonToken.Boolean) return (bool)reader.Value ? (byte)1 : (byte)0;
            // If it's already a standard number, read it normally
            if (reader.TokenType == JsonToken.Integer) return Convert.ToByte(reader.Value);
            throw new JsonSerializationException($"Unexpected token type {reader.TokenType} when converting to byte.");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // When sending data back, write it as a normal number
            writer.WriteValue((byte)value);
        }
    }
}
