using GpuTest;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace GpuTest.Utils.Color
{
    /// <summary>
    /// Color converter between HSV and Rgb
    /// See <see href="http://www.poynton.com/PDFs/coloureq.pdf"/> for formulas.
    /// </summary>
    internal sealed class HsvAndRgbConverter
    {
        /// <summary>
        /// The epsilon value for comparing floating point numbers.
        /// </summary>
        public static readonly float Epsilon = 0.001F;

        /// <summary>
        /// The epsilon squared value for comparing floating point numbers.
        /// </summary>
        public static readonly float EpsilonSquared = Epsilon * Epsilon;


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

        /// <summary>
        /// Конвертировать (RGB48 -> HSV)
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static byte[] ConvertRGB48toHSV(byte[] bytes, int width, int height)
        {
            // размер входного пикселя в байтах
            int pixelSize = GetPixelSize(ColorDepths.Bits16, 3);
            int rowSize = pixelSize * width;
            int resultPixelSize = Marshal.SizeOf<HSV>();
            int resultRowSize = resultPixelSize * width;

            // Буфер для результата
            byte[] result = new byte[resultRowSize * height];
            var converter = new HsvAndRgbConverter();

            Parallel.For(0, height, y =>
            {
                ReadOnlySpan<byte> inputSpan = bytes;
                ReadOnlySpan<byte> inputRowSpan = inputSpan.Slice(rowSize * y, width * pixelSize);
                ReadOnlySpan<RGB48> rgbs = MemoryMarshal.Cast<byte, RGB48>(inputRowSpan);

                Span<byte> outputSpan = result;
                Span<byte> resRow = outputSpan.Slice(resultRowSize * y, resultRowSize);
                Span<HSV> res = MemoryMarshal.Cast<byte, HSV>(resRow);

                for (int x = 0; x < width; ++x)
                {
                    ref readonly RGB48 pixel = ref rgbs[x];
                    RGB rgb = pixel;
                    res[x] = converter.Convert(rgb);
                }
            });

            return result;
        }

        /// <summary>
        /// Конвертировать (HSV -> RGB48)
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static byte[] ConvertHSVtoRGB48(byte[] bytes, int width, int height)
        {
            // размер входного пикселя в байтах
            int pixelSize = Marshal.SizeOf<HSV>();
            int rowSize = pixelSize * width;
            int resultPixelSize = GetPixelSize(ColorDepths.Bits16, 3);
            int resultRowSize = resultPixelSize * width;

            // Буфер для результата
            byte[] result = new byte[resultRowSize * height];
            var converter = new HsvAndRgbConverter();

            Parallel.For(0, height, y =>
            {
                ReadOnlySpan<byte> inputSpan = bytes;
                ReadOnlySpan<byte> inputRowSpan = inputSpan.Slice(rowSize * y, width * pixelSize);
                ReadOnlySpan<HSV> hsvs = MemoryMarshal.Cast<byte, HSV>(inputRowSpan);

                Span<byte> outputSpan = result;
                Span<byte> resRow = outputSpan.Slice(resultRowSize * y, resultRowSize);
                Span<RGB48> res = MemoryMarshal.Cast<byte, RGB48>(resRow);

                for (int x = 0; x < width; ++x)
                {
                    ref readonly HSV pixel = ref hsvs[x];
                    RGB rgb = converter.Convert(pixel);
                    res[x] = (RGB48)rgb;
                }
            });

            return result;
        }

        /// <summary>
        /// Performs the conversion from the <see cref="HSV"/> input to an instance of <see cref="RGB"/> type.
        /// </summary>
        /// <param name="input">The input color instance.</param>
        /// <returns>The converted result</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public RGB Convert(in HSV input)
        {
            float s = input.S;
            float v = input.V;

            if (MathF.Abs(s) < Epsilon)
            {
                return new RGB(v, v, v);
            }

            float h = MathF.Abs(input.H - 360) < Epsilon ? 0 : input.H / 60;
            int i = (int)Math.Truncate(h);
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
        /// Performs the conversion from the <see cref="RGB"/> input to an instance of <see cref="HSV"/> type.
        /// </summary>
        /// <param name="input">The input color instance.</param>
        /// <returns>The converted result</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public HSV Convert(in RGB input)
        {
            float r = input.R;
            float g = input.G;
            float b = input.B;

            float max = MathF.Max(r, MathF.Max(g, b));
            float min = MathF.Min(r, MathF.Min(g, b));
            float chroma = max - min;
            float h = 0;
            float s = 0;
            float v = max;

            if (MathF.Abs(chroma) < Epsilon)
            {
                return new HSV(0, s, v);
            }

            if (MathF.Abs(r - max) < Epsilon)
            {
                h = (g - b) / chroma;
            }
            else if (MathF.Abs(g - max) < Epsilon)
            {
                h = 2 + (b - r) / chroma;
            }
            else if (MathF.Abs(b - max) < Epsilon)
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
