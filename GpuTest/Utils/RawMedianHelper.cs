using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static GpuTest.CropHelper;

namespace CrossImage.Common.Imaging
{
    /// <summary>
    /// Класс для методов уменьшения размера недебаеризованного изображения
    /// </summary>
    public static class RawMedianHelper
    {
        #region Кострукторы

        /// <summary>
        /// Статический конструктор
        /// </summary>
        static RawMedianHelper()
        {

        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort Clamp(float value)
        {
            float result = value < 0 ? 0 : value;
            
            return (ushort)(result > (float)ushort.MaxValue ?ushort.MaxValue : result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SwapElements(ref float p, ref float q)
        {
            float temp = p;
            p = q;
            q = temp;
        }

        public static float Median(Span<float> arr, int n)
        {
            int middle, ll, hh;

            int low = 0; int high = n - 1; int median = (low + high) / 2;
            for (; ; )
            {
                if (high <= low)
                    return arr[median];

                if (high == low + 1)
                {
                    if (arr[low] > arr[high])
                        SwapElements(ref arr[low], ref arr[high]);
                    return arr[median];
                }

                middle = (low + high) / 2;

                if (arr[middle] > arr[high])
                    SwapElements(ref arr[middle], ref arr[high]);

                if (arr[low] > arr[high])
                    SwapElements(ref arr[low], ref arr[high]);

                if (arr[middle] > arr[low])
                    SwapElements(ref arr[middle], ref arr[low]);

                SwapElements(ref arr[middle], ref arr[low + 1]);

                ll = low + 1;
                hh = high;
                for (; ; )
                {
                    do ll++; while (ll < high && arr[low] > arr[ll]);
                    do hh--; while (hh > -1 && arr[hh] > arr[low]);

                    if (hh < ll)
                        break;

                    SwapElements(ref arr[ll], ref arr[hh]);
                }

                SwapElements(ref arr[low], ref arr[hh]);

                if (hh <= median)
                    low = ll;
                if (hh >= median)
                    high = hh - 1;
            }
        }

        public static ImageBytesResult MedianRaw16(byte[] bytes, int width, int height)
        {
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
            int cntY = halfHeight -1;
            int cntX = halfWidth - 1;

            Parallel.For(1, cntY, j =>
            {
                Span<float> medianBufferI = stackalloc float[9];
                Span<float> medianBufferQ = stackalloc float[9];

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

                            ref readonly ushort r = ref row1[x + hRidx];
                            ref readonly ushort g1 = ref row1[x + hG1idx];
                            ref readonly ushort g2 = ref row2[x + hG2idx];
                            ref readonly ushort b = ref row2[x + hBidx];

                            medianBufferI[3 * (h + 1) + (w + 1)] = ((g1 + g2) / 2);
                        }
                    }


                    ref ushort red = ref outputRow1[x2 + hRidx];
                    ref ushort green1 = ref outputRow1[x2 + hG1idx];
                    ref ushort green2 = ref outputRow2[x2 + hG2idx];
                    ref ushort blue = ref outputRow2[x2 + hBidx];

             
                    float g = Median(medianBufferI, 9);
                  
                    green1 = (ushort)g;
                    green2 = (ushort)g;
                }
            });


            return new ImageBytesResult() { Bytes = result, Width = width, Height = height };
        }
    }
}
