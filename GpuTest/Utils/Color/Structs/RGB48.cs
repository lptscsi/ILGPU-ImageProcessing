using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GpuTest.Utils.Color
{
    /// <summary>
    /// Packed pixel type containing three 16-bit unsigned normalized values ranging from 0 to 635535.
    /// <para>
    /// Ranges from [0, 0, 0, 1] to [1, 1, 1, 1] in vector form.
    /// </para>
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public partial struct RGB48: IVec3, IEquatable<RGB48>
    {
        private const float Max = ushort.MaxValue;

        /// <summary>
        /// Gets or sets the red component.
        /// </summary>
        public ushort R;

        /// <summary>
        /// Gets or sets the green component.
        /// </summary>
        public ushort G;

        /// <summary>
        /// Gets or sets the blue component.
        /// </summary>
        public ushort B;

        /// <summary>
        /// Initializes a new instance of the <see cref="RGB48"/> struct.
        /// </summary>
        /// <param name="r">The red component.</param>
        /// <param name="g">The green component.</param>
        /// <param name="b">The blue component.</param>
        public RGB48(ushort r, ushort g, ushort b)
            : this()
        {
            this.R = r;
            this.G = g;
            this.B = b;
        }

        public Vec3 ToVec3() => new Vec3(this);

        /// <summary>
        /// Allows the implicit conversion of an instance of <see cref="Vec3"/> to a
        /// <see cref="RGB48"/>.
        /// </summary>
        /// <param name="color">The instance of <see cref="Vec3"/> to convert.</param>
        /// <returns>An instance of <see cref="RGB24"/>.</returns>
        public static implicit operator RGB48(in Vec3 color) => new RGB48(PixelRound(color.X * Max), PixelRound(color.Y * Max), PixelRound(color.Z * Max));

        /// <summary>
        /// Allows the implicit conversion of an instance of <see cref="RGB"/> to a
        /// <see cref="RGB48"/>.
        /// </summary>
        /// <param name="color">The instance of <see cref="Rgba32"/> to convert.</param>
        /// <returns>An instance of <see cref="RGB"/>.</returns>
        public static implicit operator RGB48(in RGB color) => new RGB48(PixelRound(color.R * Max), PixelRound(color.G * Max), PixelRound(color.B * Max));

        /// <summary>
        /// Compares two <see cref="RGB48"/> objects for equality.
        /// </summary>
        /// <param name="left">The <see cref="RGB48"/> on the left side of the operand.</param>
        /// <param name="right">The <see cref="RGB48"/> on the right side of the operand.</param>
        /// <returns>
        /// True if the <paramref name="left"/> parameter is equal to the <paramref name="right"/> parameter; otherwise, false.
        /// </returns>
        public static bool operator ==(RGB48 left, RGB48 right) => left.Equals(right);

        /// <summary>
        /// Compares two <see cref="RGB48"/> objects for equality.
        /// </summary>
        /// <param name="left">The <see cref="RGB48"/> on the left side of the operand.</param>
        /// <param name="right">The <see cref="RGB48"/> on the right side of the operand.</param>
        /// <returns>
        /// True if the <paramref name="left"/> parameter is not equal to the <paramref name="right"/> parameter; otherwise, false.
        /// </returns>
        public static bool operator !=(RGB48 left, RGB48 right) => !left.Equals(right);
     
        /// <inheritdoc />
        public override readonly bool Equals(object obj) => obj is RGB48 rgb48 && this.Equals(rgb48);

        /// <inheritdoc />
        public readonly bool Equals(RGB48 other) => this.R.Equals(other.R) && this.G.Equals(other.G) && this.B.Equals(other.B);

        /// <inheritdoc />
        public override readonly string ToString() => $"Rgb48({this.R}, {this.G}, {this.B})";

        /// <inheritdoc />
        public override readonly int GetHashCode() => HashCode.Combine(this.R, this.G, this.B);

        /// <summary>
        /// Round pixel calculated value with saturation
        /// </summary>
        /// <param name="a">Pixel value to pack in unsigned 16 bits</param>
        /// <returns>Packed (rounded) value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort PixelRound(float value)
        {
            float result = value < 0 ? 0 : value;

            return (ushort)(result > Max ? Max : result);
        }
    }
}
