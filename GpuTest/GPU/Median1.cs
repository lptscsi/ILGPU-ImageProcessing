using GpuTest.GPU.Core;
using GpuTest.Utils.Color;
using ILGPU;
using ILGPU.Runtime;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static GpuTest.Utils.Median;

namespace GpuTest.GPU
{
    /// <summary>
    /// Median 3x3
    /// </summary>
    public static class Median1
    {
        private static Action<KernelConfig, Config> _kernel;

        public struct Config
        {
            public readonly int width;
            public readonly int height;
 
            public readonly ArrayView2D<RGBA32, Stride2D.DenseX> input;
            public readonly ArrayView2D<RGBA32, Stride2D.DenseX> output;

            public Config(int width, int height, ArrayView2D<RGBA32, Stride2D.DenseX> input, ArrayView2D<RGBA32, Stride2D.DenseX> output)
            {
                this.width = width;
                this.height = height;
                this.input = input;
                this.output = output;
            }
        }


        #region Helper methods

        static int divUp(int a, int b) => (a + b - 1) / b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int idxClip(int idx, int idxMax) => (idx > (idxMax - 1) ? (idxMax - 1) : (idx < 0 ? 0 : idx));

        #endregion

        public static void WarmUp(Accelerator device)
        {
            _ = LoadKernel(device);
        }

        static Action<KernelConfig, Config> LoadKernel(Accelerator device)
        {
            return LazyInitializer.EnsureInitialized(ref _kernel, () =>
            {
                return device.LoadStreamKernel<Config>(Kernel);
            });
        }


        /// <summary>
        /// Executes Median filter
        /// </summary>
        /// <param name="device"></param>
        /// <param name="inputImage"></param>
        public static async Task<GPUImage<RGBA32>> Execute(Accelerator device, GPUImage<RGBA32> inputImage)
        {
            int resultWidth = (int)inputImage.Extent2D.X;
            int resultHeight = (int)inputImage.Extent2D.Y;
            GPUImage<RGBA32> outputImage = new GPUImage<RGBA32>(resultWidth, resultHeight);

            int maxThreadsPerGroup = device.MaxNumThreadsPerGroup;
            int groupXDim = 16;
            int groupYDim = maxThreadsPerGroup / groupXDim;
            int gridXDim = divUp(inputImage.Extent2D.X, groupXDim);
            int gridYDim = divUp(inputImage.Extent2D.Y, groupYDim);
            KernelConfig kernelConfig = new KernelConfig(new Index2D(gridXDim, gridYDim), new Index2D(groupXDim, groupYDim));

            ArrayView2D<RGBA32, Stride2D.DenseX> input2DView = await inputImage.ToDevice2D(device.DefaultStream);
            ArrayView2D<RGBA32, Stride2D.DenseX> output2DView = await outputImage.ToDevice2D(device.DefaultStream);

            var kernel = LoadKernel(device);
            var conf = new Config(inputImage.Extent2D.X, inputImage.Extent2D.Y, input2DView, output2DView);
            kernel(kernelConfig, conf);
            device.Synchronize();

            return outputImage;
        }

        static void Kernel(Config conf)
        {
            var idx = Grid.IdxX * Group.DimX + Group.IdxX;
            var idy = Grid.IdxY * Group.DimY + Group.IdxY;

            if (idy >= conf.height || idx >= conf.width)
            {
                return;
            }


            Index2D index = new Index2D(idx, idy);
            const int RAD = 1;
            const int kernelSize = 2 * RAD + 1;
            

            ArrayView<float> red = LocalMemory.Allocate<float>(9);
            ArrayView<float> green = LocalMemory.Allocate<float>(9);
            ArrayView<float> blue = LocalMemory.Allocate<float>(9);

            for (int y = -RAD; y <= RAD; y++)
            {
                for (int x = -RAD; x <= RAD; x++)
                {
                    int idy_y = idxClip(idy + y, conf.height);
                    int idx_x = idxClip(idx + x, conf.width);
                    int y1 = y + RAD;
                    int x1 = x + RAD;
                    int num = y1 * kernelSize + x1;

                    Vec3 color_xy = conf.input[idx_x, idy_y];


                    // unrolling indexes for better performance!!!
                    if (num == 0)
                    {
                        red[0] = color_xy.X;
                        green[0] = color_xy.Y;
                        blue[0] = color_xy.Z;
                    }

                    if (num == 1)
                    {
                        red[1] = color_xy.X;
                        green[1] = color_xy.Y;
                        blue[1] = color_xy.Z;
                    }

                    if (num == 2)
                    {
                        red[2] = color_xy.X;
                        green[2] = color_xy.Y;
                        blue[2] = color_xy.Z;
                    }

                    if (num == 3)
                    {
                        red[3] = color_xy.X;
                        green[3] = color_xy.Y;
                        blue[3] = color_xy.Z;
                    }

                    if (num == 4)
                    {
                        red[4] = color_xy.X;
                        green[4] = color_xy.Y;
                        blue[4] = color_xy.Z;
                    }

                    if (num == 5)
                    {
                        red[5] = color_xy.X;
                        green[5] = color_xy.Y;
                        blue[5] = color_xy.Z;
                    }

                    if (num == 6)
                    {
                        red[6] = color_xy.X;
                        green[6] = color_xy.Y;
                        blue[6] = color_xy.Z;
                    }

                    if (num == 7)
                    {
                        red[7] = color_xy.X;
                        green[7] = color_xy.Y;
                        blue[7] = color_xy.Z;
                    }

                    if (num == 8)
                    {
                        red[8] = color_xy.X;
                        green[8] = color_xy.Y;
                        blue[8] = color_xy.Z;
                    }
                }
            }

            float r = median9(red);
            float g = median9(green);
            float b = median9(blue);

            conf.output[index] = new RGBA32(r, g, b);
        }
    }
}
