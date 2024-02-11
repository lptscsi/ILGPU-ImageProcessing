using GpuTest.Utils.Color;
using OpenCvSharp;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static GpuTest.CropHelper;

namespace GpuTest.CPU
{
    public static class ScaleRGB
    {
        private const float Max = byte.MaxValue;

        static RGB24 Tex2D(float x, float y, ReadOnlySpan<RGB24> src, int VW, int VH)
        {
            if (y < 0)
            {
                y = 0;
            }
            if (x < 0)
            {
                x = 0;
            }

            int y0 = (int)MathF.Floor(y);
            if (y0 >= VH)
            {
                y0 = VH - 1;
            }
            int y1 = y0 + 1;
            if (y1 >= VH)
            {
                y1 = VH - 1;
            }
            int x0 = (int)MathF.Floor(x);
            if (x0 >= VW)
            {
                x0 = VW - 1;
            }
            int x1 = x0 + 1;
            if (x1 >= VW)
            {
                x1 = VW - 1;
            }

            float fx = x - x0;
            float fy = y - y0;

            RGB24 v00 = src[VW * y0 + x0];
            RGB24 v10 = src[VW * y0 + x1];
            RGB24 v01 = src[VW * y1 + x0];
            RGB24 v11 = src[VW * y1 + x1];

            float r = MathUtils.Blerp(v00.R, v10.R, v01.R, v11.R, fx, fy);
            float g = MathUtils.Blerp(v00.G, v10.G, v01.G, v11.G, fx, fy);
            float b = MathUtils.Blerp(v00.B, v10.B, v01.B, v11.B, fx, fy);
            return new RGB24(PixelRound(r), PixelRound(g), PixelRound(b));
        }

        /// <summary>
        /// Производит масштабирование коэффициентов для другого размера изображения
        /// </summary>
        /// <param name="dstSize"></param>
        /// <returns></returns>
        public static ImageBytesResult Scale(ImageBytesResult colorGains, Size dstSize)
        {
            Size srcSize = new Size(colorGains.Width, colorGains.Height);

            if (dstSize.Width < 1 || dstSize.Height < 1)
            {
                throw new InvalidOperationException("Destination size must be nonzero");
            }

            int kernelSize = 1;
            int VW = colorGains.Width;
            int VH = colorGains.Height;

            int VW2 = RoundUpDivide(dstSize.Width, kernelSize);
            int VH2 = RoundUpDivide(dstSize.Height, kernelSize);

            float scaleX = VW2 / (float)VW;
            float scaleY = VH2 / (float)VH;


            byte[] dst = new byte[VW2 * VH2 * 3];

            Parallel.For(0, VH2, j =>
            {
                float j2 = j / scaleY;
            
                Span<RGB24> dstSpan = MemoryMarshal.Cast<byte, RGB24>(dst);
                ReadOnlySpan<RGB24> src = MemoryMarshal.Cast<byte, RGB24>(colorGains.Bytes);
                int len = src.Length;
                Span<RGB24> dstRowSpan = dstSpan.Slice(VW2 * j, VW2);

                for (int i = 0; i < VW2; ++i)
                {
                    float i2 = i / scaleX;
                    RGB24 v = Tex2D(i2, j2, src, VW, VH);

                    dstRowSpan[i] = v;
                }
            });

            return new ImageBytesResult()
            {
                Bytes = dst,
                Width = dstSize.Width,
                Height = dstSize.Height
            };
        }

        /// <summary>
        /// Round up division by formula res = a / b;
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int RoundUpDivide(int a, int b)
        {
            return (a - 1) / b + 1;
        }

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
