// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.
using System.Runtime.CompilerServices;
using DTC.Emulation.Snapshot;

namespace DTC.Emulation.Devices;

/// <summary>
/// Simple linear RAM device.
/// </summary>
/// <remarks>
/// Provides flat read/write storage for the full address space by default.
/// </remarks>
public sealed class Memory : IMemDevice
{
    private readonly uint m_length;
    
    public byte[] Data { get; }
    public uint FromAddr { get; }
    public uint ToAddr { get; }

    public Memory(uint size = 0x10000)
    {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size));

        Data = new byte[size];
        m_length = size;
        FromAddr = 0x00000000;
        ToAddr = size - 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte Read8(uint address) => Data[GetIndex(address)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write8(uint address, byte value) => Data[GetIndex(address)] = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint GetIndex(uint address) =>
        address < m_length ? address : address % m_length;

    public int GetStateSize() => Data.Length;

    public void SaveState(ref StateWriter writer) =>
        writer.WriteBytes(Data);

    public void LoadState(ref StateReader reader) =>
        reader.ReadBytes(Data);
}
