// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DTC.Core.Image;
using DTC.Emulation.Image;

namespace DTC.Emulation;

/// <summary>
/// Simple LCD surface that copies RGB frame buffers into a writeable bitmap with optional screen effects.
/// </summary>
public sealed class LcdScreen : ILcdScreen, IDisposable
{
    private readonly FrameBuffer m_previousOutput;
    private CrtBlendWeights m_crtBlendWeights = CrtBlendWeights.Default;
    private bool m_hasPreviousOutput;

    public WriteableBitmap Display { get; }
    public CrtFrameBuffer FrameBuffer { get; }

    public bool IsPaused
    {
        get => FrameBuffer.IsPaused;
        set => FrameBuffer.IsPaused = value;
    }

    /// <summary>
    /// Controls CRT phosphor persistence blending between previous and current frames.
    /// Defaults to a legacy 3:2 blend to preserve existing behavior in other apps.
    /// </summary>
    public CrtBlendWeights CrtBlendWeights
    {
        get => m_crtBlendWeights;
        set
        {
            if (!value.IsValid)
                throw new ArgumentOutOfRangeException(nameof(value), "At least one blend weight must be non-zero.");
            m_crtBlendWeights = value;
        }
    }

    public LcdScreen(int width, int height)
    {
        FrameBuffer = new CrtFrameBuffer(width, height);
        var pixelSize = new PixelSize(FrameBuffer.OutputWidth, FrameBuffer.OutputHeight);
        Display = new WriteableBitmap(pixelSize, new Vector(96, 96), PixelFormat.Rgba8888, AlphaFormat.Premul);
        m_previousOutput = new FrameBuffer(FrameBuffer.OutputWidth, FrameBuffer.OutputHeight, CrtFrameBuffer.BytesPerPixel);
        FillBlack();
    }

    public void Update(byte[] frameBuffer)
    {
        if (frameBuffer == null)
            throw new ArgumentNullException(nameof(frameBuffer));

        var output = FrameBuffer.Apply(frameBuffer);
        byte[] blended;
        if (FrameBuffer.IsCrt)
        {
            if (!m_hasPreviousOutput)
                m_previousOutput.CopyFrom(output);
            else
                m_previousOutput.BlendWithPrevious(output, m_crtBlendWeights.PreviousFrameWeight, m_crtBlendWeights.CurrentFrameWeight);

            blended = m_previousOutput.Data;
            m_hasPreviousOutput = true;
        }
        else
        {
            m_previousOutput.CopyFrom(output);
            blended = output;
            m_hasPreviousOutput = true;
        }

        using var fb = Display.Lock();
        var length = Math.Min(blended.Length, fb.RowBytes * fb.Size.Height);
        Marshal.Copy(blended, 0, fb.Address, length);
    }

    public void Dispose() => Display?.Dispose();

    private void FillBlack()
    {
        using var fb = Display.Lock();
        var bytes = new byte[fb.RowBytes * fb.Size.Height];
        for (var i = 3; i < bytes.Length; i += CrtFrameBuffer.BytesPerPixel)
            bytes[i] = 255; // Alpha channel is always opaque.
        Marshal.Copy(bytes, 0, fb.Address, bytes.Length);
    }
}
