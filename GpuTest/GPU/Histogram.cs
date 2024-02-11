using GpuTest.GPU.Core;
using GpuTest.Utils.Color;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Algorithms.HistogramOperations;
using ILGPU.Runtime;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace GpuTest.GPU
{
    public class Histogram
    {
        struct CustomBinOperation : IComputeMultiBinOperation<RGBA32, int, HistogramIncrementInt32>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static byte Get8BitBT709Luminance(byte r, byte g, byte b)
                => (byte)((r * .2126F) + (g * .7152F) + (b * .0722F) + 0.5F);


            public void ComputeHistogramBins(
                RGBA32 value,
                ArrayView<int> histogram,
                in HistogramIncrementInt32 incrementOperation,
                out bool didOverflow)
            {
                didOverflow = false;
                int luminance = Get8BitBT709Luminance(value.R, value.G, value.B);

                incrementOperation.Increment(
                          ref histogram[luminance],
                          out var tempDidOverflow);
                if (tempDidOverflow)
                    didOverflow = true;
            }
        }

        /// <summary>
        /// Warm UP
        /// </summary>
        public static void WarmUp(Accelerator device)
        {
            using var inputView = device.Allocate1D<RGBA32>(256);
            using var histogram = device.Allocate1D<int>(256);
            histogram.MemSetToZero();

            device.HistogramUnchecked<
                RGBA32,
                Stride1D.Dense,
                int,
                HistogramIncrementInt32,
                CustomBinOperation>(
                device.DefaultStream,
                inputView.View,
                histogram.View);

            int[] result = histogram.GetAsArray1D();
        }

        /// <summary>
        /// Executes histogram
        /// </summary>
        public static async Task<int[]> Execute(Accelerator device, GPUImage<RGBA32> inputImage)
        {
            ArrayView1D<RGBA32, Stride1D.Dense> inputView = await inputImage.ToDevice1D(device.DefaultStream);

            using var histogram = device.Allocate1D<int>(256);
            histogram.MemSetToZero();

            device.HistogramUnchecked<
                RGBA32,
                Stride1D.Dense,
                int,
                HistogramIncrementInt32,
                CustomBinOperation>(
                device.DefaultStream,
                inputView,
                histogram.View);

            int[] result = histogram.GetAsArray1D();

            return result;
        }
    }
}
