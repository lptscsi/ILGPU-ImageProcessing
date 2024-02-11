using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace GpuTest.Utils.Color
{
    public static class RgbConverter
    {
        #region Row Width

        /// <summary>
        /// Размер канала пикселя в байтах
        /// </summary>
        /// <param name="colorDepth"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetPixelSize(ColorDepths colorDepth, int channelCount)
        {
            return colorDepth switch
            {
                ColorDepths.Bits8 => 1 * channelCount,
                ColorDepths.Bits16 => 2 * channelCount,
                _ => throw new ArgumentOutOfRangeException(nameof(colorDepth), $"Not expected colorDepth value: {colorDepth}"),
            };
        }

        internal static void ConvertRGB24toL8()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Возвращает размер одного ряда в байтах
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="colorDepth"></param>
        /// <returns></returns>
        public static int GetRowSize(byte[] bytes, int width, int height, ColorDepths colorDepth, int channelCount)
        {
            int pixelSize = GetPixelSize(colorDepth, channelCount);

            int rowSize = width * pixelSize;
            int expectedSize = rowSize * height;

            if (bytes.Length < expectedSize)
            {
                throw new InvalidOperationException($"Invalid bytes length: {bytes.Length}, expected {expectedSize}");
            }

            // rows with padding
            if (bytes.Length > expectedSize)
            {
                rowSize = (int)TDIBWIDTHBYTES(colorDepth, (uint)width);
                expectedSize = rowSize * height;
                if (bytes.Length != expectedSize)
                {
                    throw new InvalidOperationException($"Invalid bytes length: {bytes.Length}, expected {expectedSize}");
                }
            }

            return rowSize;
        }

        /// <summary>
        /// Ширина в байтах 1 ряда в изображении
        /// </summary>
        /// <param name="bits"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint TDIBWIDTHBYTES(uint bits)
        {
            return (uint)(bits + 31 & ~31) / 8;
        }

        /// <summary>
        /// Ширина в байтах 1 ряда изображении
        /// </summary>
        /// <param name="colorDepth"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint TDIBWIDTHBYTES(ColorDepths colorDepth, uint width)
        {
            switch (colorDepth)
            {
                case ColorDepths.Bits8:
                    return TDIBWIDTHBYTES(24 * width);
                case ColorDepths.Bits16:
                    return TDIBWIDTHBYTES(48 * width);
                default:
                    throw new ArgumentOutOfRangeException(nameof(colorDepth));
            }
        }

        #endregion

        #region BitsConversion

        /// <summary>
        /// Gets the luminance from the rgb components using the formula
        /// as specified by ITU-R Recommendation BT.709.
        /// </summary>
        /// <param name="r">The red component.</param>
        /// <param name="g">The green component.</param>
        /// <param name="b">The blue component.</param>
        /// <returns>The <see cref="byte"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Get8BitBT709Luminance(byte r, byte g, byte b)
        {
            return (byte)(r * .2126F + g * .7152F + b * .0722F + 0.5F);
        }

        /// <summary>
        /// Gets the luminance from the rgb components using the formula as
        /// specified by ITU-R Recommendation BT.709.
        /// </summary>
        /// <param name="r">The red component.</param>
        /// <param name="g">The green component.</param>
        /// <param name="b">The blue component.</param>
        /// <returns>The <see cref="ushort"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Get16BitBT709Luminance(ushort r, ushort g, ushort b)
        {
            return (ushort)(r * .2126F + g * .7152F + b * .0722F + 0.5F);
        }


        /// <summary>
        /// Scales a value from a 16 bit <see cref="ushort"/> to an
        /// 8 bit <see cref="byte"/> equivalent.
        /// </summary>
        /// <param name="component">The 8 bit component value.</param>
        /// <returns>The <see cref="byte"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte DownScaleFrom16BitTo8Bit(ushort component)
        {
            // To scale to 8 bits From a 16-bit value V the required value (from the PNG specification) is:
            //
            //    (V * 255) / 65535
            //
            // This reduces to round(V / 257), or floor((V + 128.5)/257)
            //
            // Represent V as the two byte value vhi.vlo.  Make a guess that the
            // result is the top byte of V, vhi, then the correction to this value
            // is:
            //
            //    error = floor(((V-vhi.vhi) + 128.5) / 257)
            //          = floor(((vlo-vhi) + 128.5) / 257)
            //
            // This can be approximated using integer arithmetic (and a signed
            // shift):
            //
            //    error = (vlo-vhi+128) >> 8;
            //
            // The approximate differs from the exact answer only when (vlo-vhi) is
            // 128; it then gives a correction of +1 when the exact correction is
            // 0.  This gives 128 errors.  The exact answer (correct for all 16-bit
            // input values) is:
            //
            //    error = (vlo-vhi+128)*65535 >> 24;
            //
            // An alternative arithmetic calculation which also gives no errors is:
            //
            //    (V * 255 + 32895) >> 16
            return (byte)(component * 255 + 32895 >> 16);
        }

        /// <summary>
        /// Scales a value from an 8 bit <see cref="byte"/> to
        /// an 16 bit <see cref="ushort"/> equivalent.
        /// </summary>
        /// <param name="component">The 8 bit component value.</param>
        /// <returns>The <see cref="ushort"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort UpscaleFrom8BitTo16Bit(byte component)
        {
            return (ushort)(component * 257);
        }

        #endregion

        #region Gray Conversion

        /// <summary>
        /// Конвертировать в серый цвет (RGB24 -> H8)
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static byte[] ConvertRGB24toU8(byte[] bytes, int width, int height)
        {
            // размер входного пикселя в байтах
            int pixelSize = GetPixelSize(ColorDepths.Bits8, 3);
            int rowSize = GetRowSize(bytes, width, height, ColorDepths.Bits8, 3);
            int resultPixelSize = GetPixelSize(ColorDepths.Bits8, 1);
            int resultRowSize = resultPixelSize * width;

            // Буфер для результата
            byte[] result = new byte[resultRowSize * height];
            var converter = new HsvAndRgbConverter();

            Parallel.For(0, height, y =>
            {
                ReadOnlySpan<byte> inputSpan = bytes;
                ReadOnlySpan<byte> inputRowSpan = inputSpan.Slice(rowSize * y, width * pixelSize);
                ReadOnlySpan<RGB24> rgb24s = MemoryMarshal.Cast<byte, RGB24>(inputRowSpan);

                Span<byte> outputSpan = result;
                Span<byte> l8s = outputSpan.Slice(resultRowSize * y, resultRowSize);

                for (int x = 0; x < width; ++x)
                {
                    ref readonly RGB24 pixel = ref rgb24s[x];
                    RGB rgb = pixel;
                    HSV hsv = converter.Convert(rgb);

                    // double h = Math.Round(hsv.H / 90.0) / 4.0;
                    double l = Math.Round(hsv.V / 0.1) / 10.0;
                    double s = Math.Round(hsv.S / 0.05) / 20.0;

                    double v = 255 * (s * 0.7 + l * 0.3);

                    if (v > 255)
                    {
                        v = 255;
                    }
                    else if (v < 0)
                    {
                        v = 0;
                    }

                    l8s[x] = (byte)v;
                }
            });

            return result;
        }

        public struct CIE
        {
            /// <summary>
            /// Y channel
            /// </summary>
            public float L;

            /// <summary>
            /// I channel
            /// </summary>
            public float A;

            /// <summary>
            /// Q channel
            /// </summary>
            public float B;
        }


        /// <summary>
        /// Конвертировать в серый цвет (RGB24 -> L8)
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static byte[] ConvertRGB24toL8(byte[] bytes, int width, int height)
        {
            // размер входного пикселя в байтах
            int pixelSize = GetPixelSize(ColorDepths.Bits8, 3);
            int rowSize = GetRowSize(bytes, width, height, ColorDepths.Bits8, 3);
            int resultPixelSize = GetPixelSize(ColorDepths.Bits8, 1);
            int resultRowSize = resultPixelSize * width;

            // Буфер для результата
            byte[] result = new byte[resultRowSize * height];

            Parallel.For(0, height, y =>
            {
                ReadOnlySpan<byte> inputSpan = bytes;
                ReadOnlySpan<byte> inputRowSpan = inputSpan.Slice(rowSize * y, width * pixelSize);
                ReadOnlySpan<RGB24> rgb24s = MemoryMarshal.Cast<byte, RGB24>(inputRowSpan);

                Span<byte> outputSpan = result;
                Span<byte> l8s = outputSpan.Slice(resultRowSize * y, resultRowSize);

                for (int x = 0; x < width; ++x)
                {
                    ref readonly RGB24 pixel = ref rgb24s[x];
                    l8s[x] = Get8BitBT709Luminance(pixel.R, pixel.G, pixel.B);
                }
            });

            return result;
        }



        /// <summary>
        /// Конвертировать в серый цвет (RGB48 -> L16)
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static byte[] ConvertRGB48toL16(byte[] bytes, int width, int height)
        {
            // размер входного пикселя в байтах
            int pixelSize = GetPixelSize(ColorDepths.Bits16, 3);
            int rowSize = GetRowSize(bytes, width, height, ColorDepths.Bits16, 3);
            int resultPixelSize = GetPixelSize(ColorDepths.Bits16, 1);
            int resultRowSize = resultPixelSize * width;

            // Буфер для результата
            byte[] result = new byte[resultRowSize * height];

            Parallel.For(0, height, y =>
            {
                ReadOnlySpan<byte> inputSpan = bytes;
                ReadOnlySpan<byte> inputRowSpan = inputSpan.Slice(rowSize * y, width * pixelSize);
                ReadOnlySpan<RGB48> rgb48s = MemoryMarshal.Cast<byte, RGB48>(inputRowSpan);

                Span<byte> outputSpan = result;
                Span<byte> outputRowSpan = outputSpan.Slice(resultRowSize * y, resultRowSize);
                Span<ushort> l16s = MemoryMarshal.Cast<byte, ushort>(outputRowSpan);

                for (int x = 0; x < width; ++x)
                {
                    ref readonly RGB48 pixel = ref rgb48s[x];
                    l16s[x] = Get16BitBT709Luminance(pixel.R, pixel.G, pixel.B);
                }
            });

            return result;
        }

        /// <summary>
        /// Конвертировать в серый цвет (RGB48 -> L8)
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static byte[] ConvertRGB48toL8(byte[] bytes, int width, int height)
        {
            // размер входного пикселя в байтах
            int pixelSize = GetPixelSize(ColorDepths.Bits16, 3);
            int rowSize = GetRowSize(bytes, width, height, ColorDepths.Bits16, 3);
            int resultPixelSize = GetPixelSize(ColorDepths.Bits8, 1);
            int resultRowSize = resultPixelSize * width;

            // Буфер для результата
            byte[] result = new byte[resultRowSize * height];

            Parallel.For(0, height, y =>
            {
                ReadOnlySpan<byte> inputSpan = bytes;
                ReadOnlySpan<byte> inputRowSpan = inputSpan.Slice(rowSize * y, width * pixelSize);
                ReadOnlySpan<RGB48> rgb48s = MemoryMarshal.Cast<byte, RGB48>(inputRowSpan);

                Span<byte> outputSpan = result;
                Span<byte> l8s = outputSpan.Slice(resultRowSize * y, resultRowSize);

                for (int x = 0; x < width; ++x)
                {
                    ref readonly RGB48 pixel = ref rgb48s[x];
                    l8s[x] = DownScaleFrom16BitTo8Bit(Get16BitBT709Luminance(pixel.R, pixel.G, pixel.B));
                }
            });

            return result;
        }

        #endregion

        #region RGB Conversions

        /// <summary>
        /// Конвертировать RGB48 -> RGB24
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static byte[] ConvertRGB48toRGB24(byte[] bytes, int width, int height)
        {
            // размер входного пикселя в байтах
            int pixelSize = GetPixelSize(ColorDepths.Bits16, 3);
            int rowSize = GetRowSize(bytes, width, height, ColorDepths.Bits16, 3);
            int resultPixelSize = GetPixelSize(ColorDepths.Bits8, 3);
            int resultRowSize = resultPixelSize * width;

            // Буфер для результата
            byte[] result = new byte[resultRowSize * height];

            Parallel.For(0, height, y =>
            {
                ReadOnlySpan<byte> inputSpan = bytes;
                ReadOnlySpan<byte> inputRowSpan = inputSpan.Slice(rowSize * y, width * pixelSize);
                ReadOnlySpan<RGB48> rgb48s = MemoryMarshal.Cast<byte, RGB48>(inputRowSpan);

                Span<byte> outputSpan = result;
                Span<byte> outputRowSpan = outputSpan.Slice(resultRowSize * y, resultRowSize);
                Span<RGB24> rgb24s = MemoryMarshal.Cast<byte, RGB24>(outputRowSpan);

                for (int x = 0; x < width; ++x)
                {
                    ref readonly RGB48 pixel = ref rgb48s[x];
                    byte r = DownScaleFrom16BitTo8Bit(pixel.R);
                    byte g = DownScaleFrom16BitTo8Bit(pixel.G);
                    byte b = DownScaleFrom16BitTo8Bit(pixel.B);
                    rgb24s[x] = new RGB24(r, g, b);
                }
            });

            return result;
        }

        /// <summary>
        /// Поменять местами B и R каналы (например, конвертировать BGR24 в RGB24)
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static int ExchangeBandRChannels8(byte[] bytes, int width, int height)
        {
            // размер входного пикселя в байтах
            int pixelSize = GetPixelSize(ColorDepths.Bits8, 3);
            int rowSize = GetRowSize(bytes, width, height, ColorDepths.Bits8, 3);

            Parallel.For(0, height, y =>
            {
                Span<byte> inputSpan = bytes;
                Span<byte> inputRowSpan = inputSpan.Slice(rowSize * y, width * pixelSize);
                Span<RGB24> rgb24s = MemoryMarshal.Cast<byte, RGB24>(inputRowSpan);

                for (int x = 0; x < width; ++x)
                {
                    ref RGB24 pixel = ref rgb24s[x];
                    byte pixelB = pixel.B;
                    pixel.B = pixel.R;
                    pixel.R = pixelB;
                }
            });

            return bytes.Length;
        }

        /// <summary>
        /// Поменять местами B и R каналы (например, конвертировать BGR48 в RGB48)
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static int ExchangeBandRChannels16(byte[] bytes, int width, int height)
        {
            // размер входного пикселя в байтах
            int pixelSize = GetPixelSize(ColorDepths.Bits16, 3);
            int rowSize = GetRowSize(bytes, width, height, ColorDepths.Bits16, 3);


            Parallel.For(0, height, y =>
            {
                Span<byte> inputSpan = bytes;
                Span<byte> inputRowSpan = inputSpan.Slice(rowSize * y, width * pixelSize);
                Span<RGB48> rgb48s = MemoryMarshal.Cast<byte, RGB48>(inputRowSpan);

                for (int x = 0; x < width; ++x)
                {
                    ref RGB48 pixel = ref rgb48s[x];
                    ushort pixelB = pixel.B;
                    pixel.B = pixel.R;
                    pixel.R = pixelB;
                }
            });

            return bytes.Length;
        }

        #endregion


        /// <summary>
        /// Calculate white balance gains using gray world algorithm over image window
        /// </summary>
        /// <returns>White balance gains</returns>
        public static RgbCoeffs AwbWindowGreyWorld(byte[] bytes, int width, int height, OpenCvSharp.Rect windowRect)
        {
            // размер входного пикселя в байтах
            int pixelSize = GetPixelSize(ColorDepths.Bits16, 3);
            int rowSize = GetRowSize(bytes, width, height, ColorDepths.Bits16, 3);

            long Ravg = 0;
            long Gavg = 0;
            long Bavg = 0;

            int numPixels = windowRect.Width * windowRect.Height;

            RgbCoeffs awbCoeffs = RgbCoeffs.Zero;
            ReadOnlySpan<byte> inputSpan = bytes;

            for (int y = windowRect.Y; y < windowRect.Y + windowRect.Height; ++y)
            {
                ReadOnlySpan<byte> inputRowSpan = inputSpan.Slice(rowSize * y, width * pixelSize);
                ReadOnlySpan<RGB48> rgb48s = MemoryMarshal.Cast<byte, RGB48>(inputRowSpan);

                for (int x = windowRect.X; x < windowRect.X + windowRect.Width; ++x)
                {
                    ref readonly RGB48 pixel = ref rgb48s[x];

                    Ravg += pixel.R;
                    Gavg += pixel.G;
                    Bavg += pixel.B;
                }
            }

            Ravg /= numPixels;
            Gavg /= numPixels;
            Bavg /= numPixels;

            awbCoeffs.GreenCoeff = 1 << RgbCoeffs.FractionBits;

            double Ravg_temp = Ravg / (double)(1 << RgbCoeffs.FractionBits);
            double Gavg_temp = Gavg / (double)(1 << RgbCoeffs.FractionBits);
            double Bavg_temp = Bavg / (double)(1 << RgbCoeffs.FractionBits);

            double corrR_temp = Gavg_temp / Ravg_temp;
            double corrB_temp = Gavg_temp / Bavg_temp;

            awbCoeffs.RedCoeff = (int)(corrR_temp * (1 << RgbCoeffs.FractionBits));
            awbCoeffs.BlueCoeff = (int)(corrB_temp * (1 << RgbCoeffs.FractionBits));

            return awbCoeffs;
        }

        public static readonly RgbCoeffs WBCoeffs = new RgbCoeffs(8050, 4096, 8050);

        public static void ApplyColorGainRGB48(byte[] bytes, int width, int height, RgbCoeffs colorGains)
        {
            // размер входного пикселя в байтах
            int pixelSize = GetPixelSize(ColorDepths.Bits16, 3);
            int rowSize = GetRowSize(bytes, width, height, ColorDepths.Bits16, 3);

            Parallel.For(0, height, y =>
            {
                Span<byte> inputSpan = bytes;
                Span<byte> inputRowSpan = inputSpan.Slice(rowSize * y, width * pixelSize);
                Span<RGB48> rgb48s = MemoryMarshal.Cast<byte, RGB48>(inputRowSpan);

                for (int x = 0; x < width; ++x)
                {
                    ref RGB48 pixel = ref rgb48s[x];
                    pixel.R = PixelRound(colorGains.RedCoeff * (long)pixel.R >> RgbCoeffs.FractionBits);
                    pixel.G = PixelRound(colorGains.GreenCoeff * (long)pixel.G >> RgbCoeffs.FractionBits);
                    pixel.B = PixelRound(colorGains.BlueCoeff * (long)pixel.B >> RgbCoeffs.FractionBits);
                }
            });
        }

        // <summary>
        /// Round pixel calculated value with saturation
        /// </summary>
        /// <param name="a">Pixel value to pack in unsigned 16 bits</param>
        /// <returns>Packed (rounded) value</returns>
        private static ushort PixelRound(long a)
        {
            a = Math.Min(a, ushort.MaxValue);
            a = Math.Max(a, ushort.MinValue);

            return (ushort)a;
        }
    }
}
