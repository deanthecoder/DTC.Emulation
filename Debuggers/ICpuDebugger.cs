// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any
// purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.
namespace DTC.Emulation.Debuggers;

/// <summary>
/// Receives CPU debugging callbacks before and after instructions.
/// </summary>
public interface ICpuDebugger
{
    /// <summary>
    /// Called before an instruction executes.
    /// </summary>
    /// <param name="cpu">CPU issuing the callback.</param>
    /// <param name="opcodeAddress">Address of the instruction word.</param>
    /// <param name="opcode">Instruction opcode value. For 8-bit CPUs use the low byte.</param>
    void BeforeInstruction(CpuBase cpu, uint opcodeAddress, ushort opcode);

    void AfterStep(CpuBase cpu);
    void OnMemoryRead(CpuBase cpu, uint address, byte value);
    void OnMemoryWrite(CpuBase cpu, uint address, byte value);
}
