using GpuTest;
using GpuTest.Utils.Color;
using ILGPU.Algorithms;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static GpuTest.CropHelper;

namespace CrossImage.Common.Imaging
{
    /// <summary>
    /// Класс для методов уменьшения размера RGB изображения
    /// </summary>
    public static class RgbScaleHelper
    {

        /// <summary>
        /// Возвращает весовые коэффициенты пикселов для различных масштабов уменьшения
        /// </summary>
        /// <param name="factor"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private static float[] GetScaleWeights(float factor, float sigma = 3)
        {
            // precalculation of gaussian coefficients 
            int radius = (int)Math.Floor(factor / 2);
            int BUFF_SIZE = radius * 2 + 1;
            float[] cGaussian = new float[BUFF_SIZE];
            for (int i = 0; i < BUFF_SIZE; ++i)
            {
                float x = i - radius;
                cGaussian[i] = XMath.Exp(-(x * x) / (2 * sigma * sigma));
            }
            return cGaussian;
        }

        /// <summary>
        /// Уменьшает размер RGB изображения в N раз
        /// </summary>
        /// <returns>balance gains</returns>
        public static ImageBytesResult DownScaleRgb(byte[] bytes, int width, int height, float factor, ColorDepths colorDepth)
        {
            if (factor <= 1)
            {
                return new ImageBytesResult() { Bytes = bytes, Width = width, Height = height };
            }
            if (factor > 8)
            {
                factor = 8;
            }

            return colorDepth switch
            {
                ColorDepths.Bits8 => DownScaleRgb24(bytes, width, height, factor),
                ColorDepths.Bits16 => DownScaleRgb48(bytes, width, height, factor),
                _ => throw new ArgumentOutOfRangeException(nameof(colorDepth))
            };
        }

        /// <summary>
        /// Уменьшает размер 8 битного RGB изображения в N раз
        /// </summary>
        /// <returns>balance gains</returns>
        private static ImageBytesResult DownScaleRgb24(byte[] bytes, int width, int height, float factor)
        {
            float[] coeff = GetScaleWeights(factor);

            // размер входного пикселя в байтах
            const int pixelSize = 3;

            int width2 = (int)MathF.Floor(width / factor);
            int height2 = (int)MathF.Floor(height / factor);
            int rowSize = width * pixelSize;
            int imageSize = rowSize * height;
            int rowSize2 = width2 * pixelSize;
            byte[] result = new byte[rowSize2 * height2];
            int RAD = (int)Math.Floor(factor / 2);

            Parallel.For(0, height2, j =>
            //for(int j=0; j < height2; ++j)
            {
                int startY = (int)MathF.Floor(j * factor);

                ReadOnlySpan<byte> inputSpan = bytes;
                Span<byte> resultSpan = result;

                for (int i = 0; i < width2; ++i)
                {
                    int startX = (int)MathF.Floor(i * factor);

                    float r = 0;
                    float g = 0;
                    float b = 0;
                    float sum = 0;

                    for (int rd = -RAD; rd <= RAD; rd++)
                    {
                        int y = startY + rd;

                        if (y < 0 || y >= height)
                            continue;

                        ReadOnlySpan<byte> row = inputSpan.Slice(rowSize * y, rowSize);
                        ReadOnlySpan<RGB24> rowPixels = MemoryMarshal.Cast<byte, RGB24>(row);

                        for (int cd = -RAD; cd <= RAD; cd++)
                        {
                            int x = startX + cd;

                            if (x < 0 || x >= width)
                                continue;

                            RGB24 pixel = rowPixels[x];

                            float weight = coeff[rd + RAD] * coeff[cd + RAD];

                            r += pixel.R * weight;
                            g += pixel.G * weight;
                            b += pixel.B * weight;
                            sum += weight;
                        }
                    }
                   
                    Span<byte> row2 = resultSpan.Slice(rowSize2 * j, rowSize2);
                    Span<RGB24> rowPixels2 = MemoryMarshal.Cast<byte, RGB24>(row2);
                    ref RGB24 pixel2 = ref rowPixels2[i];

                    pixel2.R = (byte)(r / sum);
                    pixel2.G = (byte)(g / sum);
                    pixel2.B = (byte)(b / sum);
                }
            });


            return new ImageBytesResult() { Bytes = result, Width = width2, Height = height2 };
        }

        /// <summary>
        /// Уменьшает размер 16 битного RGB изображения в N раз
        /// </summary>
        /// <returns>balance gains</returns>
        private static ImageBytesResult DownScaleRgb48(byte[] bytes, int width, int height, float factor)
        {
            float[] coeff = GetScaleWeights(factor);

            // размер входного пикселя в байтах
            const int pixelSize = 3 * sizeof(ushort);

            int width2 = (int)MathF.Floor(width / factor);
            int height2 = (int)MathF.Floor(height / factor);
            int rowSize = width * pixelSize;
            int imageSize = rowSize * height;
            int rowSize2 = width2 * pixelSize;
            byte[] result = new byte[rowSize2 * height2];
            int RAD = (int)Math.Floor(factor / 2);

            Parallel.For(0, height2, j =>
            //for(int j=0; j < height2; ++j)
            {
                int startY = (int)MathF.Floor(j * factor);

                ReadOnlySpan<byte> inputSpan = bytes;
                Span<byte> resultSpan = result;

                for (int i = 0; i < width2; ++i)
                {
                    int startX = (int)MathF.Floor(i * factor);

                    float r = 0;
                    float g = 0;
                    float b = 0;
                    float sum = 0;

                    for (int rd = -RAD; rd <= RAD; rd++)
                    {
                        int y = startY + rd;

                        if (y < 0 || y >= height)
                            continue;

                        ReadOnlySpan<byte> row = inputSpan.Slice(rowSize * y, rowSize);
                        ReadOnlySpan<RGB48> rowPixels = MemoryMarshal.Cast<byte, RGB48>(row);

                        for (int cd = -RAD; cd <= RAD; cd++)
                        {
                            int x = startX + cd;

                            if (x < 0 || x >= width)
                                continue;

                            RGB48 pixel = rowPixels[x];

                            float weight = coeff[rd + RAD] * coeff[cd + RAD];

                            r += pixel.R * weight;
                            g += pixel.G * weight;
                            b += pixel.B * weight;
                            sum += weight;
                        }
                    }

                    Span<byte> row2 = resultSpan.Slice(rowSize2 * j, rowSize2);
                    Span<RGB48> rowPixels2 = MemoryMarshal.Cast<byte, RGB48>(row2);
                    ref RGB48 pixel2 = ref rowPixels2[i];

                    pixel2.R = (ushort)(r / sum);
                    pixel2.G = (ushort)(g / sum);
                    pixel2.B = (ushort)(b / sum);
                }
            });


            return new ImageBytesResult() { Bytes = result, Width = width2, Height = height2 };
        }
    }
}
