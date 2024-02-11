using GpuTest.Utils.Color;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace GpuTest
{
    /// <summary>
    /// Класс помошник для обрезки изображения
    /// </summary>
    public static class CropHelper
    {
        public struct ImageBytesResult
        {
            public byte[] Bytes { get; init; }
            public int Width { get; init; }
            public int Height { get; init; }
        }

        /// <summary>
        /// Обрезает изображение до нового размера
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static ImageBytesResult Crop<T>(byte[] bytes, int width, int height, OpenCvSharp.Rect cropRectangle)
            where T : struct
        {
            OpenCvSharp.Rect rect = new OpenCvSharp.Rect(0, 0, width, height);
            rect = rect.Intersect(cropRectangle);

            // размер входного пикселя в байтах
            int pixelSize = Marshal.SizeOf<T>();
            int rowSize = pixelSize * width;
            int resultPixelSize = pixelSize;
            int resultRowSize = resultPixelSize * rect.Width;

            // Буфер для результата
            byte[] result = new byte[resultRowSize * rect.Height];
            int X2 = rect.X + rect.Width;
            int Y2 = rect.Y + rect.Height;

            Parallel.For(rect.Y, Y2, y =>
            {
                int newY = y - rect.Y;

                ReadOnlySpan<byte> inputSpan = bytes;
                ReadOnlySpan<byte> inputRowSpan = inputSpan.Slice(rowSize * y, width * pixelSize);
                ReadOnlySpan<T> src = MemoryMarshal.Cast<byte, T>(inputRowSpan);

                Span<byte> outputSpan = result;
                Span<byte> outputRowSpan = outputSpan.Slice(resultRowSize * newY, resultRowSize);
                Span<T> dst = MemoryMarshal.Cast<byte, T>(outputRowSpan);
                
                var slice = src.Slice(rect.X, rect.Width);
                slice.CopyTo(dst);
            });

            return new ImageBytesResult() { Bytes = result, Width = rect.Width, Height = rect.Height };
        }

        /// <summary>
        /// Обрезает изображение RGB24 или RGB48 до нового размера, и переводит его в Gray8
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static ImageBytesResult CropToGray8(byte[] bytes, int width, int height, ColorDepths colorDepth, OpenCvSharp.Rect cropRectangle)
        {

            ImageBytesResult cropRes = colorDepth switch
            {
                ColorDepths.Bits8 => Crop<RGB24>(bytes, width, height, cropRectangle),
                ColorDepths.Bits16 => Crop<RGB48>(bytes, width, height, cropRectangle),
                _ => throw new ArgumentOutOfRangeException(nameof(colorDepth))
            };

            return colorDepth switch
            {
                ColorDepths.Bits8 => new ImageBytesResult() { Bytes = RgbConverter.ConvertRGB24toL8(cropRes.Bytes, cropRes.Width, cropRes.Height), Width = cropRes.Width, Height = cropRes.Height },
                ColorDepths.Bits16 => new ImageBytesResult() { Bytes = RgbConverter.ConvertRGB48toL8(cropRes.Bytes, cropRes.Width, cropRes.Height), Width = cropRes.Width, Height = cropRes.Height },
                _ => throw new ArgumentOutOfRangeException(nameof(colorDepth))
            };
        }
    }
}
