using GpuTest;
using GpuTest.Utils.Color;
using ImageMagick;
using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace OpenCvSharp
{
    /// <summary>
    /// A loader of OpenCvSharp Mats
    /// </summary>
    public static class MatLoader
    {
        #region Convert To

        /// <summary>
        /// Преобразовать цветное изображение OpenCV <see cref="Mat"/> 
        /// с прозрачностью или без и глубиной цвета 16 или 8 бит в 
        /// ImageMagick <see cref="MagickImage"/>
        /// </summary>        
        public static MagickImage ConvertToMagickImage(this Mat mat)
        {
            byte[] imageBytes;
            int depth;
            MagickFormat magickFormat;

            MatType matType = mat.Type();
            if (matType == MatType.CV_8UC4)
            {
                imageBytes = MatLoader.RgbaFromMat32(mat);
                depth = 8;
                magickFormat = MagickFormat.Rgba;
            }
            else if (matType == MatType.CV_16UC4)
            {
                imageBytes = MatLoader.RgbaFromMat64(mat);
                depth = 16;
                magickFormat = MagickFormat.Rgba;
            }
            else if (matType == MatType.CV_8UC3)
            {
                imageBytes = MatLoader.RgbFromMat24(mat);
                depth = 8;
                magickFormat = MagickFormat.Rgb;
            }
            else if (matType == MatType.CV_16UC3)
            {
                imageBytes = MatLoader.RgbFromMat48(mat);
                depth = 16;
                magickFormat = MagickFormat.Rgb;
            }
            else
            {
                throw new NotImplementedException(
                    $@"Преобразование изображения из OpenCV формата {matType} в ImageMagick не реализовано");
            }

            MagickImage convertedImage =
                new MagickImage(
                    imageBytes,
                    new MagickReadSettings()
                    {
                        Height = mat.Height,
                        Width = mat.Width,
                        Format = magickFormat,
                        Depth = depth
                    });

            return convertedImage;
        }

        /// <summary>
        /// Загрузить байты в формате RGBA32
        /// в BGRA изображение OpenCV
        /// </summary>
        private static Mat LoadRGBA32ToOpenCVBGRA(byte[] bytes, int width, int height)
        {
            int pixelSize = 4;
            int rowWidth = width * pixelSize;
            int expectedSize = rowWidth * height;

            if (bytes.Length != expectedSize)
            {
                throw new InvalidOperationException(
                    $"Invalid bytes length: {bytes.Length}, expected {expectedSize}");
            }            

            Vec4b[,] matArray = new Vec4b[height, width];

            Parallel.For(0, height, y =>
            {
                Span<byte> span = bytes;
                Span<byte> rowSpan = span.Slice(rowWidth * y, width * pixelSize);
                Span<RGBA32> RGBA32s = MemoryMarshal.Cast<byte, RGBA32>(rowSpan);

                for (int x = 0; x < width; ++x)
                {
                    ref readonly RGBA32 pixel = ref RGBA32s[x];
                    Vec4b newValue = new Vec4b(pixel.B, pixel.G, pixel.R, pixel.A);
                    matArray[y, x] = newValue;
                }
            });

            Mat mat = new Mat(height, width, MatType.CV_8UC4, matArray);
            return mat;
        }

        /// <summary>
        /// Загрузить байты в формате RGB24
        /// в BGR изображение OpenCV
        /// </summary>
        private static Mat LoadRGB24ToOpenCVBGR(byte[] bytes, int width, int height)
        {
            int pixelSize = 3;
            int rowWidth = width * pixelSize;
            int expectedSize = rowWidth * height;

            if (bytes.Length < expectedSize)
            {
                throw new InvalidOperationException($"Invalid bytes length: {bytes.Length}, expected {expectedSize}");
            }

            // rows with padding
            if (bytes.Length > expectedSize)
            {
                rowWidth = (int)RgbConverter.TDIBWIDTHBYTES(ColorDepths.Bits8, (uint)width);
                expectedSize = rowWidth * height;
                if (bytes.Length != expectedSize)
                    throw new InvalidOperationException($"Invalid bytes length: {bytes.Length}, expected {expectedSize}");
            }

            Vec3b[,] matArray = new Vec3b[height, width];

            Parallel.For(0, height, y =>
            {
                Span<byte> span = bytes;
                Span<byte> rowSpan = span.Slice(rowWidth * y, width * pixelSize);
                Span<RGB24> RGB24s = MemoryMarshal.Cast<byte, RGB24>(rowSpan);

                for (int x = 0; x < width; ++x)
                {
                    ref readonly RGB24 pixel = ref RGB24s[x];
                    Vec3b newValue = new Vec3b(pixel.B, pixel.G, pixel.R);
                    matArray[y, x] = newValue;
                }
            });

            Mat mat = new Mat(height, width, MatType.CV_8UC3, matArray);
            return mat;
        }

        /// <summary>
        /// Загрузить байты в формате RGBA64
        /// в BGRA изображение OpenCV
        /// </summary>
        private static Mat LoadRGBA64ToOpenCVBGRA(byte[] bytes, int width, int height)
        {
            int pixelSize = 8;
            int rowWidth = width * pixelSize;
            int expectedSize = rowWidth * height;

            if (bytes.Length != expectedSize)
            {
                throw new InvalidOperationException(
                    $"Invalid bytes length: {bytes.Length}, expected {expectedSize}");
            }

            Vec4w[,] matArray = new Vec4w[height, width];

            Parallel.For(0, height, y =>
            {
                Span<byte> span = bytes;
                Span<byte> rowSpan = span.Slice(rowWidth * y, width * pixelSize);
                Span<RGBA64> rgbas = MemoryMarshal.Cast<byte, RGBA64>(rowSpan);

                for (int x = 0; x < width; ++x)
                {
                    ref readonly RGBA64 pixel = ref rgbas[x];
                    Vec4w newValue = new Vec4w(pixel.B, pixel.G, pixel.R, pixel.A);
                    matArray[y, x] = newValue;
                }
            });

            Mat mat = new Mat(height, width, MatType.CV_16UC4, matArray);
            return mat;
        }

        /// <summary>
        /// Загрузить байты в формате RGB48
        /// в BGR изображение OpenCV
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private static Mat LoadRGB48ToOpenCVBGR(byte[] bytes, int width, int height)
        {
            int pixelSize = 6;
            int rowWidth = width * pixelSize;
            int expectedSize = rowWidth * height;

            if (bytes.Length < expectedSize)
            {
                throw new InvalidOperationException($"Invalid bytes length: {bytes.Length}, expected {expectedSize}");
            }

            // rows with padding
            if (bytes.Length > expectedSize)
            {
                rowWidth = (int)RgbConverter.TDIBWIDTHBYTES(ColorDepths.Bits16, (uint)width);
                expectedSize = rowWidth * height;
                if (bytes.Length != expectedSize)
                    throw new InvalidOperationException($"Invalid bytes length: {bytes.Length}, expected {expectedSize}");
            }

            Vec3w[,] matArray = new Vec3w[height, width];

            Parallel.For(0, height, y =>
            {
                Span<byte> span = bytes;
                Span<byte> rowSpan = span.Slice(rowWidth * y, width * pixelSize);
                Span<RGB48> RGB48s = MemoryMarshal.Cast<byte, RGB48>(rowSpan);

                for (int x = 0; x < width; ++x)
                {
                    ref readonly RGB48 pixel = ref RGB48s[x];
                    Vec3w newValue = new Vec3w(pixel.B, pixel.G, pixel.R);
                    matArray[y, x] = newValue;
                }
            });

            Mat mat = new Mat(height, width, MatType.CV_16UC3, matArray);
            return mat;
        }

        /// <summary>
        /// Загрузить байты в формате RGB в OpenCV BGR изображение
        /// </summary>
        public static Mat LoadRGBAToOpenCVBGRA(byte[] bytes, int width, int height, ColorDepths colorDepth)
        {
            switch (colorDepth)
            {
                case ColorDepths.Bits8:
                    return LoadRGBA32ToOpenCVBGRA(bytes, width, height);
                case ColorDepths.Bits16:
                    return LoadRGBA64ToOpenCVBGRA(bytes, width, height);
                default:
                    throw new ArgumentOutOfRangeException(nameof(colorDepth));
            }
        }

        /// <summary>
        /// Загрузить байты в формате RGB в OpenCV BGR изображение
        /// </summary>
        public static Mat LoadRGBToOpenCVBGR(
            byte[] bytes,
            int width,
            int height,
            ColorDepths colorDepth)
        {
            switch (colorDepth)
            {
                case ColorDepths.Bits8:
                   return LoadRGB24ToOpenCVBGR(bytes, width, height);
                case ColorDepths.Bits16:
                   return LoadRGB48ToOpenCVBGR(bytes, width, height);
                default:
                    throw new ArgumentOutOfRangeException(nameof(colorDepth));
            }
        }

        /// <summary>
        /// Загрузить байты в формате RGB в OpenCV BGR изображение
        /// </summary>
        public static Mat LoadBGRAToOpenCV(byte[] bytes, int width, int height, ColorDepths colorDepth)
        {
            switch (colorDepth)
            {
                case ColorDepths.Bits8:
                    return LoadRGB24ToOpenCVBGR(bytes, width, height);
                case ColorDepths.Bits16:
                    return LoadRGB48ToOpenCVBGR(bytes, width, height);
                default:
                    throw new ArgumentOutOfRangeException(nameof(colorDepth));
            }
        }

        /// <summary>
        /// Загружает байты в формате L8 (Серый)
        /// </summary>
        /// <returns></returns>
        private static Mat LoadL8(byte[] bytes, int width, int height)
        {
            int pixelSize = 1;
            int rowWidth = width * pixelSize;
            int expectedSize = rowWidth * height;

            if (bytes.Length < expectedSize)
            {
                throw new InvalidOperationException($"Invalid bytes length: {bytes.Length}, expected {expectedSize}");
            }

            // rows with padding
            if (bytes.Length > expectedSize)
            {
                rowWidth = (int)RgbConverter.TDIBWIDTHBYTES(ColorDepths.Bits8, (uint)width);
                expectedSize = rowWidth * height;
                if (bytes.Length != expectedSize)
                    throw new InvalidOperationException($"Invalid bytes length: {bytes.Length}, expected {expectedSize}");
            }

            byte[,] matArray = new byte[height, width];

            Parallel.For(0, height, y =>
            {
                Span<byte> span = bytes;
                Span<byte> rowSpan = span.Slice(rowWidth * y, width * pixelSize);

                for (int x = 0; x < width; ++x)
                {
                    matArray[y, x] = rowSpan[x];
                }
            });

            Mat mat = new Mat(height, width, MatType.CV_8UC1, matArray);
            return mat;
        }

        /// <summary>
        /// Загружает байты в формате L16 (Серый 16 бит)
        /// </summary>
        /// <returns></returns>
        private static Mat LoadL16(byte[] bytes, int width, int height)
        {
            int pixelSize = 2;
            int rowWidth = width * pixelSize;
            int expectedSize = rowWidth * height;

            if (bytes.Length < expectedSize)
            {
                throw new InvalidOperationException($"Invalid bytes length: {bytes.Length}, expected {expectedSize}");
            }

            // rows with padding
            if (bytes.Length > expectedSize)
            {
                rowWidth = (int)RgbConverter.TDIBWIDTHBYTES(ColorDepths.Bits16, (uint)width);
                expectedSize = rowWidth * height;
                if (bytes.Length != expectedSize)
                    throw new InvalidOperationException($"Invalid bytes length: {bytes.Length}, expected {expectedSize}");
            }

            ushort[,] matArray = new ushort[height, width];

            Parallel.For(0, height, y =>
            {
                Span<byte> span = bytes;
                Span<byte> rowSpan = span.Slice(rowWidth * y, width * pixelSize);
                Span<ushort> ushorts = MemoryMarshal.Cast<byte, ushort>(rowSpan);

                for (int x = 0; x < width; ++x)
                {
                    matArray[y, x] = ushorts[x];
                }
            });

            Mat mat = new Mat(height, width, MatType.CV_16UC1, matArray);
            return mat;
        }

        /// <summary>
        /// Загрузить байты одноканального серого изображения
        /// </summary>
        public static Mat LoadGray(byte[] bytes, int width, int height, ColorDepths colorDepth)
        {
            switch (colorDepth)
            {
                case ColorDepths.Bits8:
                    return LoadL8(bytes, width, height);
                case ColorDepths.Bits16:
                    return LoadL16(bytes, width, height);
                default:
                    throw new ArgumentOutOfRangeException(nameof(colorDepth));
            }
        }
        #endregion

        #region Convert From To Magick

        /// <summary>
        /// Конвертирует матрицу в MagickImage изображение
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static MagickImage MagickFromMat(Mat mat)
        {
            if (mat.Type() == MatType.CV_8UC1)
                return MagickFromMatL8(mat);
            else if (mat.Type() == MatType.CV_16UC1)
                return MagickFromMatL16(mat);
            else if (mat.Type() == MatType.CV_8UC3)
                return MagickFromMat24(mat);
            else if (mat.Type() == MatType.CV_16UC3)
                return MagickFromMat48(mat);
            else
                throw new ArgumentOutOfRangeException(nameof(mat.Type));
        }


        /// <summary>
        /// Конвертирует матрицу в MagickImage изображение
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static MagickImage MagickFromMatL8(Mat mat)
        {
            int width = mat.Cols;
            int height = mat.Rows;
            int pixelSize = 1;
            int rowWidth = width * pixelSize;
            int expectedSize = rowWidth * height;
            byte[] bytes = ArrayPool<byte>.Shared.Rent(expectedSize);

            try
            {
                mat.GetRectangularArray(out byte[,] matArray);

                Buffer.BlockCopy(matArray, 0, bytes, 0, rowWidth * height);

                var rgbImage = new MagickImage(bytes, new PixelReadSettings(width, height, StorageType.Char, "R"));

                return rgbImage;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }

        /// <summary>
        /// Конвертирует матрицу в MagickImage изображение
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static MagickImage MagickFromMatL16(Mat mat)
        {
            int width = mat.Cols;
            int height = mat.Rows;
            int pixelSize = 2;
            int rowWidth = width * pixelSize;
            int expectedSize = rowWidth * height;
            byte[] bytes = ArrayPool<byte>.Shared.Rent(expectedSize);

            try
            {
                mat.GetRectangularArray(out ushort[,] matArray);

                Buffer.BlockCopy(matArray, 0, bytes, 0, rowWidth * height);

                var rgbImage = new MagickImage(bytes, new PixelReadSettings(width, height, StorageType.Short, "R"));

                return rgbImage;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }

        /// <summary>
        /// Конвертирует матрицу в MagickImage изображение
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static MagickImage MagickFromMat24(Mat mat)
        {
            int width = mat.Cols;
            int height = mat.Rows;
            int pixelSize = 3;
            int rowWidth = width * pixelSize;
            int expectedSize = rowWidth * height;
            byte[] bytes = ArrayPool<byte>.Shared.Rent(expectedSize);

            try
            {
                mat.GetRectangularArray(out Vec3b[,] matArray);

                Parallel.For(0, height, y =>
                {
                    Span<byte> span = bytes;
                    Span<byte> rowSpan = span.Slice(rowWidth * y, width * pixelSize);
                    Span<RGB24> rgbs = MemoryMarshal.Cast<byte, RGB24>(rowSpan);

                    for (int x = 0; x < width; ++x)
                    {
                        Vec3b vec = matArray[y, x];
                        ref RGB24 pixel = ref rgbs[x];
                        pixel.B = vec.Item0;
                        pixel.G = vec.Item1;
                        pixel.R = vec.Item2;
                    }
                });

                var rgbImage = new MagickImage(bytes, new PixelReadSettings(width, height, StorageType.Char, PixelMapping.RGB));

                return rgbImage;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }


        /// <summary>
        /// Конвертирует матрицу в MagickImage изображение
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static MagickImage MagickFromMat48(Mat mat)
        {
            int width = mat.Cols;
            int height = mat.Rows;
            int pixelSize = 6;
            int rowWidth = width * pixelSize;
            int expectedSize = rowWidth * height;
            byte[] bytes = ArrayPool<byte>.Shared.Rent(expectedSize);

            try
            {
                mat.GetRectangularArray(out Vec3w[,] matArray);

                Parallel.For(0, height, y =>
                {
                    Span<byte> span = bytes;
                    Span<byte> rowSpan = span.Slice(rowWidth * y, width * pixelSize);
                    Span<RGB48> rgbs = MemoryMarshal.Cast<byte, RGB48>(rowSpan);

                    for (int x = 0; x < width; ++x)
                    {
                        Vec3w vec = matArray[y, x];
                        ref RGB48 pixel = ref rgbs[x];
                        pixel.B = vec.Item0;
                        pixel.G = vec.Item1;
                        pixel.R = vec.Item2;
                    }
                });

                var rgbImage = new MagickImage(bytes, new PixelReadSettings(width, height, StorageType.Short, PixelMapping.RGB));

                return rgbImage;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }

        #endregion

    
        /// <summary>
        /// Конвертирует матрицу в RGB байты
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static byte[] RgbFromMat24(Mat mat)
        {
            int width = mat.Cols;
            int height = mat.Rows;
            int pixelSize = 3;
            int rowWidth = width * pixelSize;
            int expectedSize = rowWidth * height;
            byte[] bytes = new byte[expectedSize];

            mat.GetRectangularArray(out Vec3b[,] matArray);

            Parallel.For(0, height, y =>
            {
                Span<byte> span = bytes;
                Span<byte> rowSpan = span.Slice(rowWidth * y, width * pixelSize);
                Span<RGB24> rgbs = MemoryMarshal.Cast<byte, RGB24>(rowSpan);

                for (int x = 0; x < width; ++x)
                {
                    Vec3b vec = matArray[y, x];
                    ref RGB24 pixel = ref rgbs[x];
                    pixel.B = vec.Item0;
                    pixel.G = vec.Item1;
                    pixel.R = vec.Item2;
                }
            });

            return bytes;
        }

        /// <summary>
        /// Конвертирует матрицу в RGBA байты
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static byte[] RgbaFromMat32(Mat mat)
        {
            int width = mat.Cols;
            int height = mat.Rows;
            int pixelSize = 4;
            int rowWidth = width * pixelSize;
            int expectedSize = rowWidth * height;
            byte[] bytes = new byte[expectedSize];

            mat.GetRectangularArray(out Vec4b[,] matArray);

            Parallel.For(0, height, y =>
            {
                Span<byte> span = bytes;
                Span<byte> rowSpan = span.Slice(rowWidth * y, width * pixelSize);
                Span<RGBA32> rgbs = MemoryMarshal.Cast<byte, RGBA32>(rowSpan);

                for (int x = 0; x < width; ++x)
                {
                    Vec4b vec = matArray[y, x];
                    ref RGBA32 pixel = ref rgbs[x];
                    pixel.B = vec.Item0;
                    pixel.G = vec.Item1;
                    pixel.R = vec.Item2;
                    pixel.A = vec.Item3;
                }
            });

            return bytes;
        }

        /// <summary>
        /// Конвертирует матрицу в RGBA байты
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static byte[] RgbaFromMat64(Mat mat)
        {
            int width = mat.Cols;
            int height = mat.Rows;
            int pixelSize = 8;
            int rowWidth = width * pixelSize;
            int expectedSize = rowWidth * height;
            byte[] bytes = new byte[expectedSize];

            mat.GetRectangularArray(out Vec4w[,] matArray);

            Parallel.For(0, height, y =>
            {
                Span<byte> span = bytes;
                Span<byte> rowSpan = span.Slice(rowWidth * y, width * pixelSize);
                Span<RGBA64> rgbs = MemoryMarshal.Cast<byte, RGBA64>(rowSpan);

                for (int x = 0; x < width; ++x)
                {
                    Vec4w vec = matArray[y, x];
                    ref RGBA64 pixel = ref rgbs[x];
                    pixel.B = vec.Item0;
                    pixel.G = vec.Item1;
                    pixel.R = vec.Item2;
                    pixel.A = vec.Item3;
                }
            });

            return bytes;
        }

        /// <summary>
        /// Конвертирует матрицу в RGB байты
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static byte[] RgbFromMat48(Mat mat)
        {
            int width = mat.Cols;
            int height = mat.Rows;
            int pixelSize = 6;
            int rowWidth = width * pixelSize;
            int expectedSize = rowWidth * height;
            byte[] bytes = new byte[expectedSize];

            mat.GetRectangularArray(out Vec3w[,] matArray);

            Parallel.For(0, height, y =>
            {
                Span<byte> span = bytes;
                Span<byte> rowSpan = span.Slice(rowWidth * y, width * pixelSize);
                Span<RGB48> rgbs = MemoryMarshal.Cast<byte, RGB48>(rowSpan);

                for (int x = 0; x < width; ++x)
                {
                    Vec3w vec = matArray[y, x];
                    ref RGB48 pixel = ref rgbs[x];
                    pixel.B = vec.Item0;
                    pixel.G = vec.Item1;
                    pixel.R = vec.Item2;
                }
            });

            return bytes;
        }
    }
}
