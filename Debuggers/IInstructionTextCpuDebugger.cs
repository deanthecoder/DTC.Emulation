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
/// Optional debugger extension that receives CPU-supplied instruction text.
/// This avoids forcing all debugger implementations to understand instruction formatting.
/// </summary>
public interface IInstructionTextCpuDebugger : ICpuDebugger
{
    /// <summary>
    /// Called before an instruction executes, including optional CPU-provided text.
    /// </summary>
    /// <param name="cpu">CPU issuing the callback.</param>
    /// <param name="opcodeAddress">Address of the instruction word.</param>
    /// <param name="opcode">Instruction opcode value. For 8-bit CPUs use the low byte.</param>
    /// <param name="instructionText">Optional resolved instruction text for display.</param>
    void BeforeInstruction(CpuBase cpu, uint opcodeAddress, ushort opcode, string instructionText);
}
