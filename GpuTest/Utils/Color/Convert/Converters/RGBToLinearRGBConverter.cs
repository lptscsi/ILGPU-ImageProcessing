namespace GpuTest.Utils.Color;

/// <inheritdoc />
public readonly struct RGBToLinearRGBConverter<TCompanding>
        where TCompanding : unmanaged, ICompanding

{
    private readonly TCompanding _sourceCompanding;

    /// <param name="sourceCompanding">Companding function of the source RGB working space.</param>
    public RGBToLinearRGBConverter(TCompanding sourceCompanding) => _sourceCompanding = sourceCompanding;

    /// <inheritdoc />
    public LinearRGB Convert(in RGB rgb)
    {
        float r = _sourceCompanding.ConvertToLinear(rgb.R);
        float g = _sourceCompanding.ConvertToLinear(rgb.G);
        float b = _sourceCompanding.ConvertToLinear(rgb.B);
        LinearRGB targetColor = new LinearRGB(in r, in g, in b);
        return targetColor;
    }
}
