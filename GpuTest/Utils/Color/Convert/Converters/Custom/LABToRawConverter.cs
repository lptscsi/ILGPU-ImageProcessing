namespace GpuTest.Utils.Color;

/// <summary>
/// Converts LAB to Linear RGB (RAW)
/// </summary>
public readonly struct LABToRawConverter
{
    private readonly RGBWorkingSpace<sRGBCompanding> _workingSpace;
    private readonly XYZ _sourceWhitePoint;
    private readonly XYZToLinearRGBConverter _toLinearRGBConverter;
    private readonly LabToXYZConverter _toXYZConverter;

    public LABToRawConverter(XYZ sourceWhitePoint)
    {
        _workingSpace = RGBWorkingSpaces.sRGB;
        _sourceWhitePoint = sourceWhitePoint;
        _toLinearRGBConverter = new XYZToLinearRGBConverter(_workingSpace.WhitePoint, _workingSpace.Primaries);
        _toXYZConverter = new LabToXYZConverter(_sourceWhitePoint);
    }

    /// <inheritdoc />
    public Vec3 Convert(in LAB lab)
    {
        XYZ xyz = _toXYZConverter.Convert(in lab);
        LinearRGB linearRGB = _toLinearRGBConverter.Convert(in xyz);
        return new Vec3(linearRGB);
    }
}
