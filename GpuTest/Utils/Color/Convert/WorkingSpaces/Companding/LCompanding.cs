using System;
using static ILGPU.Algorithms.XMath;

namespace GpuTest.Utils.Color;

/// <summary>
/// L* companding.
/// </summary>
/// <remarks>
/// For more info see:
/// http://www.brucelindbloom.com/index.html?Eqn_RGB_to_XYZ.html
/// http://www.brucelindbloom.com/index.html?Eqn_XYZ_to_RGB.html
/// </remarks>
public readonly struct LCompanding : ICompanding, IEquatable<LCompanding>
{
    private const float Kappa = (float)(24389d / 27d);
    private const float Epsilon = (float)(216d / 24389d);

    /// <inheritdoc />
    public float ConvertToLinear(in float nonLinearChannel)
    {
        float V = nonLinearChannel;
        float v = V <= 0.08f ? 100.0f * V / Kappa : Pow((V + 0.16f) / 1.16f, 3.0f);
        return v;
    }

    /// <inheritdoc />
    public float ConvertToNonLinear(in float linearChannel)
    {
        float v = linearChannel;
        float V = v <= Epsilon ? v * Kappa / 100.0f : 1.16f * Pow(v, 1.0f / 3.0f) - 0.16f;
        return V;
    }

    #region Equality

    /// <inheritdoc />
    public bool Equals(LCompanding other)
    {
        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is LCompanding;

    /// <inheritdoc />
    public override int GetHashCode() => typeof(LCompanding).GetHashCode();

    /// <inheritdoc cref="object" />
    public static bool operator ==(LCompanding left, LCompanding right) => Equals(left, right);

    /// <inheritdoc cref="object" />
    public static bool operator !=(LCompanding left, LCompanding right) => !Equals(left, right);

    #endregion
}
