// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

using DTC.Core;

namespace DTC.Emulation.Debuggers;

/// <summary>
/// Captures a rolling instruction trace from CPU debugger callbacks.
/// </summary>
public sealed class InstructionTraceDebugger : IInstructionTextCpuDebugger
{
    private readonly Lock m_sync = new();
    private readonly CircularBuffer<string> m_traceBuffer;
    private readonly Func<CpuBase, uint, ushort, string, string> m_formatter;
    private bool m_hasPendingInstruction;
    private uint m_pendingOpcodeAddress;
    private ushort m_pendingOpcode;
    private string m_pendingInstructionText;

    /// <summary>
    /// Creates a fixed-size rolling trace collector.
    /// A small ring buffer keeps recent context without flooding test output.
    /// </summary>
    public InstructionTraceDebugger(
        int capacity = 512,
        Func<CpuBase, uint, ushort, string, string> formatter = null)
    {
        if (capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");

        m_traceBuffer = new CircularBuffer<string>(capacity);
        m_formatter = formatter ?? DefaultFormat;
    }

    /// <summary>
    /// Creates a fixed-size rolling trace collector using legacy formatter signature.
    /// </summary>
    public InstructionTraceDebugger(
        int capacity,
        Func<CpuBase, uint, ushort, string> formatter)
        : this(capacity, formatter == null ? null : (cpu, opcodeAddress, opcode, _) => formatter(cpu, opcodeAddress, opcode))
    {
    }

    /// <summary>
    /// Enables or disables capture at runtime.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc />
    public void BeforeInstruction(CpuBase cpu, uint opcodeAddress, ushort opcode) =>
        BeforeInstruction(cpu, opcodeAddress, opcode, null);

    /// <inheritdoc />
    public void BeforeInstruction(CpuBase cpu, uint opcodeAddress, ushort opcode, string instructionText)
    {
        if (!IsEnabled)
            return;

        lock (m_sync)
        {
            m_hasPendingInstruction = true;
            m_pendingOpcodeAddress = opcodeAddress;
            m_pendingOpcode = opcode;
            m_pendingInstructionText = instructionText;
        }
    }

    /// <inheritdoc />
    public void AfterStep(CpuBase cpu)
    {
        if (!IsEnabled)
            return;

        lock (m_sync)
        {
            if (!m_hasPendingInstruction)
                return;

            var line = m_formatter(cpu, m_pendingOpcodeAddress, m_pendingOpcode, m_pendingInstructionText);
            m_hasPendingInstruction = false;
            m_pendingInstructionText = null;
            if (string.IsNullOrWhiteSpace(line))
                return;

            m_traceBuffer.Write(line);
        }
    }

    /// <inheritdoc />
    public void OnMemoryRead(CpuBase cpu, uint address, byte value) { }

    /// <inheritdoc />
    public void OnMemoryWrite(CpuBase cpu, uint address, byte value) { }

    /// <summary>
    /// Returns a snapshot of recent trace lines in execution order (oldest to newest).
    /// </summary>
    public IReadOnlyList<string> GetRecentLines(int maxLines = -1)
    {
        lock (m_sync)
        {
            if (m_traceBuffer.Count == 0)
                return [];

            var takeCount = maxLines < 0 ? m_traceBuffer.Count : Math.Min(maxLines, m_traceBuffer.Count);
            var skipCount = m_traceBuffer.Count - takeCount;
            var items = new List<string>(takeCount);
            var index = 0;
            foreach (var line in m_traceBuffer)
            {
                if (index < skipCount)
                {
                    index++;
                    continue;
                }

                items.Add(line);
                index++;
            }

            return items;
        }
    }

    /// <summary>
    /// Clears captured trace entries.
    /// </summary>
    public void Clear()
    {
        lock (m_sync)
        {
            m_traceBuffer.Clear();
            m_hasPendingInstruction = false;
            m_pendingInstructionText = null;
        }
    }

    private static string DefaultFormat(CpuBase cpu, uint opcodeAddress, ushort opcode, string instructionText)
    {
        var mnemonic = string.IsNullOrWhiteSpace(instructionText)
            ? string.Empty
            : $" {instructionText}";
        return $"PC=0x{opcodeAddress:X8} OPCODE=0x{opcode:X4}{mnemonic}";
    }
}
