using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GpuTest.Utils.Color
{
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct RGBA64: IVec3
    {
        const float USHORT_MAX =  ushort.MaxValue;

        public ushort R;
        public ushort G;
        public ushort B;
        public ushort A;

        public RGBA64()
        {
            R = 0;
            G = 0;
            B = 0;
            A = ushort.MaxValue;
        }

        public RGBA64(RGB col)
        {
            R = Clamp16(col.R * USHORT_MAX);
            G = Clamp16(col.G * USHORT_MAX);
            B = Clamp16(col.B * USHORT_MAX);
            A = ushort.MaxValue;
        }

        public RGBA64(Vec3 col)
        {
            R = Clamp16(col.X * USHORT_MAX);
            G = Clamp16(col.Y * USHORT_MAX);
            B = Clamp16(col.Z * USHORT_MAX);
            A = ushort.MaxValue;
        }

        public RGBA64(float x, float y, float z)
        {
            R = Clamp16(x * USHORT_MAX);
            G = Clamp16(y * USHORT_MAX);
            B = Clamp16(z * USHORT_MAX);
            A = ushort.MaxValue;
        }

        public RGBA64(float x)
        {
            R = (ushort)(x * USHORT_MAX);
            G = (ushort)(x * USHORT_MAX);
            B = (ushort)(x * USHORT_MAX);
            A = ushort.MaxValue;
        }

        public RGBA64(ushort R, ushort G, ushort B, ushort A)
        {
            this.R = R;
            this.G = G;
            this.B = B;
            this.A = A;
        }
        public Vec3 ToVec3()
        {
            return new Vec3(R / USHORT_MAX, G / USHORT_MAX, B / USHORT_MAX);
        }

        public static RGBA64 Lerp(RGBA64 A, RGBA64 B, float t)
        {
            return new RGBA64(
                (ushort)(A.R + t * (B.R - A.R)),
                (ushort)(A.G + t * (B.G - A.G)),
                (ushort)(A.B + t * (B.B - A.B)),
                (ushort)(A.A + t * (B.A - A.A))
            );
        }

        /// <summary>
        /// Allows the implicit conversion of an instance of <see cref="RGB"/> to a
        /// <see cref="RGBA64"/>.
        /// </summary>
        /// <param name="color">The instance of <see cref="RGBA64"/> to convert.</param>
        /// <returns>An instance of <see cref="RGB"/>.</returns>
        public static implicit operator RGBA64(in RGB color) => new RGBA64(color);


        /// <summary>
        /// Allows the implicit conversion of an instance of <see cref="Vec3"/> to a
        /// <see cref="RGBA64"/>.
        /// </summary>
        /// <param name="color">The instance of <see cref="RGBA64"/> to convert.</param>
        /// <returns>An instance of <see cref="RGB"/>.</returns>
        public static implicit operator RGBA64(in Vec3 color) => new RGBA64(color);

        /// <summary>
        /// Round pixel calculated value with saturation
        /// </summary>
        /// <param name="a">Pixel value to pack in unsigned 16 bits</param>
        /// <returns>Packed (rounded) value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort Clamp16(float value)
        {
            float result = value < 0 ? 0 : value;

            return (ushort)(result > USHORT_MAX ? USHORT_MAX : result);
        }
    }
}
