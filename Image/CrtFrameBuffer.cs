// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

using System;
using System.Collections;
using System.Runtime.CompilerServices;

namespace DTC.Emulation.Image;

/// <summary>
/// Software framebuffer that applies CRT-style effects to RGBA input with a 3x output scale.
/// </summary>
public sealed class CrtFrameBuffer
{
    public const int BytesPerPixel = 4;

    private const int Scale = 3;
    private const float ScanlineMultiplier = 0.9f;
    private const float VignetteMin = 0.7f;
    private const float PhosphorShrink = 0.7f;
    private const float CrtSaturationR = 1.1f;
    private const float CrtSaturationB = 1.1f;
    private const float GrainStrength = 0.04f;
    private const int PauseWidth = 38;
    private const int PauseHeight = 12;
    private const int PauseOffsetX = 20;
    private const int PauseOffsetY = 20;

    private static readonly BitArray PauseBitmap = new BitArray(new byte[]
    {
        0x1F, 0x1E, 0x21, 0x1E, 0xFF, 0x87, 0x47, 0x88, 0xC7, 0x1F, 0x12, 0x12,
        0x12, 0x10, 0x84, 0x84, 0x84, 0x04, 0x04, 0x21, 0x21, 0x21, 0x1E, 0x5F,
        0x48, 0x48, 0x88, 0xC7, 0xF7, 0xF1, 0x13, 0x02, 0x12, 0x7C, 0xFC, 0x84,
        0x80, 0x04, 0x01, 0x21, 0x21, 0x21, 0x41, 0x40, 0x48, 0x48, 0x48, 0x10,
        0x10, 0xE2, 0xE1, 0xF1, 0x07, 0x84, 0x78, 0x78, 0xFC
    });

    private readonly int m_inputWidth;
    private readonly int m_inputHeight;
    private readonly byte[] m_output;
    private readonly float[][] m_grain;
    private readonly Random m_random = new Random(0);

    /// <summary>
    /// Output framebuffer width in pixels (3x input).
    /// </summary>
    public int OutputWidth => m_inputWidth * Scale;

    /// <summary>
    /// Output framebuffer height in pixels (3x input).
    /// </summary>
    public int OutputHeight => m_inputHeight * Scale;

    /// <summary>
    /// Number of bytes required for the input buffer.
    /// </summary>
    private int InputByteLength => m_inputWidth * m_inputHeight * BytesPerPixel;

    /// <summary>
    /// Number of bytes required for the output buffer.
    /// </summary>
    private int OutputByteLength => OutputWidth * OutputHeight * BytesPerPixel;

    /// <summary>
    /// Toggles CRT-style processing; when false, a nearest-neighbor scale is applied.
    /// </summary>
    public bool IsCrt { get; set; } = true;

    /// <summary>
    /// Toggles pause-specific effects.
    /// </summary>
    public bool IsPaused { get; set; }

    /// <summary>
    /// Create a CRT framebuffer from an input size.
    /// </summary>
    public CrtFrameBuffer(int width, int height)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));

        m_inputWidth = width;
        m_inputHeight = height;
        m_output = new byte[OutputByteLength];
        m_grain = new float[OutputHeight][];
        for (var y = 0; y < OutputHeight; y++)
            m_grain[y] = new float[OutputWidth];

        RegenerateGrain();

        // Pre-fill alpha.
        Array.Fill(m_output, (byte)255);
    }

    /// <summary>
    /// Apply CRT-style processing and return the output buffer (RGBA, scaled).
    /// </summary>
    public byte[] Apply(byte[] source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (source.Length < InputByteLength)
            throw new ArgumentException($"Expected at least {InputByteLength} bytes.", nameof(source));

        if (IsCrt)
            RenderAsCrt(source);
        else
            RenderAsPlain(source);
        return m_output;
    }

    /// <summary>
    /// Rebuild the grain mask used to modulate output brightness.
    /// </summary>
    private void RegenerateGrain()
    {
        var invW = OutputWidth > 1 ? 1.0f / (OutputWidth - 1) : 0.0f;
        var invH = OutputHeight > 1 ? 1.0f / (OutputHeight - 1) : 0.0f;
        for (var y = 0; y < OutputHeight; y += Scale)
        {
            for (var x = 0; x < OutputWidth; x += Scale)
            {
                var value = 1.0f - GrainStrength * (float)m_random.NextDouble();
                for (var yy = y; yy < Math.Min(y + Scale, OutputHeight); yy++)
                {
                    var scanline = yy % Scale == Scale - 1 ? ScanlineMultiplier : 1.0f;
                    var row = m_grain[yy];
                    for (var xx = x; xx < Math.Min(x + Scale, OutputWidth); xx++)
                    {
                        var uvX = xx * invW;
                        var uvY = yy * invH;
                        var v = MathF.Sqrt(64.0f * uvX * uvY * (1.0f - uvX) * (1.0f - uvY));
                        v = Math.Clamp(v, 0.0f, 1.0f);
                        var vignette = VignetteMin + (1.0f - VignetteMin) * v;
                        row[xx] = value * scanline * vignette;
                    }
                }
            }
        }
    }

    private void RenderAsCrt(byte[] source)
    {
        const float brightness = 3.0f / (1.0f + 2.0f * PhosphorShrink);
        const float brightnessR = brightness * CrtSaturationR;
        const float brightnessB = brightness * CrtSaturationB;
        var inputStride = m_inputWidth * BytesPerPixel;
        var outputStride = OutputWidth * BytesPerPixel;

        var dy = 0;
        var iTime = 0.0;
        if (IsPaused)
        {
            iTime = DateTime.Now.TimeOfDay.TotalSeconds;
            dy = (int)(m_random.NextDouble() * 1.5);
        }

        for (var y = 0; y < m_inputHeight; y++)
        {
            var outputY = y * Scale;
            var outputRowBase = outputY * outputStride;

            var dx = 0;
            var distFromPulse = 1.0;
            if (IsPaused)
            {
                var pulseY = iTime % 8.0 / 8.0 * m_inputHeight * 2.5;
                const double pulseHeight = 20.0;
                distFromPulse = Math.Abs(y - pulseY) / pulseHeight;
                distFromPulse = Math.Clamp(distFromPulse, 0.0, 1.0);
                dx = (int)(-12.0 * m_random.NextDouble() * Math.Cos(distFromPulse * Math.PI / 2.0));
                dx = (int)(dx + (m_random.NextDouble() * 2.2 - 1.1));
            }

            for (var x = 0; x < m_inputWidth; x++)
            {
                var sampleX = x;
                var sampleY = y;
                if (IsPaused)
                {
                    sampleX = Math.Clamp(x + dx, 0, m_inputWidth - 1);
                    sampleY = Math.Clamp(y + dy, 0, m_inputHeight - 1);
                }

                var src = sampleY * inputStride + sampleX * BytesPerPixel;
                var r = (float)source[src];
                var g = (float)source[src + 1];
                var b = (float)source[src + 2];

                if (IsPaused)
                {
                    var lumin = r * 0.2f + g * 0.7f + b * 0.1f;
                    r = r * 0.1f + lumin * 0.9f;
                    g = g * 0.1f + lumin * 0.9f;
                    b = b * 0.1f + lumin * 0.9f;

                    var noise = (float)((m_random.NextDouble() - 0.5) * 50.0);
                    r += noise;
                    g += noise;
                    b += noise;

                    if (m_random.NextDouble() * (1.0 - distFromPulse) > 0.4)
                    {
                        var extra = (float)(92.0 * m_random.NextDouble());
                        r += extra;
                        g += extra;
                        b += extra;
                    }

                    var px = x - PauseOffsetX;
                    var py = sampleY - PauseOffsetY;
                    if (px >= 0 && px < PauseWidth && py >= 0 && py < PauseHeight)
                    {
                        if (PauseBitmap[py * PauseWidth + px])
                        {
                            r += 220.0f;
                            g += 220.0f;
                            b += 220.0f;
                        }
                    }
                }

                r *= brightnessR;
                g *= brightness;
                b *= brightnessB;

                // Precompute phosphor-shrunken colors to avoid repeated multiplies.
                var rPhosphor = r * PhosphorShrink;
                var gPhosphor = g * PhosphorShrink;
                var bPhosphor = b * PhosphorShrink;

                var scaledX = x * Scale;
                var outputPixelBase = outputRowBase + scaledX * BytesPerPixel;

                for (var sy = 0; sy < Scale; sy++)
                {
                    var outputRow = outputPixelBase + sy * outputStride;
                    var grainRow = m_grain[outputY + sy];

                    var grain0 = grainRow[scaledX];
                    var dst = outputRow;
                    m_output[dst] = ClampToByte(r * grain0);
                    m_output[dst + 1] = ClampToByte(gPhosphor * grain0);
                    m_output[dst + 2] = ClampToByte(bPhosphor * grain0);

                    var grain1 = grainRow[scaledX + 1];
                    dst += BytesPerPixel;
                    m_output[dst] = ClampToByte(rPhosphor * grain1);
                    m_output[dst + 1] = ClampToByte(g * grain1);
                    m_output[dst + 2] = ClampToByte(bPhosphor * grain1);

                    var grain2 = grainRow[scaledX + 2];
                    dst += BytesPerPixel;
                    m_output[dst] = ClampToByte(rPhosphor * grain2);
                    m_output[dst + 1] = ClampToByte(gPhosphor * grain2);
                    m_output[dst + 2] = ClampToByte(b * grain2);
                }
            }
        }
    }

    private void RenderAsPlain(byte[] source)
    {
        var inputStride = m_inputWidth * BytesPerPixel;
        var outputStride = OutputWidth * BytesPerPixel;

        for (var y = 0; y < m_inputHeight; y++)
        {
            var inputRow = y * inputStride;
            var outputY = y * Scale;
            var outputRowBase = outputY * outputStride;

            for (var x = 0; x < m_inputWidth; x++)
            {
                var src = inputRow + x * BytesPerPixel;
                var r = source[src];
                var g = source[src + 1];
                var b = source[src + 2];

                if (IsPaused)
                {
                    var px = x - PauseOffsetX;
                    var py = y - PauseOffsetY;
                    if (px >= 0 && px < PauseWidth && py >= 0 && py < PauseHeight)
                    {
                        if (PauseBitmap[py * PauseWidth + px])
                        {
                            r = ClampToByte(r + 200.0f);
                            g = ClampToByte(g + 200.0f);
                            b = ClampToByte(b + 200.0f);
                        }
                    }
                }

                var outputPixelBase = outputRowBase + x * Scale * BytesPerPixel;

                // No CRT effect - Scale up output.
                for (var sy = 0; sy < Scale; sy++)
                {
                    var outputRow = outputPixelBase + sy * outputStride;
                    for (var sx = 0; sx < Scale; sx++)
                    {
                        var dst = outputRow + sx * BytesPerPixel;
                        m_output[dst] = r;
                        m_output[dst + 1] = g;
                        m_output[dst + 2] = b;
                    }
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte ClampToByte(float value)
    {
        if (value < 0.0f)
            return 0;
        return value >= 255.0f ? (byte)255 : (byte)value;
    }
}
