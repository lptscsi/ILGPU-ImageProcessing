namespace GpuTest.Utils.Color;

/// <summary>
/// Computes distance between two vectors in color space.
/// </summary>
/// <typeparam name="TColor">Color space</typeparam>
public interface IColorDifference<TColor>
    where TColor : unmanaged, IVec3
{
    /// <summary>
    /// Computes distance between color x and y.
    /// </summary>
    float ComputeDifference(in TColor x, in TColor y);
}
