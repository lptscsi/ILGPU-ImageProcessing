using GpuTest.Utils.Color;
using OpenCvSharp;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static GpuTest.CropHelper;

namespace GpuTest.CPU
{
    // bicubic constrained interpolation
    public static class ScaleRGB2
    {
        private const float Max = byte.MaxValue;

        // w0, w1, w2, and w3 are the four cubic B-spline basis functions
        static float w0(float a)
        {
            //    return (1.0f/6.0f)*(-a*a*a + 3.0f*a*a - 3.0f*a + 1.0f);
            return (1.0f / 6.0f) * (a * (a * (-a + 3.0f) - 3.0f) + 1.0f);  // optimized
        }

        static float w1(float a)
        {
            //    return (1.0f/6.0f)*(3.0f*a*a*a - 6.0f*a*a + 4.0f);
            return (1.0f / 6.0f) * (a * a * (3.0f * a - 6.0f) + 4.0f);
        }

        static float w2(float a)
        {
            //    return (1.0f/6.0f)*(-3.0f*a*a*a + 3.0f*a*a + 3.0f*a + 1.0f);
            return (1.0f / 6.0f) * (a * (a * (-3.0f * a + 3.0f) + 3.0f) + 1.0f);
        }

        static float w3(float a) { return (1.0f / 6.0f) * (a * a * a); }

        // g0 and g1 are the two amplitude functions
        static float g0(float a) { return w0(a) + w1(a); }

        static float g1(float a) { return w2(a) + w3(a); }

        // h0 and h1 are the two offset functions
        static float h0(float a)
        {
            return -1.0f + w1(a) / (w0(a) + w1(a));
        }

        static float h1(float a) { return 1.0f + w3(a) / (w2(a) + w3(a)); }

        // filter 4 values using cubic splines
        static float cubicFilter(float x, float c0, float c1, float c2, float c3)
        {
            float r;
            r = c0 * w0(x);
            r += c1 * w1(x);
            r += c2 * w2(x);
            r += c3 * w3(x);
            return r;
        }

        static RGB24 Tex2Dcub(float x, float y, ReadOnlySpan<RGB24> src, int VW, int VH)
        {
            int y0 = (int)MathF.Floor(y);
            if (y0 >= VH)
            {
                y0 = VH - 1;
            }
            int y1 = y0 - 1;
            if (y1 < 0)
            {
                y1 = 0;
            }
            int y2 = y0 + 1;
            if (y2 >= VH)
            {
                y2 = VH - 1;
            }
            int y3 = y0 + 2;
            if (y3 >= VH)
            {
                y3 = VH - 1;
            }

            int x0 = (int)MathF.Floor(x);
            if (x0 >= VW)
            {
                x0 = VW - 1;
            }
            int x1 = x0 - 1;
            if (x1 < 0)
            {
                x1 = 0;
            }
            int x2 = x0 + 1;
            if (x2 >= VW)
            {
                x2 = VW - 1;
            }
            int x3 = x0 + 2;
            if (x3 >= VW)
            {
                x3 = VW - 1;
            }

            float fx = x - x0;
            float fy = y - y0;

            RGB24 v11 = src[VW * y1 + x1];
            RGB24 v01 = src[VW * y1 + x0];
            RGB24 v21 = src[VW * y1 + x2];
            RGB24 v31 = src[VW * y1 + x3];

            RGB24 v10 = src[VW * y0 + x1];
            RGB24 v00 = src[VW * y0 + x0];
            RGB24 v20 = src[VW * y0 + x2];
            RGB24 v30 = src[VW * y0 + x3];

            RGB24 v12 = src[VW * y2 + x1];
            RGB24 v02 = src[VW * y2 + x0];
            RGB24 v22 = src[VW * y2 + x2];
            RGB24 v32 = src[VW * y2 + x3];

            RGB24 v13 = src[VW * y3 + x1];
            RGB24 v03 = src[VW * y3 + x0];
            RGB24 v23 = src[VW * y3 + x2];
            RGB24 v33 = src[VW * y3 + x3];

            float r = cubicFilter(
                  fy, cubicFilter(fx, v11.R, v01.R, v21.R, v31.R),
                      cubicFilter(fx, v10.R, v00.R, v20.R, v30.R),
                      cubicFilter(fx, v12.R, v02.R, v22.R, v32.R),
                      cubicFilter(fx, v13.R, v03.R, v23.R, v33.R)
                     );

            float g = cubicFilter(
                 fy, cubicFilter(fx, v11.G, v01.G, v21.G, v31.G),
                     cubicFilter(fx, v10.G, v00.G, v20.G, v30.G),
                     cubicFilter(fx, v12.G, v02.G, v22.G, v32.G),
                     cubicFilter(fx, v13.G, v03.G, v23.G, v33.G)
                    );

            float b = cubicFilter(
                 fy, cubicFilter(fx, v11.B, v01.B, v21.B, v31.B),
                     cubicFilter(fx, v10.B, v00.B, v20.B, v30.B),
                     cubicFilter(fx, v12.B, v02.B, v22.B, v32.B),
                     cubicFilter(fx, v13.B, v03.B, v23.B, v33.B)
                    );

            return new RGB24(PixelRound(r), PixelRound(g), PixelRound(b));
        }

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
        public static ImageBytesResult Scale1(ImageBytesResult colorGains, Size dstSize)
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
                int y1 = (int)Math.Floor(j2);
                if (y1 >= VH)
                {
                    y1 = VH - 1;
                }
                int y2 = y1 + 1;
                if (y2 >= VH)
                {
                    y2 = VH - 1;
                }

                Span<RGB24> dstSpan = MemoryMarshal.Cast<byte, RGB24>(dst);
                ReadOnlySpan<RGB24> src = MemoryMarshal.Cast<byte, RGB24>(colorGains.Bytes);
                int len = src.Length;
                Span<RGB24> dstRowSpan = dstSpan.Slice(VW2 * j, VW2);

                for (int i = 0; i < VW2; ++i)
                {
                    float i2 = i / scaleX;
                    int x1 = (int)Math.Floor(i2);
                    if (x1 >= VW)
                    {
                        x1 = VW - 1;
                    }
                    int x2 = x1 + 1;
                    if (x2 >= VW)
                    {
                        x2 = VW - 1;
                    }

                    RGB24 v00 = src[VW * y1 + x1];
                    RGB24 v10 = src[VW * y1 + x2];
                    RGB24 v01 = src[VW * y2 + x1];
                    RGB24 v11 = src[VW * y2 + x2];

                    float fx = i2 - x1;
                    float fy = j2 - y1;

                    // note: we could store these functions in a lookup table texture, but maths is cheap
                    float g0x = g0(fx);
                    float g1x = g1(fx);
                    float h0x = h0(fx);
                    float h1x = h1(fx);
                    float h0y = h0(fy);
                    float h1y = h1(fy);

                    float px0 = x1 + h0x;
                    float px1 = x1 + h1x;
                    float py0 = y1 + h0y;
                    float py1 = y1 + h1y;

                    RGB24 t00 = Tex2D(px0, py0, src, VW, VH);
                    RGB24 t10 = Tex2D(px1, py0, src, VW, VH);
                    RGB24 t01 = Tex2D(px0, py1, src, VW, VH);
                    RGB24 t11 = Tex2D(px1, py1, src, VW, VH);
                    float r = g0(fy) * (g0x * t00.R + g1x * t10.R) + g1(fy) * (g0x * t01.R + g1x * t11.R);
                    float g = g0(fy) * (g0x * t00.G + g1x * t10.G) + g1(fy) * (g0x * t01.G + g1x * t11.G);
                    float b = g0(fy) * (g0x * t00.B + g1x * t10.B) + g1(fy) * (g0x * t01.B + g1x * t11.B);

                    RGB24 v = new RGB24(PixelRound(r), PixelRound(g), PixelRound(b));

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
        /// Производит масштабирование коэффициентов для другого размера изображения
        /// </summary>
        /// <param name="dstSize"></param>
        /// <returns></returns>
        public static ImageBytesResult Scale2(ImageBytesResult colorGains, Size dstSize)
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
 
                    RGB24 v = Tex2Dcub(i2, j2, src, VW, VH);
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
