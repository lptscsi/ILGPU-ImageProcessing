namespace GpuTest.Utils.Color;


/// <remarks>
/// Chromaticity of primaries taken from:
/// http://www.brucelindbloom.com/index.html?WorkingSpaceInfo.html
/// </remarks>
public static class RGBWorkingSpaces
{
    /// <summary>
    /// sRGB.
    /// </summary>
    /// <remarks>
    /// Uses proper companding function, according to:
    /// http://www.brucelindbloom.com/index.html?Eqn_RGB_to_XYZ.html
    /// </remarks>
    public static readonly RGBWorkingSpace<sRGBCompanding> sRGB = new RGBWorkingSpace<sRGBCompanding>(in Illuminants.D65, new sRGBCompanding(), 
        new RGBPrimaries(new xyChromaticity(x: 0.6400f, y: 0.3300f), new xyChromaticity(x: 0.3000f, y: 0.6000f), new xyChromaticity(x: 0.1500f, y: 0.0600f)));
    
    /// <summary>
    /// Simplified sRGB (uses <see cref="GammaCompanding">gamma companding</see> instead of <see cref="sRGBCompanding" />).
    /// See also <see cref="sRGB" />.
    /// </summary>
    public static readonly RGBWorkingSpace<GammaCompanding> sRGBSimplified = new RGBWorkingSpace<GammaCompanding>(in Illuminants.D65, new GammaCompanding(gamma: 2.2f), 
        new RGBPrimaries(new xyChromaticity(x: 0.6400f, y: 0.3300f), new xyChromaticity(x: 0.3000f, y: 0.6000f), new xyChromaticity(x: 0.1500f, y: 0.0600f)));

    /// <summary>
    /// Rec. 709 (ITU-R Recommendation BT.709).
    /// </summary>
    public static readonly RGBWorkingSpace<Rec709Companding> Rec709 = new RGBWorkingSpace<Rec709Companding>(in Illuminants.D65, new Rec709Companding(), 
        new RGBPrimaries(new xyChromaticity(x: 0.64f, y: 0.33f), new xyChromaticity(x: 0.30f, y: 0.60f), new xyChromaticity(x: 0.15f, y: 0.06f)));

    /// <summary>
    /// Rec. 2020 (ITU-R Recommendation BT.2020).
    /// </summary>
    public static readonly RGBWorkingSpace<Rec2020Companding> Rec2020 = new RGBWorkingSpace<Rec2020Companding>(in Illuminants.D65, new Rec2020Companding(), 
        new RGBPrimaries(new xyChromaticity(x: 0.708f, y: 0.292f), new xyChromaticity(x: 0.170f, y: 0.797f), new xyChromaticity(x: 0.131f, y: 0.046f)));

    /// <summary>
    /// ECI RGB v2.
    /// </summary>
    public static readonly RGBWorkingSpace<LCompanding> ECIRGBv2 = new RGBWorkingSpace<LCompanding>(in Illuminants.D50, new LCompanding(), 
        new RGBPrimaries(new xyChromaticity(x: 0.6700f, y: 0.3300f), new xyChromaticity(x: 0.2100f, y: 0.7100f), new xyChromaticity(x: 0.1400f, y: 0.0800f)));

    /// <summary>
    /// Adobe RGB (1998).
    /// </summary>
    public static readonly RGBWorkingSpace<GammaCompanding> AdobeRGB1998 = new RGBWorkingSpace<GammaCompanding>(in Illuminants.D65, new GammaCompanding(gamma: 2.2f),
        new RGBPrimaries(new xyChromaticity(x: 0.6400f, y: 0.3300f), new xyChromaticity(x: 0.2100f, y: 0.7100f), new xyChromaticity(x: 0.1500f, y: 0.0600f)));

    /// <summary>
    /// Apple sRGB.
    /// </summary>
    public static readonly RGBWorkingSpace<GammaCompanding> ApplesRGB = new RGBWorkingSpace<GammaCompanding>(in Illuminants.D65, new GammaCompanding(gamma: 1.8f), 
        new RGBPrimaries(new xyChromaticity(x: 0.6250f, y: 0.3400f), new xyChromaticity(x: 0.2800f, y: 0.5950f), new xyChromaticity(x: 0.1550f, y: 0.0700f)));

    /// <summary>
    /// Best RGB.
    /// </summary>
    public static readonly RGBWorkingSpace<GammaCompanding> BestRGB = new RGBWorkingSpace<GammaCompanding>(in Illuminants.D50, new GammaCompanding(gamma: 2.2f), 
        new RGBPrimaries(new xyChromaticity(x: 0.7347f, y: 0.2653f), new xyChromaticity(x: 0.2150f, y: 0.7750f), new xyChromaticity(x: 0.1300f, y: 0.0350f)));

    /// <summary>
    /// Beta RGB.
    /// </summary>
    public static readonly RGBWorkingSpace<GammaCompanding> BetaRGB = new RGBWorkingSpace<GammaCompanding>(in Illuminants.D50, new GammaCompanding(gamma: 2.2f), 
        new RGBPrimaries(new xyChromaticity(x: 0.6888f, y: 0.3112f), new xyChromaticity(x: 0.1986f, y: 0.7551f), new xyChromaticity(x: 0.1265f, y: 0.0352f)));

    /// <summary>
    /// Bruce RGB.
    /// </summary>
    public static readonly RGBWorkingSpace<GammaCompanding> BruceRGB = new RGBWorkingSpace<GammaCompanding>(in Illuminants.D65, new GammaCompanding(gamma: 2.2f),
        new RGBPrimaries(new xyChromaticity(x: 0.6400f, y: 0.3300f), new xyChromaticity(x: 0.2800f, y: 0.6500f), new xyChromaticity(x: 0.1500f, y: 0.0600f)));

    /// <summary>
    /// CIE RGB.
    /// </summary>
    public static readonly RGBWorkingSpace<GammaCompanding> CIERGB = new RGBWorkingSpace<GammaCompanding>(in Illuminants.E, new GammaCompanding(gamma: 2.2f), 
        new RGBPrimaries(new xyChromaticity(x: 0.7350f, y: 0.2650f), new xyChromaticity(x: 0.2740f, y: 0.7170f), new xyChromaticity(x: 0.1670f, y: 0.0090f)));

    /// <summary>
    /// ColorMatch RGB.
    /// </summary>
    public static readonly RGBWorkingSpace<GammaCompanding> ColorMatchRGB = new RGBWorkingSpace<GammaCompanding>(in Illuminants.D50, new GammaCompanding(gamma: 1.8f), 
        new RGBPrimaries(new xyChromaticity(x: 0.6300f, y: 0.3400f), new xyChromaticity(x: 0.2950f, y: 0.6050f), new xyChromaticity(x: 0.1500f, y: 0.0750f)));

    /// <summary>
    /// Don RGB 4.
    /// </summary>
    public static readonly RGBWorkingSpace<GammaCompanding> DonRGB4 = new RGBWorkingSpace<GammaCompanding>(in Illuminants.D50, new GammaCompanding(gamma: 2.2f), 
        new RGBPrimaries(new xyChromaticity(x: 0.6960f, y: 0.3000f), new xyChromaticity(x: 0.2150f, y: 0.7650f), new xyChromaticity(x: 0.1300f, y: 0.0350f)));

    /// <summary>
    /// Ekta Space PS5.
    /// </summary>
    public static readonly RGBWorkingSpace<GammaCompanding> EktaSpacePS5 = new RGBWorkingSpace<GammaCompanding>(in Illuminants.D50, new GammaCompanding(gamma: 2.2f), 
        new RGBPrimaries(new xyChromaticity(x: 0.6950f, y: 0.3050f), new xyChromaticity(x: 0.2600f, y: 0.7000f), new xyChromaticity(x: 0.1100f, y: 0.0050f)));

    /// <summary>
    /// NTSC RGB.
    /// </summary>
    public static readonly RGBWorkingSpace<GammaCompanding> NTSCRGB = new RGBWorkingSpace<GammaCompanding>(in Illuminants.C, new GammaCompanding(gamma: 2.2f), 
        new RGBPrimaries(new xyChromaticity(x: 0.6700f, y: 0.3300f), new xyChromaticity(x: 0.2100f, y: 0.7100f), new xyChromaticity(x: 0.1400f, y: 0.0800f)));

    /// <summary>
    /// PAL/SECAM RGB.
    /// </summary>
    public static readonly RGBWorkingSpace<GammaCompanding> PALSECAMRGB = new RGBWorkingSpace<GammaCompanding>(in Illuminants.D65, new GammaCompanding(gamma: 2.2f), 
        new RGBPrimaries(new xyChromaticity(x: 0.6400f, y: 0.3300f), new xyChromaticity(x: 0.2900f, y: 0.6000f), new xyChromaticity(x: 0.1500f, y: 0.0600f)));

    /// <summary>
    /// ProPhoto RGB.
    /// </summary>
    public static readonly RGBWorkingSpace<GammaCompanding> ProPhotoRGB = new RGBWorkingSpace<GammaCompanding>(in Illuminants.D50, new GammaCompanding(gamma: 1.8f), 
        new RGBPrimaries(new xyChromaticity(x: 0.7347f, y: 0.2653f), new xyChromaticity(x: 0.1596f, y: 0.8404f), new xyChromaticity(x: 0.0366f, y: 0.0001f)));

    /// <summary>
    /// SMPTE-C RGB.
    /// </summary>
    public static readonly RGBWorkingSpace<GammaCompanding> SMPTECRGB = new RGBWorkingSpace<GammaCompanding>(in Illuminants.D65, new GammaCompanding(gamma: 2.2f), 
        new RGBPrimaries(new xyChromaticity(x: 0.6300f, y: 0.3400f), new xyChromaticity(x: 0.3100f, y: 0.5950f), new xyChromaticity(x: 0.1550f, y: 0.0700f)));

    /// <summary>
    /// Wide Gamut RGB.
    /// </summary>
    public static readonly RGBWorkingSpace<GammaCompanding> WideGamutRGB = new RGBWorkingSpace<GammaCompanding>(in Illuminants.D50, new GammaCompanding(gamma: 2.2f),
        new RGBPrimaries(new xyChromaticity(x: 0.7350f, y: 0.2650f), new xyChromaticity(x: 0.1150f, y: 0.8260f), new xyChromaticity(x: 0.1570f, y: 0.0180f)));
}