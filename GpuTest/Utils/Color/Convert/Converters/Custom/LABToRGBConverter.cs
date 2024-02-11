namespace GpuTest.Utils.Color;

/// <summary>
/// Converts LAB to sRGB
/// </summary>
public readonly struct LABToRGBConverter
{
    private readonly RGBWorkingSpace<sRGBCompanding> _workingSpace;
    private readonly XYZ _sourceWhitePoint;
    private readonly LinearRGBToRGBConverter<sRGBCompanding> _toRGBConverter;
    private readonly XYZToLinearRGBConverter _toLinearRGBConverter;
    private readonly LabToXYZConverter _toXYZConverter;

    public LABToRGBConverter()
    {
        _workingSpace = RGBWorkingSpaces.sRGB;
        _sourceWhitePoint = _workingSpace.WhitePoint;
        _toRGBConverter = new LinearRGBToRGBConverter<sRGBCompanding>(_workingSpace.Companding);
        _toLinearRGBConverter = new XYZToLinearRGBConverter(_workingSpace.WhitePoint, _workingSpace.Primaries);
        _toXYZConverter = new LabToXYZConverter(_sourceWhitePoint);
    }

    public RGB Convert(in LAB lab)
    {
        XYZ xyz = _toXYZConverter.Convert(in lab);
        LinearRGB linearRGB = _toLinearRGBConverter.Convert(in xyz);
        return _toRGBConverter.Convert(in linearRGB);
    }
}
