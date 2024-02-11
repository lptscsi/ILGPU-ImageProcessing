using System;
using static ILGPU.Algorithms.XMath;

namespace GpuTest.Utils.Color;

/// <summary>
/// sRGB companding.
/// </summary>
/// <remarks>
/// For more info see:
/// http://www.brucelindbloom.com/index.html?Eqn_RGB_to_XYZ.html
/// http://www.brucelindbloom.com/index.html?Eqn_XYZ_to_RGB.html
/// </remarks>
public readonly struct sRGBCompanding : ICompanding, IEquatable<sRGBCompanding>
{
    /// <inheritdoc />
    public float ConvertToLinear(in float nonLinearChannel)
    {
        float V = nonLinearChannel;
        float v = V <= 0.04045f ? V / 12.92f : Pow((V + 0.055f) / 1.055f, 2.4f);
        return v;
    }

    /// <inheritdoc />
    public float ConvertToNonLinear(in float linearChannel)
    {
        float v = linearChannel;
        float V = v <= 0.0031308f ? 12.92f * v : 1.055f * Pow(v, 1 / 2.4f) - 0.055f;
        return V;
    }

    #region Equality

    /// <inheritdoc />
    public bool Equals(sRGBCompanding other)
    {
        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is sRGBCompanding;

    /// <inheritdoc />
    public override int GetHashCode() => typeof(sRGBCompanding).GetHashCode();

    /// <inheritdoc cref="object" />
    public static bool operator ==(sRGBCompanding left, sRGBCompanding right) => Equals(left, right);

    /// <inheritdoc cref="object" />
    public static bool operator !=(sRGBCompanding left, sRGBCompanding right) => !Equals(left, right);

    #endregion
}
