using static GpuTest.Utils.Color.MatrixUtils;
namespace GpuTest.Utils.Color;

/// <summary>
/// Utils for conversions between Linear RGB and XYZ.
/// </summary>
public static class LinearRGBConversionUtils
{
    /// <summary>
    /// Computes the RGB/XYZ matrix.
    /// </summary>
    public static Matrix3x3 GetRGBToXYZMatrix(in RGBPrimaries sourcePrimaries, in XYZ sourceWhitePoint)
    {
        // for more info, see: http://www.brucelindbloom.com/index.html?Eqn_RGB_XYZ_Matrix.html

        float xr = sourcePrimaries.R.x,
            xg = sourcePrimaries.G.x,
            xb = sourcePrimaries.B.x,
            yr = sourcePrimaries.R.y,
            yg = sourcePrimaries.G.y,
            yb = sourcePrimaries.B.y;

        var Xr = xr / yr;
        const float Yr = 1;
        var Zr = (1 - xr - yr) / yr;

        var Xg = xg / yg;
        const float Yg = 1;
        var Zg = (1 - xg - yg) / yg;

        var Xb = xb / yb;
        const float Yb = 1;
        var Zb = (1 - xb - yb) / yb;

        Matrix3x3 S = Inverse(new Matrix3x3(
            Xr, Xg, Xb,
            Yr, Yg, Yb,
            Zr, Zg, Zb
        ));

        Vec3 W = sourceWhitePoint.ToVec3();

        Vec3 SW = MultiplyBy(in S, in W);
        var Sr = SW.X;
        var Sg = SW.Y;
        var Sb = SW.Z;

        Matrix3x3 M = new Matrix3x3(
            Sr * Xr, Sg * Xg, Sb * Xb,
            Sr * Yr, Sg * Yg, Sb * Yb,
            Sr * Zr, Sg * Zg, Sb * Zb
        );

        return M;
    }

    /// <summary>
    /// Computes the XYZ/RGB matrix.
    /// </summary>
    public static Matrix3x3 GetXYZToRGBMatrix(in RGBPrimaries targetPrimaries, in XYZ targetWhitePoint)
        => Inverse(GetRGBToXYZMatrix(in targetPrimaries, in targetWhitePoint));
}
