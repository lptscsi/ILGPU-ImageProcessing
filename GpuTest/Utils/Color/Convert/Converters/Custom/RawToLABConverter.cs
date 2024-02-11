namespace GpuTest.Utils.Color;

/// <summary>
/// Converts Linear RGB (RAW) to LAB
/// </summary>
public readonly struct RawToLABConverter
{
    private readonly RGBWorkingSpace<sRGBCompanding> _workingSpace;
    private readonly XYZ _targetWhitePoint;
    private readonly LinearRGBToXYZConverter _toXYZConverter;
    private readonly XYZToLabConverter _toLabConverter;

    public RawToLABConverter(XYZ targetWhitePoint)
    {
        _workingSpace = RGBWorkingSpaces.sRGB;
        _targetWhitePoint = targetWhitePoint;
        _toXYZConverter = new LinearRGBToXYZConverter(_workingSpace.WhitePoint, _workingSpace.Primaries);
        _toLabConverter = new XYZToLabConverter(_targetWhitePoint);
    }

    /// <inheritdoc />
    public LAB Convert(in Vec3 vec)
    {
        LinearRGB linearRGB = new LinearRGB(vec);
        XYZ xyz = _toXYZConverter.Convert(in linearRGB);
        return _toLabConverter.Convert(in xyz);
    }
}
