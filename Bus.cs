// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.
using DTC.Emulation.Devices;

namespace DTC.Emulation;

/// <summary>
/// Minimal bus with device mapping for memory and ports.
/// </summary>
public sealed class Bus
{
    private readonly List<IMemDevice> m_devices = [];
    private readonly IPortDevice m_portDevice;

    public Bus(int byteSize, IPortDevice portDevice = null)
        : this(new Memory(byteSize), portDevice)
    {
    }

    public Bus(Memory memory, IPortDevice portDevice = null)
    {
        MainMemory = memory ?? throw new ArgumentNullException(nameof(memory));
        if (MainMemory.ToAddr < MainMemory.FromAddr)
            throw new ArgumentOutOfRangeException(nameof(memory), "Memory address range is invalid.");

        m_portDevice = portDevice;
        MaxAddress = MainMemory.ToAddr;
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

        m_devices.Add(device);
    }

    public byte Read8(uint address)
    {
        if (address > MaxAddress)
            return 0xFF;

        var device = FindDevice(address);
        return device?.Read8(address) ?? 0xFF;
    }

    public void Write8(uint address, byte value)
    {
        if (address > MaxAddress)
            return;

        var device = FindDevice(address);
        device?.Write8(address, value);
    }

    /// <summary>
    /// Reads a 16-bit value in little-endian order.
    /// </summary>
    public ushort Read16(uint address)
    {
        var lo = Read8(address);
        var hi = Read8(address + 1);
        return (ushort)((hi << 8) | lo);
    }

    /// <summary>
    /// Writes a 16-bit value in little-endian order.
    /// </summary>
    public void Write16(uint address, ushort value)
    {
        Write8(address, (byte)(value & 0xFF));
        Write8(address + 1, (byte)(value >> 8));
    }

    /// <summary>
    /// Reads a 16-bit value in big-endian order.
    /// </summary>
    public ushort Read16BigEndian(uint address)
    {
        var hi = Read8(address);
        var lo = Read8(address + 1);
        return (ushort)((hi << 8) | lo);
    }

    /// <summary>
    /// Writes a 16-bit value in big-endian order.
    /// </summary>
    public void Write16BigEndian(uint address, ushort value)
    {
        Write8(address, (byte)(value >> 8));
        Write8(address + 1, (byte)(value & 0xFF));
    }

    /// <summary>
    /// Reads a 24-bit value in big-endian order.
    /// </summary>
    public uint Read24BigEndian(uint address)
    {
        var b0 = Read8(address);
        var b1 = Read8(address + 1);
        var b2 = Read8(address + 2);
        return (uint)((b0 << 16) | (b1 << 8) | b2);
    }

    /// <summary>
    /// Writes a 24-bit value in big-endian order.
    /// </summary>
    public void Write24BigEndian(uint address, uint value)
    {
        Write8(address, (byte)((value >> 16) & 0xFF));
        Write8(address + 1, (byte)((value >> 8) & 0xFF));
        Write8(address + 2, (byte)(value & 0xFF));
    }

    /// <summary>
    /// Reads a 32-bit value in big-endian order.
    /// </summary>
    public uint Read32BigEndian(uint address)
    {
        var b0 = Read8(address);
        var b1 = Read8(address + 1);
        var b2 = Read8(address + 2);
        var b3 = Read8(address + 3);
        return (uint)((b0 << 24) | (b1 << 16) | (b2 << 8) | b3);
    }

    /// <summary>
    /// Writes a 32-bit value in big-endian order.
    /// </summary>
    public void Write32BigEndian(uint address, uint value)
    {
        Write8(address, (byte)((value >> 24) & 0xFF));
        Write8(address + 1, (byte)((value >> 16) & 0xFF));
        Write8(address + 2, (byte)((value >> 8) & 0xFF));
        Write8(address + 3, (byte)(value & 0xFF));
    }
    
    public byte ReadPort(ushort portAddress) =>
        m_portDevice?.Read8(portAddress) ?? 0xFF;

    public void WritePort(ushort portAddress, byte value) =>
        m_portDevice?.Write8(portAddress, value);

    private IMemDevice FindDevice(uint address)
    {
        for (var i = m_devices.Count - 1; i >= 0; i--)
        {
            var device = m_devices[i];
            if (address >= device.FromAddr && address <= device.ToAddr)
                return device;
        }

        return null;
    }
}
