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
/// Exposes frame buffer output for an emulated video device.
/// </summary>
public interface IVideoSource
{
    int FrameWidth { get; }
    int FrameHeight { get; }
    
    /// <summary>
    /// Gets the number of bytes per pixel in the frame buffer exposed by this source.
    /// </summary>
    int FrameBytesPerPixel { get; }
    
    event EventHandler<byte[]> FrameRendered;
    void CopyToFrameBuffer(Span<byte> frameBuffer);
}
