using static GpuTest.Utils.Color.CIEConstants;

namespace GpuTest.Utils.Color;

/// <inheritdoc />
public readonly struct LabToXYZConverter
{
    private readonly XYZ _sourceWhitePoint;

    /// <param name="sourceWhitePoint">White point of the source color.</param>
    public LabToXYZConverter(in XYZ sourceWhitePoint)
    { 
        _sourceWhitePoint = sourceWhitePoint;
    }

    // <summary>
    /// Compute x^3.
    /// </summary>
    /// <param name="x">Base.</param>
    /// <returns>Result of the exponentiation.</returns>
    static float Pow3(in float x) => x * x * x;

    /// <inheritdoc />
    public XYZ Convert(in LAB sourceColor)
    {
        // conversion algorithm described here: http://www.brucelindbloom.com/index.html?Eqn_Lab_to_XYZ.html
        float L = sourceColor.L, a = sourceColor.a, b = sourceColor.b;
        float fy = (L + 16f) / 116f;
        float fx = a / 500f + fy;
        float fz = fy - b / 200f;

        float fx3 = Pow3(fx);
        float fz3 = Pow3(fz);

        float xr = fx3 > Epsilon ? fx3 : (116f * fx - 16f) / Kappa;
        float yr = L > Kappa * Epsilon ? Pow3((L + 16f) / 116f) : L / Kappa;
        float zr = fz3 > Epsilon ? fz3 : (116f * fz - 16f) / Kappa;

        float Xr = _sourceWhitePoint.X, Yr = _sourceWhitePoint.Y, Zr = _sourceWhitePoint.Z;

        float X = xr * Xr;
        float Y = yr * Yr;
        float Z = zr * Zr;

        XYZ targetColor = new XYZ(in X, in Y, in Z);
        return targetColor;
    }
}
