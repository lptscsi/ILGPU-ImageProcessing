namespace GpuTest.Utils.Color;

/// <inheritdoc />
public readonly struct XYZToLinearRGBConverter
{
    private readonly Matrix3x3 _conversionMatrix;

    /// <param name="targetWhitePoint">White point of the RGB working space.</param>
    /// <param name="targetPrimaries">Chromaticity of RGB working space primaries.</param>
    public XYZToLinearRGBConverter(in XYZ targetWhitePoint, in RGBPrimaries targetPrimaries)
    {
        _conversionMatrix = LinearRGBConversionUtils.GetXYZToRGBMatrix(in targetPrimaries, in targetWhitePoint);
    }

    /// <inheritdoc />
    public LinearRGB Convert(in XYZ sourceColor)
    {
        var sourceVector = sourceColor.ToVec3();
        var targetVector = MatrixUtils.MultiplyBy(in _conversionMatrix, in sourceVector);
        var targetColor = new LinearRGB(in targetVector);
        return targetColor;
    }
}
