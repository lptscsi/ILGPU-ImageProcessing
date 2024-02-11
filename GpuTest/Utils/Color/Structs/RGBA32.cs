using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GpuTest.Utils.Color
{
    [StructLayout(LayoutKind.Sequential, Size = 4, Pack = 4)]
    public struct RGBA32 : IVec3
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public RGBA32()
        {
            R = 0;
            G = 0;
            B = 0;
            A = 255;
        }

        public RGBA32(RGB col)
        {
            R = Clamp8(col.R * 255);
            G = Clamp8(col.G * 255);
            B = Clamp8(col.B * 255);
            A = 255;
        }

        public RGBA32(Vec3 col)
        {
            R = Clamp8(col.X * 255);
            G = Clamp8(col.Y * 255);
            B = Clamp8(col.Z * 255);
            A = 255;
        }

        public RGBA32(float x, float y, float z)
        {
            R = Clamp8(x * 255);
            G = Clamp8(y * 255);
            B = Clamp8(z * 255);
            A = 255;
        }

        public RGBA32(float x)
        {
            R = (byte)(x * 255);
            G = (byte)(x * 255);
            B = (byte)(x * 255);
            A = 255;
        }

        public RGBA32(byte R, byte G, byte B, byte A)
        {
            this.R = R;
            this.G = G;
            this.B = B;
            this.A = A;
        }

        public Vec3 ToVec3()
        {
            return new Vec3(R / 255f, G / 255f, B / 255f);
        }

        public static RGBA32 Lerp(RGBA32 A, RGBA32 B, float t)
        {
            return new RGBA32(
                (byte)(A.R + t * (B.R - A.R)),
                (byte)(A.G + t * (B.G - A.G)),
                (byte)(A.B + t * (B.B - A.B)),
                (byte)(A.A + t * (B.A - A.A))
            );
        }

        /// <summary>
        /// Allows the implicit conversion of an instance of <see cref="RGB"/> to a
        /// <see cref="RGBA32"/>.
        /// </summary>
        /// <param name="color">The instance of <see cref="RGBA32"/> to convert.</param>
        /// <returns>An instance of <see cref="RGB"/>.</returns>
        public static implicit operator RGBA32(in RGB color) => new RGBA32(color);


        /// <summary>
        /// Allows the implicit conversion of an instance of <see cref="Vec3"/> to a
        /// <see cref="RGBA32"/>.
        /// </summary>
        /// <param name="color">The instance of <see cref="RGBA32"/> to convert.</param>
        /// <returns>An instance of <see cref="RGB"/>.</returns>
        public static implicit operator RGBA32(in Vec3 color) => new RGBA32(color);

        /// <summary>
        /// Round pixel calculated value with saturation
        /// </summary>
        /// <param name="a">Pixel value to pack in unsigned 16 bits</param>
        /// <returns>Packed (rounded) value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte Clamp8(float value)
        {
            float result = value < 0 ? 0 : value;

            return (byte)(result > 255f ? 255f : result);
        }
    }
}
