using System;
using static ILGPU.Algorithms.XMath;

namespace GpuTest.Utils.Color;

/// <summary>
/// Gamma companding.
/// </summary>
/// <remarks>
/// For more info see:
/// http://www.brucelindbloom.com/index.html?Eqn_RGB_to_XYZ.html
/// http://www.brucelindbloom.com/index.html?Eqn_XYZ_to_RGB.html
/// </remarks>
public readonly struct GammaCompanding : ICompanding, IEquatable<GammaCompanding>
{
    /// <summary>
    /// Constructs with given gamma.
    /// </summary>
    public GammaCompanding(in float gamma) => Gamma = gamma;

    /// <summary>
    /// Gamma.
    /// </summary>
    public float Gamma { get; }

    /// <inheritdoc />
    public float ConvertToLinear(in float nonLinearChannel)
    {
        var V = nonLinearChannel;
        var v = Pow(V, Gamma);
        return v;
    }

    /// <inheritdoc />
    public float ConvertToNonLinear(in float linearChannel)
    {
        var v = linearChannel;
        var V = Pow(v, 1 / Gamma);
        return V;
    }

    /// <inheritdoc />
    public bool Equals(GammaCompanding other)
    {
        return Gamma == other.Gamma;
    }

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is GammaCompanding other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Gamma.GetHashCode();

    /// <inheritdoc cref="object" />
    public static bool operator ==(GammaCompanding left, GammaCompanding right) => Equals(left, right);

    /// <inheritdoc cref="object" />
    public static bool operator !=(GammaCompanding left, GammaCompanding right) => !Equals(left, right);
}
