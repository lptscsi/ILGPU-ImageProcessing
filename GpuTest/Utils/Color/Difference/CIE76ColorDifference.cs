using static ILGPU.Algorithms.XMath;

namespace GpuTest.Utils.Color;

/// <summary>
/// CIE Delta-E 1976 color difference formula.
/// </summary>
public readonly struct CIE76ColorDifference : IColorDifference<LAB>
{
    /// <param name="x">Reference color.</param>
    /// <param name="y">Sample color.</param>
    /// <returns>Delta-E (1976) color difference.</returns>
    public float ComputeDifference(in LAB x, in LAB y)
    {
        // Euclidean distance
        var distance = Sqrt(
            (x.L - y.L) * (x.L - y.L) +
            (x.a - y.a) * (x.a - y.a) +
            (x.b - y.b) * (x.b - y.b)
        );
        return distance;
    }
}
