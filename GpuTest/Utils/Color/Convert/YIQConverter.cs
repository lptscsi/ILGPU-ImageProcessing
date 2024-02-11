using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace GpuTest.Utils.Color
{
    public static class YIQConverter
    {
        /// <summary>
        /// Round pixel calculated value with saturation
        /// </summary>
        /// <param name="value">Pixel value to pack in unsigned 16 bits</param>
        /// <returns>Packed (rounded) value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort PixelRound(float value)
        {
            float result = value < 0 ? 0 : value;

            return (ushort)(result > ushort.MaxValue ? ushort.MaxValue : result);
        }

        /// <summary>
        /// Color channel order in YIQ color space (YIQ representation)
        /// </summary>
        public struct YIQ
        {
            /// <summary>
            /// Y channel
            /// </summary>
            public float Y;

            /// <summary>
            /// I channel
            /// </summary>
            public float I;

            /// <summary>
            /// Q channel
            /// </summary>
            public float Q;
        }

        /// <summary>
        /// Number of channels per pixel for RGB/BGR arrays
        /// </summary>
        private const int RgbChannelsCount = 3;

        /// <summary>
        /// Number of channels per pixel for YIQ arrays
        /// </summary>
        private const int YiqChannelsCount = 3;


        /// <summary>
        /// Convert RGB to YIQ values
        /// </summary>
        public static void RgbToYiq(byte[] src, byte[] dst, int width, int height)
        {
            const int yiqPixelSize = YiqChannelsCount * sizeof(float);
            int yiqRowSize = yiqPixelSize * width;
            const int rgbPixelSize = RgbChannelsCount * sizeof(ushort);
            int rgbRowSize = rgbPixelSize * width;

            Parallel.For(0, height, y =>
            {
                ReadOnlySpan<byte> sourceSpan = src.AsSpan().Slice(rgbRowSize * y, rgbRowSize);
                ReadOnlySpan<RGB48> source = MemoryMarshal.Cast<byte, RGB48>(sourceSpan);

                Span<byte> resultSpan = dst.AsSpan().Slice(yiqRowSize * y, yiqRowSize);
                Span<YIQ> result = MemoryMarshal.Cast<byte, YIQ>(resultSpan);

                for (int x = 0; x < width; x++)
                {
                    ref readonly RGB48 rgb = ref source[x];
                    result[x] = ToYIQ(rgb);
                }
            });
        }

        /// <summary>
        /// Convert YIQ to RGB values
        /// </summary>
        public static void YiqToRgb(byte[] src, byte[] dst, int width, int height)
        {
            const int yiqPixelSize = YiqChannelsCount * sizeof(float);
            int yiqRowSize = yiqPixelSize * width;
            const int rgbPixelSize = RgbChannelsCount * sizeof(ushort);
            int rgbRowSize = rgbPixelSize * width;

            Parallel.For(0, height, y =>
            {
                ReadOnlySpan<byte> sourceSpan = src.AsSpan().Slice(yiqRowSize * y, yiqRowSize);
                ReadOnlySpan<YIQ> source = MemoryMarshal.Cast<byte, YIQ>(sourceSpan);

                Span<byte> resultSpan = dst.AsSpan().Slice(rgbRowSize * y, rgbRowSize);
                Span<RGB48> result = MemoryMarshal.Cast<byte, RGB48>(resultSpan);

                for (int x = 0; x < width; x++)
                {
                    ref readonly YIQ yiq = ref source[x];
                    result[x] = ToRgb(yiq);
                }
            });
        }

        public static RGB48 ToRgb(in YIQ yiq)
        {
            return new RGB48(
                PixelRound(yiq.Y + 0.9560f * yiq.I + 0.6190f * yiq.Q),
                PixelRound(yiq.Y - 0.2720f * yiq.I - 0.6470f * yiq.Q),
                PixelRound(yiq.Y - 1.1060f * yiq.I + 1.7030f * yiq.Q)
            );
        }

        public static YIQ ToYIQ(in RGB48 rgb)
        {
            return new YIQ()
            {
                Y = .2990f * rgb.R + .5870f * rgb.G + .1140f * rgb.B,
                I = .5959f * rgb.R - .2746f * rgb.G - .3213f * rgb.B,
                Q = .2115f * rgb.R - .5227f * rgb.G + .3112f * rgb.B
            };
        }
    }

}
