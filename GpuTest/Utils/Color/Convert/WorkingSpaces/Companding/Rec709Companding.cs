using System;
using static ILGPU.Algorithms.XMath;

namespace GpuTest.Utils.Color;

/// <summary>
/// Rec. 709 companding function.
/// </summary>
/// <remarks>
/// http://en.wikipedia.org/wiki/Rec._709
/// </remarks>
public readonly struct Rec709Companding : ICompanding, IEquatable<Rec709Companding>
{
    /// <inheritdoc />
    public float ConvertToLinear(in float nonLinearChannel)
    {
        float V = nonLinearChannel;
        float L = V < 0.081f ? V / 4.5f : Pow((V + 0.099f) / 1.099f, 1 / 0.45f);
        return L;
    }

    /// <inheritdoc />
    public float ConvertToNonLinear(in float linearChannel)
    {
        float L = linearChannel;
        float V = L < 0.018f ? 4.5f * L : 1.099f * Pow(L, 0.45f) - 0.099f;
        return V;
    }

    #region Equality

    /// <inheritdoc />
    public bool Equals(Rec709Companding other)
    {
        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is Rec709Companding;

    /// <inheritdoc />
    public override int GetHashCode() => typeof(Rec709Companding).GetHashCode();

    /// <inheritdoc cref="object" />
    public static bool operator ==(Rec709Companding left, Rec709Companding right) => Equals(left, right);

    /// <inheritdoc cref="object" />
    public static bool operator !=(Rec709Companding left, Rec709Companding right) => !Equals(left, right);

    #endregion
}
