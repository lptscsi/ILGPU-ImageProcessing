namespace GpuTest.Utils.Color;

/// <summary>
/// Pair of companding functions for <see cref="RGBWorkingSpace" />.
/// Used for conversion to XYZ and backwards.
/// </summary>
public interface ICompanding
{
    /// <summary>
    /// Inverse companding. The input companded channel is made linear with respect to the energy.
    /// </summary>
    /// <remarks>
    /// For more info see:
    /// http://www.brucelindbloom.com/index.html?Eqn_RGB_to_XYZ.html
    /// </remarks>
    float ConvertToLinear(in float nonLinearChannel);

    /// <summary>
    /// Companding. The input uncompanded channel (linear) is made nonlinear (depends on the RGB color system).
    /// </summary>
    /// <remarks>
    /// For more info see:
    /// http://www.brucelindbloom.com/index.html?Eqn_XYZ_to_RGB.html
    /// </remarks>
    float ConvertToNonLinear(in float linearChannel);
}
