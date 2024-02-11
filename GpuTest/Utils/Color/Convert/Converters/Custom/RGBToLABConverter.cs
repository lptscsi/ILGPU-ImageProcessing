namespace GpuTest.Utils.Color;

/// <summary>
/// Converts sRGB to LAB
/// </summary>
public readonly struct RGBToLABConverter
{
    private readonly RGBWorkingSpace<sRGBCompanding> _workingSpace;
    private readonly XYZ _targetWhitePoint;
    private readonly RGBToLinearRGBConverter<sRGBCompanding> _toLinearRGBConverter;
    private readonly LinearRGBToXYZConverter _toXYZConverter;
    private readonly XYZToLabConverter _toLabConverter;

    public RGBToLABConverter()
    {
        _workingSpace = RGBWorkingSpaces.sRGB;
        _targetWhitePoint = _workingSpace.WhitePoint;
        _toLinearRGBConverter = new RGBToLinearRGBConverter<sRGBCompanding>(_workingSpace.Companding);
        _toXYZConverter = new LinearRGBToXYZConverter(_workingSpace.WhitePoint, _workingSpace.Primaries);
        _toLabConverter = new XYZToLabConverter(_targetWhitePoint);
    }

    /// <inheritdoc />
    public LAB Convert(in RGB rgb)
    {
        LinearRGB linearRGB = _toLinearRGBConverter.Convert(in rgb); 
        XYZ xyz = _toXYZConverter.Convert(in linearRGB);
        return _toLabConverter.Convert(in xyz);
    }
}
