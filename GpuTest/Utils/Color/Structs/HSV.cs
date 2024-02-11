using System;
using System.Runtime.InteropServices;

namespace GpuTest.Utils.Color
{
    /// <summary>
    /// Represents a HSV (hue, saturation, value) color. Also known as HSB (hue, saturation, brightness).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct HSV : IEquatable<HSV>
    {
        private static readonly Vec3 Min = new Vec3(0, 0, 0);
        private static readonly Vec3 Max = new Vec3(360, 1, 1);

        /// <summary>
        /// Gets the hue component.
        /// <remarks>A value ranging between 0 and 360.</remarks>
        /// </summary>
        public readonly float H;

        /// <summary>
        /// Gets the saturation component.
        /// <remarks>A value ranging between 0 and 1.</remarks>
        /// </summary>
        public readonly float S;

        /// <summary>
        /// Gets the value (brightness) component.
        /// <remarks>A value ranging between 0 and 1.</remarks>
        /// </summary>
        public readonly float V;

        /// <summary>
        /// Initializes a new instance of the <see cref="HSV"/> struct.
        /// </summary>
        /// <param name="h">The h hue component.</param>
        /// <param name="s">The s saturation component.</param>
        /// <param name="v">The v value (brightness) component.</param>
        public HSV(float h, float s, float v)
            : this(new Vec3(h, s, v))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HSV"/> struct.
        /// </summary>
        /// <param name="vector">The vector representing the h, s, v components.</param>
        public HSV(Vec3 vector)
        {
            vector = Vec3.Clamp(vector, Min, Max);
            H = vector.X;
            S = vector.Y;
            V = vector.Z;
        }

        /// <summary>
        /// Compares two <see cref="HSV"/> objects for equality.
        /// </summary>
        /// <param name="left">The <see cref="HSV"/> on the left side of the operand.</param>
        /// <param name="right">The <see cref="HSV"/> on the right side of the operand.</param>
        /// <returns>
        /// True if the current left is equal to the <paramref name="right"/> parameter; otherwise, false.
        /// </returns>
        public static bool operator ==(HSV left, HSV right) => left.Equals(right);

        /// <summary>
        /// Compares two <see cref="HSV"/> objects for inequality.
        /// </summary>
        /// <param name="left">The <see cref="HSV"/> on the left side of the operand.</param>
        /// <param name="right">The <see cref="HSV"/> on the right side of the operand.</param>
        /// <returns>
        /// True if the current left is unequal to the <paramref name="right"/> parameter; otherwise, false.
        /// </returns>
        public static bool operator !=(HSV left, HSV right) => !left.Equals(right);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(H, S, V);

        /// <inheritdoc/>
        public override string ToString() => FormattableString.Invariant($"Hsv({H:#0.##}, {S:#0.##}, {V:#0.##})");

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is HSV other && Equals(other);

        /// <inheritdoc/>
        public bool Equals(HSV other)
        {
            return H.Equals(other.H)
                && S.Equals(other.S)
                && V.Equals(other.V);
        }
    }
}
