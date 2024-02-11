using static ILGPU.Algorithms.XMath;

namespace GpuTest.Utils.Color;

/// <summary>
/// Euclidean distance between two color vectors.
/// </summary>
public readonly struct EuclideanDistanceColorDifference<TColor> : IColorDifference<TColor>
    where TColor : unmanaged, IVec3
{
    /// <summary>
    /// Computes distance between two color vectors as euclidean distance.
    /// </summary>
    public float ComputeDifference(in TColor x, in TColor y)
    {
        var distanceSquared = 0f;
        Vec3 v1 = x.ToVec3();
        Vec3 v2 = y.ToVec3();
        var xyiDiff = v1.X - v2.X;
        distanceSquared += xyiDiff * xyiDiff;
        xyiDiff = v1.Y - v2.Y;
        distanceSquared += xyiDiff * xyiDiff;
        xyiDiff = v1.Z - v2.Z;
        distanceSquared += xyiDiff * xyiDiff;

        var distance = Sqrt(distanceSquared);
        return distance;
    }
}
