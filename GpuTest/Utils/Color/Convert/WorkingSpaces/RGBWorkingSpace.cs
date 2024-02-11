namespace GpuTest.Utils.Color;

public readonly struct RGBWorkingSpace<TCompanding>
    where TCompanding : unmanaged, ICompanding
{
    /// <summary>
    /// Constructs RGB working space using a white point, companding, and primeries.
    /// </summary>
    public RGBWorkingSpace(in XYZ whitePoint, in TCompanding companding, in RGBPrimaries primaries)
    {
        WhitePoint = whitePoint;
        Companding = companding;
        Primaries = primaries;
    }

    /// <inheritdoc />
    public XYZ WhitePoint { get; }

    /// <inheritdoc />
    public RGBPrimaries Primaries { get; }

    /// <inheritdoc />
    public TCompanding Companding { get; }
}
