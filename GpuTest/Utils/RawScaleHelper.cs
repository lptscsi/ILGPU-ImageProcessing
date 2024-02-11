using GpuTest;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static GpuTest.CropHelper;

namespace CrossImage.Common.Imaging
{
    /// <summary>
    /// Класс для методов уменьшения размера недебаеризованного изображения
    /// </summary>
    public static class RawScaleHelper
    {
        #region Поля

        static readonly int[,] rCoeff2 = new int[2, 2] {
                {8,3},
                {3,2}
            };

        static readonly int[,] g1Coeff2 = new int[2, 2] {
                {3,8},
                {2,3}
            };

        static readonly int[,] g2Coeff2 = new int[2, 2] {
                {3,2},
                {8,3}
            };

        static readonly int[,] bCoeff2 = new int[2, 2] {
                {2,3},
                {3,8}
        };

        static readonly int Coeff2Sum;

        static readonly int[,] rCoeff4 = new int[4, 4] {
                {9,9,3,3},
                {9,9,3,3},
                {3,3,1,1},
                {3,3,1,1},
            };

        static readonly int[,] g1Coeff4 = new int[4, 4] {
                {3,3,9,9},
                {3,3,9,9},
                {1,1,3,3},
                {1,1,3,3},
            };

        static readonly int[,] g2Coeff4 = new int[4, 4] {
                {3,3,1,1},
                {3,3,1,1},
                {9,9,3,3},
                {9,9,3,3},
            };

        static readonly int[,] bCoeff4 = new int[4, 4] {
                {1,1,3,3},
                {1,1,3,3},
                {3,3,9,9},
                {3,3,9,9},
            };

        static readonly int Coeff4Sum;
        
        #endregion

        #region Кострукторы

        /// <summary>
        /// Статический конструктор
        /// </summary>
        static RawScaleHelper()
        {
            Coeff2Sum = 0;
            Coeff4Sum = 0;

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    Coeff2Sum += bCoeff2[i, j];
                }
            }

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    Coeff4Sum += bCoeff4[i, j];
                }
            }
        }

        #endregion

        /// <summary>
        /// Уменьшает размер RGGB недебаеризованного изображения в 8 раз
        /// </summary>
        /// <returns>balance gains</returns>
        public static ImageBytesResult DownScaleRggbBy8(byte[] bytes, int width, int height, ColorDepths colorDepth)
        {
            // Уменьшаем в 4 раза , и затем в 2 раза
            ImageBytesResult res4 = colorDepth switch
            {
                ColorDepths.Bits8 => DownScaleRg8(bytes, width, height, 4),
                ColorDepths.Bits16 => DownScaleRg16(bytes, width, height, 4),
                _ => throw new ArgumentOutOfRangeException(nameof(colorDepth)),
            };

            return colorDepth switch
            {
                ColorDepths.Bits8 => DownScaleRg8(res4.Bytes, res4.Width, res4.Height, 2),
                ColorDepths.Bits16 => DownScaleRg16(res4.Bytes, res4.Width, res4.Height, 2),
                _ => throw new ArgumentOutOfRangeException(nameof(colorDepth)),
            };
        }


        /// <summary>
        /// Уменьшает размер 8 битного RGGB недебаеризованного изображения в N раз
        /// </summary>
        /// <returns>balance gains</returns>
        public static ImageBytesResult DownScaleRg8(byte[] bytes, int width, int height, int factor)
        {
            int[,] rcoeff = new int[0,0];
            int[,] g1coeff = new int[0,0];
            int[,] g2coeff = new int[0,0];
            int[,] bcoeff = new int[0,0];
            int devide = 1;

            if (factor == 4)
            {
                rcoeff = rCoeff4;
                g1coeff = g1Coeff4;
                g2coeff = g2Coeff4;
                bcoeff = bCoeff4;
                devide = Coeff4Sum;
            }
            else if (factor == 2)
            {
                rcoeff = rCoeff2;
                g1coeff = g1Coeff2;
                g2coeff = g2Coeff2;
                bcoeff = bCoeff2;
                devide = Coeff2Sum;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(factor));
            }

            int kernelSize = factor;
            // размер входного пикселя в байтах
            int pixelSize = 1;

            int width2 = width / kernelSize;
            int height2 = height / kernelSize;
            int rowSize = width * pixelSize;
            int imageSize = rowSize * height;
            int rowSize2 = width2 * pixelSize;
            byte[] result = new byte[rowSize2 * height2];

            (int indexR, int indexB, int indexG1, int indexG2) = (0, 3, 1, 2);

            int hRidx = indexR % 2;
            int vRidx = indexR / 2;
            int hBidx = indexB % 2;
            int vBidx = indexB / 2;

            int hG1idx = indexG1 % 2;
            int hG2idx = indexG2 % 2;

            int halfHeight = height / 2;
            int halfWidth = width / 2;
            int cntY = halfHeight / kernelSize;
            int cntX = halfWidth / kernelSize;

            Parallel.For(0, cntY, j =>
            {
                int startY = j * kernelSize;

                ReadOnlySpan<byte> inputSpan = bytes;
                int y2 = j * 2;
                int yd1 = y2 * rowSize2;
                int yd2 = (y2 + 1) * rowSize2;

                for (int i = 0; i < cntX; ++i)
                {
                    int startX = i * kernelSize;

                    int r = 0;
                    int g1 = 0;
                    int g2 = 0;
                    int b = 0;

                    // обработка окошками размером в winSize
                    for (int h = 0; h < kernelSize; h++)
                    {
                        int y = (startY + h) * 2;

                        ReadOnlySpan<byte> row1 = inputSpan.Slice(rowSize * y, rowSize);
                        ReadOnlySpan<byte> row2 = inputSpan.Slice(rowSize * (y + 1), rowSize);

                        for (int w = 0; w < kernelSize; w++)
                        {
                            int x = (startX + w) * 2;

                            r += row1[x + hRidx] * rcoeff[h, w];
                            g1 += row1[x + hG1idx] * g1coeff[h, w];
                            g2 += row2[x + hG2idx] * g2coeff[h, w];
                            b += row2[x + hBidx] * bcoeff[h, w];
                        }
                    }

                    int x2 = i * 2;
                    int yx1 = yd1 + x2;
                    int yx2 = yd2 + x2;

                    // red 
                    result[yx1] = (byte)(r / devide);
                    // green1
                    result[yx1 + 1] = (byte)(g1 / devide);

                    // green2
                    result[yx2] = (byte)(g2 / devide);
                    // blue
                    result[yx2 + 1] = (byte)(b / devide);
                }
            });


            return new ImageBytesResult() { Bytes = result, Width = width2, Height = height2 };
        }

        /// <summary>
        /// Уменьшает размер 16 битного RGGB недебаеризованного изображения в N раз
        /// </summary>
        /// <returns>balance gains</returns>
        public static ImageBytesResult DownScaleRg16(byte[] bytes, int width, int height, int factor)
        {
            int[,] rcoeff = new int[0, 0];
            int[,] g1coeff = new int[0, 0];
            int[,] g2coeff = new int[0, 0];
            int[,] bcoeff = new int[0, 0];
            int devide = 1;

            if (factor == 4)
            {
                rcoeff = rCoeff4;
                g1coeff = g1Coeff4;
                g2coeff = g2Coeff4;
                bcoeff = bCoeff4;
                devide = Coeff4Sum;
            }
            else if (factor == 2)
            {
                rcoeff = rCoeff2;
                g1coeff = g1Coeff2;
                g2coeff = g2Coeff2;
                bcoeff = bCoeff2;
                devide = Coeff2Sum;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(factor));
            }

            int kernelSize = factor;
            // размер входного пикселя в байтах
            int pixelSize = 2;

            int width2 = width / kernelSize;
            int height2 = height / kernelSize;
            int rowSize = width * pixelSize;
            int imageSize = rowSize * height;
            int rowSize2 = width2 * pixelSize;
            byte[] result = new byte[rowSize2 * height2];

            (int indexR, int indexB, int indexG1, int indexG2) = (0, 3, 1, 2);

            int hRidx = indexR % 2;
            int vRidx = indexR / 2;
            int hBidx = indexB % 2;
            int vBidx = indexB / 2;

            int hG1idx = indexG1 % 2;
            int hG2idx = indexG2 % 2;

            int halfHeight = height / 2;
            int halfWidth = width / 2;
            int cntY = halfHeight / kernelSize;
            int cntX = halfWidth / kernelSize;

            Parallel.For(0, cntY, j =>
            {
                int startY = j * kernelSize;

                ReadOnlySpan<byte> inputSpan = bytes;
                Span<byte> outputSpan = result;
                int y2 = j * 2;

                Span<byte> outputRowSpan1 = outputSpan.Slice(rowSize2 * y2, rowSize2);
                Span<byte> outputRowSpan2 = outputSpan.Slice(rowSize2 * (y2 + 1), rowSize2);

                Span<ushort> outputRow1 = MemoryMarshal.Cast<byte, ushort>(outputRowSpan1);
                Span<ushort> outputRow2 = MemoryMarshal.Cast<byte, ushort>(outputRowSpan2);

                for (int i = 0; i < cntX; ++i)
                {
                    int startX = i * kernelSize;

                    int r = 0;
                    int g1 = 0;
                    int g2 = 0;
                    int b = 0;

                    // обработка окошками размером в winSize
                    for (int h = 0; h < kernelSize; h++)
                    {
                        int y = (startY + h) * 2;

                        ReadOnlySpan<byte> inputRowSpan1 = inputSpan.Slice(rowSize * y, rowSize);
                        ReadOnlySpan<byte> inputRowSpan2 = inputSpan.Slice(rowSize * (y + 1), rowSize);

                        ReadOnlySpan<ushort> row1 = MemoryMarshal.Cast<byte, ushort>(inputRowSpan1);
                        ReadOnlySpan<ushort> row2 = MemoryMarshal.Cast<byte, ushort>(inputRowSpan2);

                        for (int w = 0; w < kernelSize; w++)
                        {
                            int x = (startX + w) * 2;

                            r += row1[x + hRidx] * rcoeff[h, w];
                            g1 += row1[x + hG1idx] * g1coeff[h, w];
                            g2 += row2[x + hG2idx] * g2coeff[h, w];
                            b += row2[x + hBidx] * bcoeff[h, w];
                        }
                    }

                    int x2 = i * 2;

                    // red 
                    outputRow1[x2] = (ushort)(r / devide);
                    // green1
                    outputRow1[x2 + 1] = (ushort)(g1 / devide);
                    // green2
                    outputRow2[x2] = (ushort)(g2 / devide);
                    // blue
                    outputRow2[x2 + 1] = (ushort)(b / devide);
                }
            });


            return new ImageBytesResult() { Bytes = result, Width = width2, Height = height2 };
        }
    }
}
