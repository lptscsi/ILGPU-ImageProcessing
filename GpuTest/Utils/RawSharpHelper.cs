using GpuTest;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static GpuTest.CropHelper;

namespace CrossImage.Common.Imaging
{
    /// <summary>
    /// Класс для методов уменьшения размера недебаеризованного изображения
    /// </summary>
    public static class RawSharpHelper
    {
        public static T[,] UnPack<T>(T[] data, int rows, int columns)
          where T : unmanaged
        {
            T[,] result = new T[rows, columns];
            int bytes = Buffer.ByteLength(data);
            Buffer.BlockCopy(data, 0, result, 0, bytes);
            return result;
        }

        public static T[] Pack<T>(T[,] data)
              where T : unmanaged
        {
            T[] result = new T[data.Length];
            int bytes = Buffer.ByteLength(data);
            Buffer.BlockCopy(data, 0, result, 0, bytes);
            return result;
        }


        #region Поля

        private static float[,] _baseCoeff = new float[3, 3] {
                {-0.125f,-0.125f,-0.125f},
                {-0.125f, 1.000f,-0.125f},
                {-0.125f,-0.125f,-0.125f}
            };

        /// <summary>
        /// Предварительно вычисленные  коэффициенты резкости с шагом 0.01
        /// </summary>
        private static float[][,] _coeff = new float[101][,];

        private static Func<double, double> _interpolate;

        private const int KERNEL_SIZE = 8;

        #endregion

        #region Кострукторы

        /// <summary>
        /// Статический конструктор
        /// </summary>
        static RawSharpHelper()
        {

            // Создаем коеффициенты для резкости от 0 до 1.0 с шагом 0.01
            for (int k = 0; k <= 100; k++)
            {
                float sharpen = k / 100f;

                float[,] data = _baseCoeff.Clone() as float[,];

                for (int i = 0; i < 3; ++i)
                {
                    for (int j = 0; j < 3; ++j)
                    {
                        data[i, j] = data[i, j] * sharpen;
                    }
                }

                data[1, 1] = data[1, 1] + 1;

                _coeff[k] = data;
            }

            (double x, double y)[] values = new (double x, double y)[] {
                 (0.0, 0.3)
                ,(0.1, 0.3)
                ,(0.2, 0.3)
                ,(0.3, 0.3)
                ,(0.4, 0.4)
                ,(0.5, 0.5)
                ,(0.6, 0.6)
                ,(0.7, 0.9)
                ,(0.8, 1.0)
                ,(0.9, 1.0)
                ,(1.0, 1.0)
            };
            var valuesList = values.ToList();

            // Сохраняем функцию для интерполяции резкости в зависимости от расстояния от центра
            _interpolate = MathUtils.InterpolatePiecewiseLinear(valuesList);
        }

        #endregion

        /// <summary>
        /// Returns sharpness weights with needed sharpnes ajustment [0, 1]
        /// </summary>
        /// <param name="sharpness"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float[,] GetCoeff(double sharpness)
        {
            int k = (int)(sharpness * 100);
            k = k > 100 ? 100 : k;
            return _coeff[k];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort Clamp(float value)
        {
            float result = value < 0 ? 0 : value;

            return (ushort)(result > (float)ushort.MaxValue ? ushort.MaxValue : result);
        }

        /// <summary>
        /// Round up division by formula res = a / b;
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int RoundUpDivide(int a, int b)
        {
            return (a - 1) / b + 1;
        }

        /// <summary>
        /// Применяет матрицу резкости на 16 битный RAW 
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="sharpen"></param>
        /// <returns></returns>
        public static ImageBytesResult SharpRaw16(byte[] bytes, int width, int height, float sharpen = 1f)
        {
            float[,] coeff = GetCoeff(sharpen);

            // размер входного пикселя в байтах
            int pixelSize = sizeof(ushort);

            int rowSize = width * pixelSize;
            byte[] result = new byte[rowSize * height];
            bytes.AsSpan().CopyTo(result);

            (int indexR, int indexB, int indexG1, int indexG2) = (0, 3, 1, 2);

            int hRidx = indexR % 2;
            int vRidx = indexR / 2;
            int hBidx = indexB % 2;
            int vBidx = indexB / 2;

            int hG1idx = indexG1 % 2;
            int hG2idx = indexG2 % 2;

            int halfHeight = height / 2;
            int halfWidth = width / 2;
            int cntY = halfHeight - 1;
            int cntX = halfWidth - 1;

            Parallel.For(1, cntY, j =>
            //for(int j =1; j < cntY; j++) 
            {
                int startY = j;

                ReadOnlySpan<byte> inputSpan = bytes;
                Span<byte> outputSpan = result;
                int y2 = j * 2;

                Span<byte> outputRowSpan1 = outputSpan.Slice(rowSize * y2, rowSize);
                Span<byte> outputRowSpan2 = outputSpan.Slice(rowSize * (y2 + 1), rowSize);

                Span<ushort> outputRow1 = MemoryMarshal.Cast<byte, ushort>(outputRowSpan1);
                Span<ushort> outputRow2 = MemoryMarshal.Cast<byte, ushort>(outputRowSpan2);

                for (int i = 1; i < cntX; ++i)
                {
                    int startX = i;
                    int x2 = i * 2;

                    float r = 0;
                    float g1 = 0;
                    float g2 = 0;
                    float b = 0;

                    // окошко 3x3 для применения матрицы резкости
                    for (int h = -1; h < 2; h++)
                    {
                        int y = (startY + h) * 2;

                        ReadOnlySpan<byte> inputRowSpan1 = inputSpan.Slice(rowSize * y, rowSize);
                        ReadOnlySpan<byte> inputRowSpan2 = inputSpan.Slice(rowSize * (y + 1), rowSize);

                        ReadOnlySpan<ushort> row1 = MemoryMarshal.Cast<byte, ushort>(inputRowSpan1);
                        ReadOnlySpan<ushort> row2 = MemoryMarshal.Cast<byte, ushort>(inputRowSpan2);

                        for (int w = -1; w < 2; w++)
                        {
                            int x = (startX + w) * 2;

                            r += row1[x + hRidx] * coeff[h + 1, w + 1];
                            g1 += row1[x + hG1idx] * coeff[h + 1, w + 1];
                            g2 += row2[x + hG2idx] * coeff[h + 1, w + 1];
                            b += row2[x + hBidx] * coeff[h + 1, w + 1];
                        }
                    }

                    outputRow1[x2 + hRidx] = Clamp(r);
                    outputRow1[x2 + hG1idx] = Clamp(g1);
                    outputRow2[x2 + hG2idx] = Clamp(g2);
                    outputRow2[x2 + hBidx] = Clamp(b);
                }
            });


            return new ImageBytesResult() { Bytes = result, Width = width, Height = height };
        }


        /// <summary>
        /// Применяет матрицу резкости на 16 битный RAW - увеличивая резкость от центра к краю в интервале [0, 1]
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static ImageBytesResult AdaptiveSharpRaw16(byte[] bytes, int width, int height)
        {
            // на совсем маленьких изображениях ничего не делаем
            if (bytes == null || bytes.Length < 100000)
            {
                return new ImageBytesResult() { Bytes = bytes, Height = height, Width = width };
            }

            int radiusY = height / 2;
            int radiusX = width / 2;

            // полное расстояние от центра до угла
            double fullDistance = (float)Math.Sqrt(radiusX * radiusX + radiusY * radiusY);

            // размер входного пикселя в байтах
            int pixelSize = sizeof(ushort);

            int rowSize = width * pixelSize;
            int dataSize = rowSize * height;

            byte[] result = new byte[bytes.Length];
            bytes.AsSpan().CopyTo(result);

            (int indexR, int indexB, int indexG1, int indexG2) = (0, 3, 1, 2);

            int hRidx = indexR % 2;
            int vRidx = indexR / 2;
            int hBidx = indexB % 2;
            int vBidx = indexB / 2;

            int hG1idx = indexG1 % 2;
            int hG2idx = indexG2 % 2;

            int halfHeight = height / 2;
            int halfWidth = width / 2;

            int cntY = RoundUpDivide(halfHeight, KERNEL_SIZE);
            int cntX = RoundUpDivide(halfWidth, KERNEL_SIZE);

            Parallel.For(0, cntY, j =>
            //for (int j =1; j < cntY; j++) 
            {
                int startY = j * KERNEL_SIZE;

                ReadOnlySpan<byte> inputSpan = bytes;
                Span<byte> outputSpan = result;
                
                for (int i = 0; i < cntX; ++i)
                {
                    int startX = i * KERNEL_SIZE;

                    int dy = startY * 2 - radiusY;
                    int dx = startX * 2 - radiusX;

                    // относительное растояние точки от центра в диапазоне [0, 1]
                    double distancePercent = Math.Sqrt(dy * dy + dx * dx) / fullDistance;
                    double sharpness = _interpolate(distancePercent);
                    float[,] coeff = GetCoeff(sharpness);

                    // обработка окошками размером в KERNEL_SIZE
                    for (int h = 0; h < KERNEL_SIZE; h++)
                    {
                        int y = (startY + h) * 2;
                        if ((y + 2) >= height || y < 2)
                        {
                            break;
                        }

                        Span<byte> outputRowSpan1 = outputSpan.Slice(rowSize * y, rowSize);
                        Span<byte> outputRowSpan2 = outputSpan.Slice(rowSize * (y + 1), rowSize);

                        Span<ushort> outputRow1 = MemoryMarshal.Cast<byte, ushort>(outputRowSpan1);
                        Span<ushort> outputRow2 = MemoryMarshal.Cast<byte, ushort>(outputRowSpan2);

                        for (int w = 0; w < KERNEL_SIZE; w++)
                        {
                            int x = (startX + w) * 2;

                            if ((x + 2) >= width || x < 2)
                            {
                                break;
                            }

                            float r = 0;
                            float g1 = 0;
                            float g2 = 0;
                            float b = 0;

                            // окошко 3x3 для применения матрицы резкости
                            for (int h2 = -1; h2 < 2; h2++)
                            {
                                int y2 = (startY + h + h2) * 2;

                                ReadOnlySpan<byte> inputRowSpan1 = inputSpan.Slice(rowSize * y2, rowSize);
                                ReadOnlySpan<byte> inputRowSpan2 = inputSpan.Slice(rowSize * (y2 + 1), rowSize);

                                ReadOnlySpan<ushort> row1 = MemoryMarshal.Cast<byte, ushort>(inputRowSpan1);
                                ReadOnlySpan<ushort> row2 = MemoryMarshal.Cast<byte, ushort>(inputRowSpan2);

                                for (int w2 = -1; w2 < 2; w2++)
                                {
                                    int x2 = (startX + w + w2) * 2;
                                    int h3 = h2 + 1;
                                    int w3 = w2 + 1;

                                    r += row1[x2 + hRidx] * coeff[h3, w3];
                                    g1 += row1[x2 + hG1idx] * coeff[h3, w3];
                                    g2 += row2[x2 + hG2idx] * coeff[h3, w3];
                                    b += row2[x2 + hBidx] * coeff[h3, w3];
                                }
                            }

                            outputRow1[x + hRidx] = Clamp(r);
                            outputRow1[x + hG1idx] = Clamp(g1);
                            outputRow2[x + hG2idx] = Clamp(g2);
                            outputRow2[x + hBidx] = Clamp(b);
                        }

                    }
                }
            });


            return new ImageBytesResult() { Bytes = result, Width = width, Height = height };
        }
    }
}
