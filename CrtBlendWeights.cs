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
/// Weights used when blending the current CRT output with the previous frame.
/// Higher previous-frame values create more persistence/motion blur.
/// </summary>
public readonly record struct CrtBlendWeights(byte PreviousFrameWeight, byte CurrentFrameWeight)
{
    public static CrtBlendWeights Default => new(3, 2);

    /// <summary>
    /// Returns true when at least one source contributes to the blended output.
    /// </summary>
    public bool IsValid => PreviousFrameWeight + CurrentFrameWeight > 0;
}

