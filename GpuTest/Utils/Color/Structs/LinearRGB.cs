using ILGPU.Algorithms;
using System;
using System.Runtime.InteropServices;
using static ILGPU.Algorithms.XMath;

namespace GpuTest.Utils.Color;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly struct LinearRGB : IVec3, IEquatable<LinearRGB>
{
    public const int SIZE = sizeof(float) * 3;

    #region Constructor

    /// <param name="r">Red (from 0 to 1).</param>
    /// <param name="g">Green (from 0 to 1).</param>
    /// <param name="b">Blue (from 0 to 1).</param>
    public LinearRGB(in float r, in float g, in float b)
    {
        R = r;
        G = g;
        B = b;
    }

    /// <param name="vector"><see cref="Vector" />, expected 3 dimensions (range from 0 to 1).</param>
    public LinearRGB(in Vec3 vector)
        : this(in vector.X, in vector.Y, in vector.Z)
    {
    }

    #endregion

    #region Channels

    /// <summary>
    /// Red.
    /// Ranges usually from 0 to 1.
    /// </summary>
    public readonly float R;

    /// <summary>
    /// Green.
    /// Ranges usually from 0 to 1.
    /// </summary>
    public readonly float G;

    /// <summary>
    /// Blue.
    /// Ranges usually from 0 to 1.
    /// </summary>
    public readonly float B;

    /// <summary>
    /// Returns a new <see cref="Vec3"/> representing this instance.
    /// </summary>
    /// <returns>The <see cref="Vec3"/>.</returns>
    public Vec3 ToVec3() => new Vec3(R, G, B);

    #endregion

    #region Equality

    /// <inheritdoc />
     public bool Equals(LinearRGB other) => R.Equals(other.R) && G.Equals(other.G) && B.Equals(other.B);

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is LinearRGB other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = R.GetHashCode();
            hashCode = (hashCode * 397) ^ G.GetHashCode();
            hashCode = (hashCode * 397) ^ B.GetHashCode();
            return hashCode;
        }
    }

    /// <inheritdoc cref="object" />
    public static bool operator ==(LinearRGB left, LinearRGB right) => Equals(left, right);

    /// <inheritdoc cref="object" />
    public static bool operator !=(LinearRGB left, LinearRGB right) => !Equals(left, right);

    #endregion
   
    #region Utils

    /// <summary>
    /// Returns a new color that has each channel clamped. If the channel was lower than 0, it'll be 0. If it was higher than 1, it'll be 1.
    /// </summary>
    public LinearRGB Clamp()
    {
        var r = XMath.Clamp(R, 0, 1);
        var g = XMath.Clamp(G, 0, 1);
        var b = XMath.Clamp(B, 0, 1);
        return new LinearRGB(r, g, b);
    }

    /// <summary>
    /// Returns a new color that has channels within the range, and at least one channel is maxed out to 1.
    /// Does this by dividing all channels by the maximum channel value out of those three.
    /// This is useful for scenarios where we're working with chromaticity.
    /// </summary>
    public LinearRGB NormalizeIntensity()
    {
        var maxChannel = Max(R, Max(G, B));
        if (maxChannel == 0)
        {
            maxChannel = 1;
        }

        var r = XMath.Clamp((R / maxChannel), 0, 1);
        var g = XMath.Clamp((G / maxChannel), 0, 1);
        var b = XMath.Clamp((B / maxChannel), 0, 1);
        return new LinearRGB(r, g, b);
    }

    #endregion

    #region Overrides

    /// <inheritdoc />
    public override string ToString() => FormattableString.Invariant($"LinearRgb({R:#0.##}, {G:#0.##}, {B:#0.##})");

    #endregion
}
