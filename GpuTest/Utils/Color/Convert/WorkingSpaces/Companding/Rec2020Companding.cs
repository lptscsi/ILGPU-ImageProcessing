using System;
using static ILGPU.Algorithms.XMath;

namespace GpuTest.Utils.Color;

/// <summary>
/// Rec. 2020 companding function (for 12-bit).
/// </summary>
/// <remarks>
/// http://en.wikipedia.org/wiki/Rec._2020
/// For 10-bits, companding is identical to <see cref="Rec709Companding" />
/// </remarks>
public readonly struct Rec2020Companding : ICompanding, IEquatable<Rec2020Companding>
{
    private const float Alpha = 1.09929682680944f;
    private const float Beta = 0.018053968510807f;
    private const float InverseBeta = Beta * 4.5f;

    /// <inheritdoc />
    public float ConvertToLinear(in float nonLinearChannel)
    {
        float V = nonLinearChannel;
        float L = V < InverseBeta ? V / 4.5f : Pow((V + Alpha - 1.0f) / Alpha, 1 / 0.45f);
        return L;
    }

    /// <inheritdoc />
    public float ConvertToNonLinear(in float linearChannel)
    {
        float L = linearChannel;
        float V = L < Beta ? 4.5f * L : Alpha * Pow(L, 0.45f) - (Alpha - 1.0f);
        return V;
    }

    #region Equality

    /// <inheritdoc />
    public bool Equals(Rec2020Companding other)
    {
        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is Rec2020Companding;

    /// <inheritdoc />
    public override int GetHashCode() => typeof(Rec2020Companding).GetHashCode();

    /// <inheritdoc cref="object" />
    public static bool operator ==(Rec2020Companding left, Rec2020Companding right) => Equals(left, right);

    /// <inheritdoc cref="object" />
    public static bool operator !=(Rec2020Companding left, Rec2020Companding right) => !Equals(left, right);

    #endregion
}
