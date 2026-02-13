// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

namespace DTC.Emulation;

/// <summary>
/// Represents a fully wired emulated machine with CPU, video, audio, and input.
/// </summary>
public interface IMachine
{
    /// <summary>
    /// Gets the user-facing machine name.
    /// This should be stable and human-readable (for example, menu/display text).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets static descriptor metadata (clock rates, display geometry, and identity).
    /// Implement this immediately when adding a machine.
    /// </summary>
    IMachineDescriptor Descriptor { get; }

    /// <summary>
    /// Gets cumulative CPU clock ticks since reset.
    /// This is used by timing/sync code and should increase monotonically while running.
    /// </summary>
    long CpuTicks { get; }

    /// <summary>
    /// Gets whether a ROM/cartridge/program image is currently loaded.
    /// Use this to gate UI actions that require loaded media.
    /// </summary>
    bool HasLoadedCartridge { get; }

    /// <summary>
    /// Gets the machine video source.
    /// Return a concrete implementation when video exists; return null only during early bring-up.
    /// </summary>
    IVideoSource Video { get; }

    /// <summary>
    /// Gets the machine audio source.
    /// Return a concrete implementation when audio exists; return null only during early bring-up.
    /// </summary>
    IAudioSource Audio { get; }

    /// <summary>
    /// Gets snapshot support for save/load state workflows.
    /// Return a concrete implementation when state capture is supported; return null only during early bring-up.
    /// </summary>
    IMachineSnapshotter Snapshotter { get; }

    /// <summary>
    /// Resets the machine to power-on state.
    /// Must reset CPU/device state and timing counters to a clean boot condition.
    /// Implement this from day one.
    /// </summary>
    void Reset();

    /// <summary>
    /// Loads ROM/program bytes into machine memory and prepares execution state.
    /// Typical behavior is: validate image, map/copy bytes, then call <see cref="Reset"/>.
    /// Implement this from day one for ROM-based systems.
    /// </summary>
    /// <param name="romData">Raw ROM/program image bytes.</param>
    /// <param name="romName">Source/display name used for diagnostics.</param>
    void LoadRom(byte[] romData, string romName);

    /// <summary>
    /// Executes one CPU step (one instruction boundary in current core model).
    /// Machine-level interception for platform-specific hooks may occur here before CPU decode.
    /// Implement this from day one.
    /// </summary>
    void StepCpu();

    /// <summary>
    /// Advances non-CPU devices by the supplied CPU tick delta.
    /// Use this to update timers/video/audio and to schedule pending interrupts.
    /// May be a no-op during earliest bring-up, but should be implemented once device timing exists.
    /// </summary>
    /// <param name="deltaTicks">CPU ticks elapsed since the previous machine update.</param>
    void AdvanceDevices(long deltaTicks);

    /// <summary>
    /// Attempts to consume one pending machine interrupt request.
    /// Return true when a device has a pending IRQ to present to the CPU; false otherwise.
    /// Synthetic periodic IRQ generation is acceptable during early bring-up.
    /// </summary>
    /// <returns>True if an interrupt is pending and should be requested from the CPU.</returns>
    bool TryConsumeInterrupt();

    /// <summary>
    /// Requests interrupt delivery to the CPU using the machine's resolved IRQ level.
    /// Typical implementation forwards the chosen level to CPU interrupt latch logic.
    /// Should be implemented together with <see cref="TryConsumeInterrupt"/> once IRQ sources exist.
    /// </summary>
    void RequestInterrupt();

    /// <summary>
    /// Updates machine input-active state (focus/foreground user interaction hint).
    /// Use this to pause/resume input scanning or gate side effects when inactive.
    /// May be a no-op until input devices are implemented.
    /// </summary>
    /// <param name="isActive">True when machine input should be considered active.</param>
    void SetInputActive(bool isActive);
}
