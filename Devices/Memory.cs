// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.
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
    public byte[] Data { get; }
    public uint FromAddr { get; }
    public uint ToAddr { get; }

    public Memory(int size = 0x10000)
    {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size));

        Data = new byte[size];
        FromAddr = 0x00000000;
        ToAddr = (uint)(size - 1);
    }

    public byte Read8(uint address) => Data[GetIndex(address)];

    public void Write8(uint address, byte value) => Data[GetIndex(address)] = value;

    public int GetStateSize() => Data.Length;

    public void SaveState(ref StateWriter writer) =>
        writer.WriteBytes(Data);

    public void LoadState(ref StateReader reader) =>
        reader.ReadBytes(Data);

    private int GetIndex(uint address) =>
        (int)(address % (uint)Data.Length);
}
