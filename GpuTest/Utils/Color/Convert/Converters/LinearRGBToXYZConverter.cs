namespace GpuTest.Utils.Color;

/// <inheritdoc />
public readonly struct LinearRGBToXYZConverter
{
    private readonly Matrix3x3 _conversionMatrix;

    /// <param name="sourceWhitePoint">White point of the RGB working space.</param>
    /// <param name="sourcePrimaries">Chromaticity of RGB working space primaries.</param>
    public LinearRGBToXYZConverter(in XYZ sourceWhitePoint, in RGBPrimaries sourcePrimaries)
    {
        _conversionMatrix = LinearRGBConversionUtils.GetRGBToXYZMatrix(in sourcePrimaries, in sourceWhitePoint);
    }

    /// <inheritdoc />
    public XYZ Convert(in LinearRGB sourceColor)
    {
        var sourceVector = sourceColor.ToVec3();
        var targetVector =  MatrixUtils.MultiplyBy(in _conversionMatrix, in sourceVector);
        var targetColor = new XYZ(in targetVector);
        return targetColor;
    }
}
