using System;

namespace GpuTest.Utils.Color;

/// <summary>
/// CIE L*a*b* (1976) color.
/// </summary>
public readonly struct LAB : IVec3, IEquatable<LAB>
{
    #region Constructor

    /// <param name="l">L* (lightness) (from 0 to 100).</param>
    /// <param name="a">a* (usually from -100 to 100).</param>
    /// <param name="b">b* (usually from -100 to 100).</param>
    public LAB(in float l, in float a, in float b)
    {
        L = l;
        this.a = a;
        this.b = b;
    }

    /// <param name="vector"><see cref="Vector" />, expected 3 dimensions.</param>
    public LAB(in Vec3 vector)
        : this(in vector.X, in vector.Y, in vector.Z)
    {
    }

    #endregion

    #region Channels

    /// <summary>
    /// L* (lightness).
    /// Ranges usually from 0 to 100.
    /// </summary>
    public readonly float L;

    /// <summary>
    /// a*.
    /// Ranges usually from -100 to 100.
    /// Negative values indicate green while positive values indicate magenta.
    /// </summary>
    public readonly float a;

    /// <summary>
    /// b*.
    /// Ranges usually from -100 to 100.
    /// Negative values indicate blue and positive values indicate yellow.
    /// </summary>
    public readonly float b;

    /// <inheritdoc />
    public Vec3 ToVec3()
    {
        return new Vec3(L, a, b);
    }

    #endregion

    #region Equality

    /// <inheritdoc />
     public bool Equals(LAB other) =>
        L == other.L &&
        a == other.a &&
        b == other.b;

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is LAB other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = L.GetHashCode();
            hashCode = (hashCode * 397) ^ a.GetHashCode();
            hashCode = (hashCode * 397) ^ b.GetHashCode();
            return hashCode;
        }
    }

    /// <inheritdoc cref="object" />
    public static bool operator ==(LAB left, LAB right) => Equals(left, right);

    /// <inheritdoc cref="object" />
    public static bool operator !=(LAB left, LAB right) => !Equals(left, right);

    #endregion

    #region Overrides

    /// <inheritdoc />
    public override string ToString() => FormattableString.Invariant($"Lab({L:#0.##}, {a:#0.##}, {b:#0.##})");

    #endregion
}
