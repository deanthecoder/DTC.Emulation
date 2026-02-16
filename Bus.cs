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
using DTC.Emulation.Devices;

namespace DTC.Emulation;

/// <summary>
/// Minimal bus with device mapping for memory and ports.
/// </summary>
public sealed class Bus
{
    private readonly IMemDevice[] m_devices;

    public Bus(uint byteSize)
        : this(new Memory(byteSize))
    {
    }

    public Bus(Memory memory)
    {
        MainMemory = memory ?? throw new ArgumentNullException(nameof(memory));
        if (MainMemory.ToAddr < MainMemory.FromAddr)
            throw new ArgumentOutOfRangeException(nameof(memory), "Memory address range is invalid.");

        MaxAddress = MainMemory.ToAddr;
        var busSize = checked((int)(MaxAddress + 1));
        m_devices = new IMemDevice[busSize];
        Attach(MainMemory);
    }

    public Memory MainMemory { get; }

    public uint MaxAddress { get; }

    public void Attach(IMemDevice device)
    {
        if (device == null)
            throw new ArgumentNullException(nameof(device));

        if (device.ToAddr < device.FromAddr)
            throw new ArgumentOutOfRangeException(nameof(device), "Device address range is invalid.");

        if (device.ToAddr > MaxAddress)
            throw new ArgumentOutOfRangeException(nameof(device), "Device address range is outside bus space.");

        var fromAddress = checked((int)device.FromAddr);
        var mapLength = checked((int)(device.ToAddr - device.FromAddr + 1));
        Array.Fill(m_devices, device, fromAddress, mapLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte Read8(uint address)
    {
        if (address > MaxAddress)
            return 0xFF;

        var device = m_devices[(int)address];
        return device?.Read8(address) ?? 0xFF;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write8(uint address, byte value)
    {
        if (address > MaxAddress)
            return;

        var device = m_devices[(int)address];
        device?.Write8(address, value);
    }

    /// <summary>
    /// Reads a 16-bit value in big-endian order.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Read16BigEndian(uint address)
    {
        var hi = Read8(address);
        var lo = Read8(address + 1);
        return (ushort)((hi << 8) | lo);
    }

    /// <summary>
    /// Writes a 16-bit value in big-endian order.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write16BigEndian(uint address, ushort value)
    {
        Write8(address, (byte)(value >> 8));
        Write8(address + 1, (byte)(value & 0xFF));
    }

    /// <summary>
    /// Reads a 32-bit value in big-endian order.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Read32BigEndian(uint address)
    {
        var b0 = Read8(address);
        var b1 = Read8(address + 1);
        var b2 = Read8(address + 2);
        var b3 = Read8(address + 3);
        return (uint)((b0 << 24) | (b1 << 16) | (b2 << 8) | b3);
    }
}
