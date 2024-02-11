namespace GpuTest.Utils.Color;

/// <summary>
/// Standard illuminants.
/// </summary>
/// <remarks>
/// Coefficients taken from:
/// http://www.brucelindbloom.com/index.html?Eqn_ChromAdapt.html
/// <br />
/// Descriptions taken from:
/// http://en.wikipedia.org/wiki/Standard_illuminant
/// </remarks>
public static class Illuminants
{
    /// <summary>
    /// Incandescent / Tungsten.
    /// </summary>
    public static readonly XYZ A = new XYZ(x: 1.09850f, y: 1, z: 0.35585f);

    /// <summary>
    /// Direct sunlight at noon (obsolete).
    /// </summary>
    public static readonly XYZ B = new XYZ(x: 0.99072f, y: 1, z: 0.85223f);

    /// <summary>
    /// Average / North sky Daylight (obsolete).
    /// </summary>
    public static readonly XYZ C = new XYZ(x: 0.98074f, y: 1, z: 1.18232f);

    /// <summary>
    /// Horizon Light. ICC profile PCS.
    /// </summary>
    public static readonly XYZ D50 = new XYZ(x: 0.96422f, y: 1, z: 0.82521f);

    /// <summary>
    /// Mid-morning / Mid-afternoon Daylight.
    /// </summary>
    public static readonly XYZ D55 = new XYZ(x: 0.95682f, y: 1, z: 0.92149f);

    /// <summary>
    /// Noon Daylight: Television, sRGB color space.
    /// </summary>
    public static readonly XYZ D65 = new XYZ(x: 0.95047f, y: 1, z: 1.08883f);

    /// <summary>
    /// North sky Daylight.
    /// </summary>
    public static readonly XYZ D75 = new XYZ(x: 0.94972f, y: 1, z: 1.22638f);

    /// <summary>
    /// Equal energy.
    /// </summary>
    public static readonly XYZ E = new XYZ(x: 1, y: 1, z: 1);

    /// <summary>
    /// Cool White Fluorescent.
    /// </summary>
    public static readonly XYZ F2 = new XYZ(x: 0.99186f, y: 1, z: 0.67393f);

    /// <summary>
    /// D65 simulator, Daylight simulator.
    /// </summary>
    public static readonly XYZ F7 = new XYZ(x: 0.95041f, y: 1, z: 1.08747f);

    /// <summary>
    /// Philips TL84, Ultralume 40.
    /// </summary>
    public static readonly XYZ F11 = new XYZ(x: 1.00962f, y: 1, z: 0.64350f);
}
