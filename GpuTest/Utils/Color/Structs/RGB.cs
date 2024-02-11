using ILGPU.Algorithms;
using System;
using System.Runtime.InteropServices;
using static ILGPU.Algorithms.XMath;

namespace GpuTest.Utils.Color
{
    /// <summary>
    /// Represents an RGB color
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct RGB : IVec3, IEquatable<RGB>
    {
        public const int SIZE = sizeof(float) * 3;

        private const float MAX_USHORT = ushort.MaxValue;

        /// <summary>
        /// Gets the red component.
        /// <remarks>A value usually ranging between 0 and 1.</remarks>
        /// </summary>
        public float R;

        /// <summary>
        /// Gets the green component.
        /// <remarks>A value usually ranging between 0 and 1.</remarks>
        /// </summary>
        public float G;

        /// <summary>
        /// Gets the blue component.
        /// <remarks>A value usually ranging between 0 and 1.</remarks>
        /// </summary>
        public float B;


        /// <summary>
        /// Initializes a new instance of the <see cref="RGB"/> struct.
        /// </summary>
        /// <param name="r">The red component ranging between 0 and 1.</param>
        /// <param name="g">The green component ranging between 0 and 1.</param>
        /// <param name="b">The blue component ranging between 0 and 1.</param>
        public RGB(in float r, in float g, in float b)
        {
            R = r;
            G = g;
            B = b;
        }

        /// <summary>
        /// Allows the implicit conversion of an instance of <see cref="RGB24"/> to a
        /// <see cref="RGB"/>.
        /// </summary>
        /// <param name="color">The instance of <see cref="Rgba32"/> to convert.</param>
        /// <returns>An instance of <see cref="RGB"/>.</returns>
        public static implicit operator RGB(in RGB48 color) => new RGB(color.R / MAX_USHORT, color.G / MAX_USHORT, color.B / MAX_USHORT);

        /// <summary>
        /// Allows the implicit conversion of an instance of <see cref="RGB24"/> to a
        /// <see cref="RGB"/>.
        /// </summary>
        /// <param name="color">The instance of <see cref="Rgba32"/> to convert.</param>
        /// <returns>An instance of <see cref="RGB"/>.</returns>
        public static implicit operator RGB(in RGB24 color) => new RGB(color.R / 255F, color.G / 255F, color.B / 255F);

        /// <summary>
        /// Allows the implicit conversion of an instance of <see cref="RGBA32"/> to a
        /// <see cref="RGB"/>.
        /// </summary>
        /// <param name="color">The instance of <see cref="RGBA32"/> to convert.</param>
        /// <returns>An instance of <see cref="RGB"/>.</returns>
        public static implicit operator RGB(in RGBA32 color) => new RGB(color.R / 255F, color.G / 255F, color.B / 255F);

        /// <summary>
        /// Allows the implicit conversion of an instance of <see cref="RGBA64"/> to a
        /// <see cref="RGB"/>.
        /// </summary>
        /// <param name="color">The instance of <see cref="RGBA64"/> to convert.</param>
        /// <returns>An instance of <see cref="RGB"/>.</returns>
        public static implicit operator RGB(in RGBA64 color) => new RGB(color.R / MAX_USHORT, color.G / MAX_USHORT, color.B / MAX_USHORT);

        /// <summary>
        /// Allows the implicit conversion of an instance of <see cref="Vec3"/> to a
        /// <see cref="RGB"/>.
        /// </summary>
        /// <param name="color">The instance of <see cref="Vec3"/> to convert.</param>
        /// <returns>An instance of <see cref="RGB"/>.</returns>
        public static implicit operator RGB(in Vec3 color) => new RGB(color.X, color.Y, color.Z);

        /// <summary>
        /// Compares two <see cref="RGB"/> objects for equality.
        /// </summary>
        /// <param name="left">
        /// The <see cref="RGB"/> on the left side of the operand.
        /// </param>
        /// <param name="right">
        /// The <see cref="RGB"/> on the right side of the operand.
        /// </param>
        /// <returns>
        /// True if the current left is equal to the <paramref name="right"/> parameter; otherwise, false.
        /// </returns>
        public static bool operator ==(RGB left, RGB right) => left.Equals(right);

        /// <summary>
        /// Compares two <see cref="RGB"/> objects for inequality.
        /// </summary>
        /// <param name="left">The <see cref="RGB"/> on the left side of the operand.</param>
        /// <param name="right">The <see cref="RGB"/> on the right side of the operand.</param>
        /// <returns>
        /// True if the current left is unequal to the <paramref name="right"/> parameter; otherwise, false.
        /// </returns>
        public static bool operator !=(RGB left, RGB right) => !left.Equals(right);

        /// <summary>
        /// Returns a new <see cref="Vec3"/> representing this instance.
        /// </summary>
        /// <returns>The <see cref="Vec3"/>.</returns>
        public Vec3 ToVec3() => new Vec3(R, G, B);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(R, G, B);

        /// <inheritdoc/>
        public override string ToString() => FormattableString.Invariant($"Rgb({R:#0.##}, {G:#0.##}, {B:#0.##})");

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is RGB other && Equals(other);

        /// <inheritdoc/>
        public bool Equals(RGB other)
        {
            return R.Equals(other.R)
                && G.Equals(other.G)
                && B.Equals(other.B);
        }


        /// <summary>
        /// Returns a new color that has each channel clamped. If the channel was lower than 0, it'll be 0. If it was higher than 1, it'll be 1.
        /// </summary>
        public RGB Clamp()
        {
            var r = XMath.Clamp(R, 0, 1);
            var g = XMath.Clamp(G, 0, 1);
            var b = XMath.Clamp(B, 0, 1);
            return new RGB(r, g, b);
        }

        /// <summary>
        /// Returns a new color that has channels within the range, and at least one channel is maxed out to 1.
        /// Does this by dividing all channels by the maximum channel value out of those three.
        /// This is useful for scenarios where we're working with chromaticity.
        /// </summary>
        public RGB NormalizeIntensity()
        {
            var maxChannel = Max(R, Max(G, B));
            if (maxChannel == 0)
            {
                maxChannel = 1;
            }

            var r = XMath.Clamp((R / maxChannel), 0, 1);
            var g = XMath.Clamp((G / maxChannel), 0, 1);
            var b = XMath.Clamp((B / maxChannel), 0, 1);
            return new RGB(r, g, b);
        }

        /// <summary>
        /// Performs the conversion from the <see cref="HSV"/> input to an instance of <see cref="Vec3"/> type.
        /// </summary>
        /// <param name="input">The input color instance.</param>
        /// <returns>The converted result</returns>
        public static RGB HSV2RGB(in HSV input)
        {
            const float Epsilon = 0.001F;
            float s = input.S;
            float v = input.V;

            if (XMath.Abs(s) < Epsilon)
            {
                return new RGB(v, v, v);
            }

            float h = XMath.Abs(input.H - 360) < Epsilon ? 0 : input.H / 60;
            int i = (int)XMath.Truncate(h);
            float f = h - i;

            float p = v * (1F - s);
            float q = v * (1F - s * f);
            float t = v * (1F - s * (1F - f));

            float r, g, b;
            switch (i)
            {
                case 0:
                    r = v;
                    g = t;
                    b = p;
                    break;

                case 1:
                    r = q;
                    g = v;
                    b = p;
                    break;

                case 2:
                    r = p;
                    g = v;
                    b = t;
                    break;

                case 3:
                    r = p;
                    g = q;
                    b = v;
                    break;

                case 4:
                    r = t;
                    g = p;
                    b = v;
                    break;

                default:
                    r = v;
                    g = p;
                    b = q;
                    break;
            }

            return new RGB(r, g, b);
        }

        /// <summary>
        /// Performs the conversion from the <see cref="Vec3"/> input to an instance of <see cref="HSV"/> type.
        /// </summary>
        /// <param name="input">The input color instance.</param>
        /// <returns>The converted result</returns>
        public static HSV RGB2HSV(in RGB input)
        {
            const float Epsilon = 0.001F;
            float r = input.R;
            float g = input.G;
            float b = input.B;

            float max = XMath.Max(r, XMath.Max(g, b));
            float min = XMath.Min(r, XMath.Min(g, b));
            float chroma = max - min;
            float h = 0;
            float s = 0;
            float v = max;

            if (XMath.Abs(chroma) < Epsilon)
            {
                return new HSV(0, s, v);
            }

            if (XMath.Abs(r - max) < Epsilon)
            {
                h = (g - b) / chroma;
            }
            else if (XMath.Abs(g - max) < Epsilon)
            {
                h = 2 + (b - r) / chroma;
            }
            else if (XMath.Abs(b - max) < Epsilon)
            {
                h = 4 + (r - g) / chroma;
            }

            h *= 60;
            if (h < 0.0)
            {
                h += 360;
            }

            s = chroma / v;

            return new HSV(h, s, v);
        }
    }
}
