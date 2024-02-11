using static GpuTest.Utils.Color.CIEConstants;
using static ILGPU.Algorithms.XMath;

namespace GpuTest.Utils.Color;

/// <inheritdoc />
public readonly struct XYZToLabConverter 
{
    private readonly XYZ _targetWhitePoint;

    /// <param name="targetWhitePoint">White point of the target color.</param>
    public XYZToLabConverter(in XYZ targetWhitePoint)
    {
        _targetWhitePoint = targetWhitePoint;
    }

    /// <inheritdoc />
    public LAB Convert(in XYZ sourceColor)
    {
        // conversion algorithm described here: http://www.brucelindbloom.com/index.html?Eqn_XYZ_to_Lab.html
        float Xr = _targetWhitePoint.X, Yr = _targetWhitePoint.Y, Zr = _targetWhitePoint.Z;

        float xr = sourceColor.X / Xr, yr = sourceColor.Y / Yr, zr = sourceColor.Z / Zr;

        float fx = f(xr);
        float fy = f(yr);
        float fz = f(zr);

        float L = 116 * fy - 16;
        float a = 500 * (fx - fy);
        float b = 200 * (fy - fz);

        LAB targetColor = new LAB(in L, in a, in b);
        return targetColor;
    }

    private static float f(float cr)
    {
        float fc = cr > Epsilon ? Pow(cr, 1 / 3f) : (Kappa * cr + 16f) / 116f;
        return fc;
    }
}
