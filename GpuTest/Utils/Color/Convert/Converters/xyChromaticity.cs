using System;

namespace GpuTest.Utils.Color;

/// <summary>
/// CIE xy chromaticity space.
/// </summary>
public readonly struct xyChromaticity : IEquatable<xyChromaticity>
{
    /// <param name="x">Chromaticity x (usually from 0 to 1).</param>
    /// <param name="y">Chromaticity y (usually from 0 to 1).</param>
    public xyChromaticity(in float x, in float y)
    {
        this.x = x;
        this.y = y;
    }

    /// <summary>
    /// Chromaticity x.
    /// Ranges usually from 0 to 1.
    /// </summary>
    public readonly float x;

    /// <summary>
    /// Chromaticity y.
    /// Ranges usually from 0 to 1.
    /// </summary>
    public readonly float y;


    #region Equality

    /// <inheritdoc />
    public bool Equals(xyChromaticity other) => x.Equals(other.x) && y.Equals(other.y);

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is xyChromaticity xyChromaticity && Equals(xyChromaticity);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return (x.GetHashCode() * 397) ^ y.GetHashCode();
    }

    /// <inheritdoc cref="object" />
    public static bool operator ==(xyChromaticity left, xyChromaticity right) => left.Equals(right);

    /// <inheritdoc cref="object" />
    public static bool operator !=(xyChromaticity left, xyChromaticity right) => !left.Equals(right);

    #endregion

    #region Overrides

    /// <inheritdoc />
    //public override string ToString() => string.Format(CultureInfo.InvariantCulture, "xy [x={0:0.##}, y={1:0.##}]", x, y);

    #endregion
}
