namespace GpuTest.Utils.Color;

/// <inheritdoc />
public readonly struct LinearRGBToRGBConverter<TCompanding>
        where TCompanding : unmanaged, ICompanding
{
    private readonly TCompanding _targetCompanding;

    /// <param name="targetCompanding">Companding function of the target RGB working space.</param>
    public LinearRGBToRGBConverter(TCompanding targetCompanding)
    {
        _targetCompanding = targetCompanding;
    }

    /// <inheritdoc />
    public RGB Convert(in LinearRGB color)
    {
        var sourceVector = color.ToVec3();
        float r = _targetCompanding.ConvertToNonLinear(sourceVector.X);
        float g = _targetCompanding.ConvertToNonLinear(sourceVector.Y);
        float b = _targetCompanding.ConvertToNonLinear(sourceVector.Z);
        RGB targetColor = new RGB(in r, in g, in b);
        return targetColor;
    }
}
