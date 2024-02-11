using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace GpuTest.Utils.Color;

/// <summary>
/// CIE 1931 XYZ color space.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly struct XYZ : IVec3, IEquatable<XYZ>
{
    public const int SIZE = sizeof(float) * 3;

    #region Constructor

    /// <param name="x">X (usually from 0 to 1).</param>
    /// <param name="y">Y (usually from 0 to 1).</param>
    /// <param name="z">Z (usually from 0 to 1).</param>
    public XYZ(in float x, in float y, in float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <param name="vector"><see cref="Vector" />, expected 3 dimensions (usually from 0 to 1).</param>
    public XYZ(in Vec3 vector)
        : this(in vector.X, in vector.Y, in vector.Z)
    {
    }

    #endregion

    #region Channels

    /// <summary>
    /// X.
    /// Ranges usually from 0 to 1.
    /// </summary>
    public readonly float X;

    /// <summary>
    /// Y.
    /// Ranges usually from 0 to 1.
    /// </summary>
    public readonly float Y;

    /// <summary>
    /// Z.
    /// Ranges usually from 0 to 1.
    /// </summary>
    public readonly float Z;

    /// <inheritdoc />
    public Vec3 ToVec3()
    {
        return new Vec3(X, Y, Z);
    }

    #endregion

    #region Equality

    /// <inheritdoc />
    public bool Equals(XYZ other) =>
        X == other.X &&
        Y == other.Y &&
        Z == other.Z;

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is XYZ other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = X.GetHashCode();
            hashCode = (hashCode * 397) ^ Y.GetHashCode();
            hashCode = (hashCode * 397) ^ Z.GetHashCode();
            return hashCode;
        }
    }

    /// <inheritdoc cref="object" />
    public static bool operator ==(XYZ left, XYZ right) => Equals(left, right);

    /// <inheritdoc cref="object" />
    public static bool operator !=(XYZ left, XYZ right) => !Equals(left, right);

    #endregion

    #region Overrides

    /// <inheritdoc />
    public override string ToString() => FormattableString.Invariant($"XYZ({X:#0.##}, {Y:#0.##}, {Z:#0.##})");

    #endregion
}
