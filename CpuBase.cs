// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.
using System.Diagnostics;
using DTC.Emulation.Debuggers;

namespace DTC.Emulation;

/// <summary>
/// Base CPU type that provides bus access and debugger notifications.
/// </summary>
public abstract class CpuBase
{
    private readonly List<ICpuDebugger> m_debuggers = [];
    private bool m_hasInstructionTextDebuggerCandidates;

    protected CpuBase(Bus bus)
    {
        Bus = bus ?? throw new ArgumentNullException(nameof(bus));
    }

    public Bus Bus { get; }

    public IReadOnlyCollection<ICpuDebugger> Debuggers => m_debuggers.AsReadOnly();

    public void AddDebugger(ICpuDebugger debugger)
    {
        if (debugger == null)
            throw new ArgumentNullException(nameof(debugger));

        m_debuggers.Add(debugger);
        if (debugger is IInstructionTextCpuDebugger)
            m_hasInstructionTextDebuggerCandidates = true;
    }

    public abstract void Reset();
    public abstract void Step();
    public abstract byte Read8(uint address);
    public abstract void Write8(uint address, byte value);

    /// <summary>
    /// Gets whether one or more attached debuggers currently require resolved instruction text.
    /// </summary>
    protected bool HasInstructionTextDebugger
    {
        get
        {
            if (!m_hasInstructionTextDebuggerCandidates)
                return false;

            foreach (var debugger in m_debuggers)
            {
                if (debugger is not IInstructionTextCpuDebugger textDebugger)
                    continue;
                if (textDebugger.WantsInstructionText)
                    return true;
            }

            return false;
        }
    }

    [Conditional("DEBUG")]
    protected void NotifyBeforeInstruction(uint opcodeAddress, ushort opcode, string instructionText = null)
    {
        if (m_debuggers.Count == 0)
            return;

        foreach (var debugger in m_debuggers)
        {
            if (debugger is IInstructionTextCpuDebugger instructionTextDebugger)
                instructionTextDebugger.BeforeInstruction(this, opcodeAddress, opcode, instructionText);
            else
                debugger.BeforeInstruction(this, opcodeAddress, opcode);
        }
    }

    [Conditional("DEBUG")]
    protected void NotifyAfterStep()
    {
        if (m_debuggers.Count == 0)
            return;

        foreach (var debugger in m_debuggers)
            debugger.AfterStep(this);
    }

    [Conditional("DEBUG")]
    protected void NotifyMemoryRead(uint address, byte value)
    {
        if (m_debuggers.Count == 0)
            return;

        foreach (var debugger in m_debuggers)
            debugger.OnMemoryRead(this, address, value);
    }

    [Conditional("DEBUG")]
    protected void NotifyMemoryWrite(uint address, byte value)
    {
        if (m_debuggers.Count == 0)
            return;

        foreach (var debugger in m_debuggers)
            debugger.OnMemoryWrite(this, address, value);
    }
}
