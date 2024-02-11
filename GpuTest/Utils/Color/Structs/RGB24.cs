using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GpuTest.Utils.Color
{
    /// <summary>
    /// Pixel type containing three 8-bit unsigned normalized values ranging from 0 to 255.
    /// The color components are stored in red, green, blue order (least significant to most significant byte).
    /// <para>
    /// Ranges from [0, 0, 0, 1] to [1, 1, 1, 1] in vector form.
    /// </para>
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public partial struct RGB24 : IVec3, IEquatable<RGB24>
    {
        private const float Max = byte.MaxValue;

        /// <summary>
        /// The red component.
        /// </summary>
        public byte R;

        /// <summary>
        /// The green component.
        /// </summary>
        public byte G;

        /// <summary>
        /// The blue component.
        /// </summary>
        public byte B;

        private static readonly Vector4 MaxBytes = new(byte.MaxValue);
        private static readonly Vector4 Half = new(0.5F);

        /// <summary>
        /// Initializes a new instance of the <see cref="RGB24"/> struct.
        /// </summary>
        /// <param name="r">The red component.</param>
        /// <param name="g">The green component.</param>
        /// <param name="b">The blue component.</param>
        public RGB24(byte r, byte g, byte b)
        {
            this.R = r;
            this.G = g;
            this.B = b;
        }

        public Vec3 ToVec3() => new Vec3(this);

        /// <summary>
        /// Allows the implicit conversion of an instance of <see cref="Vec3"/> to a
        /// <see cref="RGB24"/>.
        /// </summary>
        /// <param name="color">The instance of <see cref="Vec3"/> to convert.</param>
        /// <returns>An instance of <see cref="RGB24"/>.</returns>
        public static implicit operator RGB24(in Vec3 color) => new RGB24(PixelRound(color.X * Max), PixelRound(color.Y * Max), PixelRound(color.Z * Max));

        /// <summary>
        /// Allows the implicit conversion of an instance of <see cref="RGB"/> to a
        /// <see cref="RGB24"/>.
        /// </summary>
        /// <param name="color">The instance of <see cref="RGB"/> to convert.</param>
        /// <returns>An instance of <see cref="RGB24"/>.</returns>
        public static implicit operator RGB24(in RGB color) => new RGB24(PixelRound(color.R * Max), PixelRound(color.G * Max), PixelRound(color.B * Max));


        /// <summary>
        /// Compares two <see cref="RGB24"/> objects for equality.
        /// </summary>
        /// <param name="left">The <see cref="RGB24"/> on the left side of the operand.</param>
        /// <param name="right">The <see cref="RGB24"/> on the right side of the operand.</param>
        /// <returns>
        /// True if the <paramref name="left"/> parameter is equal to the <paramref name="right"/> parameter; otherwise, false.
        /// </returns>
        public static bool operator ==(RGB24 left, RGB24 right) => left.Equals(right);

        /// <summary>
        /// Compares two <see cref="RGB24"/> objects for equality.
        /// </summary>
        /// <param name="left">The <see cref="RGB24"/> on the left side of the operand.</param>
        /// <param name="right">The <see cref="RGB24"/> on the right side of the operand.</param>
        /// <returns>
        /// True if the <paramref name="left"/> parameter is not equal to the <paramref name="right"/> parameter; otherwise, false.
        /// </returns>
        public static bool operator !=(RGB24 left, RGB24 right) => !left.Equals(right);

    
         /// <inheritdoc/>
        public override readonly bool Equals(object obj) => obj is RGB24 other && this.Equals(other);

        /// <inheritdoc/>
        public readonly bool Equals(RGB24 other) => this.R.Equals(other.R) && this.G.Equals(other.G) && this.B.Equals(other.B);

        /// <inheritdoc/>
        public override readonly int GetHashCode() => HashCode.Combine(this.R, this.B, this.G);

        /// <inheritdoc/>
        public override readonly string ToString() => $"Rgb24({this.R}, {this.G}, {this.B})";

        /// <summary>
        /// Round pixel calculated value with saturation
        /// </summary>
        /// <param name="a">Pixel value to pack in unsigned 16 bits</param>
        /// <returns>Packed (rounded) value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte PixelRound(float value)
        {
            float result = value < 0 ? 0 : value;

            return (byte)(result > Max ? Max : result);
        }
    }
}
