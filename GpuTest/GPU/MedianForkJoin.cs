using GpuTest.GPU.Core;
using GpuTest.Utils;
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
    /// Median 7x7 (splits input into individiual channels)
    /// </summary>
    public static class MedianForkJoin
    {
        private static Action<AcceleratorStream, KernelConfig, ForkConfig> _forkKernel;
        private static Action<AcceleratorStream, KernelConfig, JoinConfig> _joinKernel;
        private static Action<AcceleratorStream, KernelConfig, Config> _kernel1;
        private static Action<AcceleratorStream, KernelConfig, Config> _kernel2;
        private static Action<AcceleratorStream, KernelConfig, Config> _kernel3;

        #region Config

        public struct ForkConfig
        {
            public readonly int width;
            public readonly int height;

            public readonly ArrayView2D<RGBA32, Stride2D.DenseX> input;
            public readonly ArrayView2D<float, Stride2D.DenseX> outputR;
            public readonly ArrayView2D<float, Stride2D.DenseX> outputG;
            public readonly ArrayView2D<float, Stride2D.DenseX> outputB;

            public ForkConfig(int width, int height, ArrayView2D<RGBA32, Stride2D.DenseX> input, 
                ArrayView2D<float, Stride2D.DenseX> outputR,
                ArrayView2D<float, Stride2D.DenseX> outputG,
                ArrayView2D<float, Stride2D.DenseX> outputB)
            {
                this.width = width;
                this.height = height;
                this.input = input;
                this.outputR = outputR;
                this.outputG = outputG;
                this.outputB = outputB;
            }
        }

        public struct Config
        {
            public readonly int width;
            public readonly int height;

            public readonly ArrayView2D<float, Stride2D.DenseX> input;
            public readonly ArrayView2D<float, Stride2D.DenseX> output;

            public Config(int width, int height, ArrayView2D<float, Stride2D.DenseX> input, ArrayView2D<float, Stride2D.DenseX> output)
            {
                this.width = width;
                this.height = height;
                this.input = input;
                this.output = output;
            }
        }

        public struct JoinConfig
        {
            public readonly int width;
            public readonly int height;

            public readonly ArrayView2D<float, Stride2D.DenseX> inputR;
            public readonly ArrayView2D<float, Stride2D.DenseX> inputG;
            public readonly ArrayView2D<float, Stride2D.DenseX> inputB;
            public readonly ArrayView2D<RGBA32, Stride2D.DenseX> output;

            public JoinConfig(int width, int height, 
                ArrayView2D<float, Stride2D.DenseX> inputR,
                ArrayView2D<float, Stride2D.DenseX> inputG,
                ArrayView2D<float, Stride2D.DenseX> inputB,
                ArrayView2D<RGBA32, Stride2D.DenseX> output)
            {
                this.width = width;
                this.height = height;
                this.inputR = inputR;
                this.inputG = inputG;
                this.inputB = inputB;
                this.output = output;
            }
        }

        #endregion

        #region Helper methods

        static int divUp(int a, int b) => (a + b - 1) / b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int idxClip(int idx, int idxMax) => (idx > (idxMax - 1) ? (idxMax - 1) : (idx < 0 ? 0 : idx));

        #endregion

        /// <summary>
        /// Preloads (compiles) all kernels
        /// </summary>
        /// <param name="device"></param>
        public static void WarmUp(Accelerator device)
        {
            _ = LoadForkKernel(device);
            _ = LoadJoinKernel(device);
            _ = LoadKernel1(device);
            _ = LoadKernel2(device);
            _ = LoadKernel3(device);
        }

        /// <summary>
        /// Median 3x3
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        static Action<AcceleratorStream, KernelConfig, Config> LoadKernel1(Accelerator device)
        {
            return LazyInitializer.EnsureInitialized(ref _kernel1, () =>
            {
                return device.LoadKernel<Config>(Kernel1);
            });
        }

        /// <summary>
        /// Median 5x5
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        static Action<AcceleratorStream, KernelConfig, Config> LoadKernel2(Accelerator device)
        {
            return LazyInitializer.EnsureInitialized(ref _kernel2, () =>
            {
                return device.LoadKernel<Config>(Kernel2);
            });
        }

        /// <summary>
        /// Median 7x7
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        static Action<AcceleratorStream, KernelConfig, Config> LoadKernel3(Accelerator device)
        {
            return LazyInitializer.EnsureInitialized(ref _kernel3, () =>
            {
                return device.LoadKernel<Config>(Kernel3);
            });
        }

        static Action<AcceleratorStream, KernelConfig, ForkConfig> LoadForkKernel(Accelerator device)
        {
            return LazyInitializer.EnsureInitialized(ref _forkKernel, () =>
            {
                return device.LoadKernel<ForkConfig>(ForkKernel);
            });
        }

        static Action<AcceleratorStream, KernelConfig, JoinConfig> LoadJoinKernel(Accelerator device)
        {
            return LazyInitializer.EnsureInitialized(ref _joinKernel, () =>
            {
                return device.LoadKernel<JoinConfig>(JoinKernel);
            });
        }

        /// <summary>
        /// Executes Median filter
        /// </summary>
        /// <param name="device"></param>
        /// <param name="inputImage"></param>
        /// <param name="radius"></param>
        public static async Task<GPUImage<RGBA32>> Execute(Accelerator device, GPUImage<RGBA32> inputImage, int radius = 1)
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
            using AcceleratorStream stream = device.CreateStream();
       
            ArrayView2D<RGBA32, Stride2D.DenseX> input2DView = await inputImage.ToDevice2D(stream);

         
            using MemoryBuffer2D<float, Stride2D.DenseX> bufferR = device.Allocate2DDenseX<float>(new LongIndex2D(inputImage.Extent2D.X, inputImage.Extent2D.Y));
            using MemoryBuffer2D<float, Stride2D.DenseX> bufferG = device.Allocate2DDenseX<float>(new LongIndex2D(inputImage.Extent2D.X, inputImage.Extent2D.Y));
            using MemoryBuffer2D<float, Stride2D.DenseX> bufferB = device.Allocate2DDenseX<float>(new LongIndex2D(inputImage.Extent2D.X, inputImage.Extent2D.Y));
            using MemoryBuffer2D<float, Stride2D.DenseX> buffer2R = device.Allocate2DDenseX<float>(new LongIndex2D(inputImage.Extent2D.X, inputImage.Extent2D.Y));
            using MemoryBuffer2D<float, Stride2D.DenseX> buffer2G = device.Allocate2DDenseX<float>(new LongIndex2D(inputImage.Extent2D.X, inputImage.Extent2D.Y));
            using MemoryBuffer2D<float, Stride2D.DenseX> buffer2B = device.Allocate2DDenseX<float>(new LongIndex2D(inputImage.Extent2D.X, inputImage.Extent2D.Y));

            ArrayView2D<RGBA32, Stride2D.DenseX> output2DView = await outputImage.ToDevice2D(stream);

            var forkKernel = LoadForkKernel(device);
            var joinKernel = LoadJoinKernel(device);

            Action<AcceleratorStream, KernelConfig, Config> kernel;
            switch (radius)
            {
                case 1:
                    kernel = LoadKernel1(device);
                    break;
                case 2:
                    kernel = LoadKernel2(device);
                    break;
                case 3:
                    kernel = LoadKernel3(device);
                    break;
                default:
                    throw new NotImplementedException();
            }
            

            var forkConf = new ForkConfig(inputImage.Extent2D.X, inputImage.Extent2D.Y, input2DView, bufferR.View, bufferG.View, bufferB.View);

            var confR = new Config(inputImage.Extent2D.X, inputImage.Extent2D.Y, bufferR.View, buffer2R.View);
            var confG = new Config(inputImage.Extent2D.X, inputImage.Extent2D.Y, bufferG.View, buffer2G.View);
            var confB = new Config(inputImage.Extent2D.X, inputImage.Extent2D.Y, bufferB.View, buffer2B.View);

            var joinConf = new JoinConfig(inputImage.Extent2D.X, inputImage.Extent2D.Y, buffer2R.View, buffer2G.View, buffer2B.View, output2DView);
         
            forkKernel(stream, kernelConfig, forkConf);
            device.Synchronize();
            kernel(stream, kernelConfig, confR);
            kernel(stream, kernelConfig, confG);
            kernel(stream, kernelConfig, confB);
            device.Synchronize();
            joinKernel(stream, kernelConfig, joinConf);
            device.Synchronize();

            return outputImage;
        }

        static void ForkKernel(ForkConfig conf)
        {
            var idx = Grid.IdxX * Group.DimX + Group.IdxX;
            var idy = Grid.IdxY * Group.DimY + Group.IdxY;

            if (idy >= conf.height || idx >= conf.width)
            {
                return;
            }

            Index2D index = new Index2D(idx, idy);

            Vec3 color_xy = conf.input[index];
            conf.outputR[index] = color_xy.X;
            conf.outputG[index] = color_xy.Y;
            conf.outputB[index] = color_xy.Z;
        }

        static void JoinKernel(JoinConfig conf)
        {
            var idx = Grid.IdxX * Group.DimX + Group.IdxX;
            var idy = Grid.IdxY * Group.DimY + Group.IdxY;

            if (idy >= conf.height || idx >= conf.width)
            {
                return;
            }

            Index2D index = new Index2D(idx, idy);

            float colorR = conf.inputR[index];
            float colorG = conf.inputG[index];
            float colorB = conf.inputB[index];

            conf.output[index] = new RGBA32(colorR, colorG, colorB);
        }

        static void Kernel1(Config conf)
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

            ArrayView<float> arr = LocalMemory.Allocate<float>(9);

            for (int y = -RAD; y <= RAD; y++)
            {
                for (int x = -RAD; x <= RAD; x++)
                {

                    int idy_y = idxClip(idy + y, conf.height);
                    int idx_x = idxClip(idx + x, conf.width);
                    int y1 = y + RAD;
                    int x1 = x + RAD;
                    int num = y1 * kernelSize + x1;

                    float value_xy = conf.input[idx_x, idy_y];

                    // unrolling indexes for better performance!!!
                    if (num == 0)
                    {
                        arr[0] = value_xy;
                    }

                    if (num == 1)
                    {
                        arr[1] = value_xy;
                    }

                    if (num == 2)
                    {
                        arr[2] = value_xy;
                    }

                    if (num == 3)
                    {
                        arr[3] = value_xy;
                    }

                    if (num == 4)
                    {
                        arr[4] = value_xy;
                    }

                    if (num == 5)
                    {
                        arr[5] = value_xy;
                    }

                    if (num == 6)
                    {
                        arr[6] = value_xy;
                    }

                    if (num == 7)
                    {
                        arr[7] = value_xy;
                    }

                    if (num == 8)
                    {
                        arr[8] = value_xy;
                    }
                }
            }

            conf.output[index] = median9(arr);
        }

        static void Kernel2(Config conf)
        {
            var idx = Grid.IdxX * Group.DimX + Group.IdxX;
            var idy = Grid.IdxY * Group.DimY + Group.IdxY;

            if (idy >= conf.height || idx >= conf.width)
            {
                return;
            }


            Index2D index = new Index2D(idx, idy);
            const int RAD = 2;
            const int kernelSize = 2 * RAD + 1;

            ArrayView<float> arr = LocalMemory.Allocate<float>(25);

            for (int y = -RAD; y <= RAD; y++)
            {
                for (int x = -RAD; x <= RAD; x++)
                {

                    int idy_y = idxClip(idy + y, conf.height);
                    int idx_x = idxClip(idx + x, conf.width);
                    int y1 = y + RAD;
                    int x1 = x + RAD;
                    int num = y1 * kernelSize + x1;

                    float value_xy = conf.input[idx_x, idy_y];

                    // unrolling indexes for better performance!!!
                    if (num == 0)
                    {
                        arr[0] = value_xy;
                    }

                    if (num == 1)
                    {
                        arr[1] = value_xy;
                    }

                    if (num == 2)
                    {
                        arr[2] = value_xy;
                    }

                    if (num == 3)
                    {
                        arr[3] = value_xy;
                    }

                    if (num == 4)
                    {
                        arr[4] = value_xy;
                    }

                    if (num == 5)
                    {
                        arr[5] = value_xy;
                    }

                    if (num == 6)
                    {
                        arr[6] = value_xy;
                    }

                    if (num == 7)
                    {
                        arr[7] = value_xy;
                    }

                    if (num == 8)
                    {
                        arr[8] = value_xy;
                    }

                    if (num == 9)
                    {
                        arr[9] = value_xy;
                    }

                    if (num == 10)
                    {
                        arr[10] = value_xy;
                    }

                    if (num == 11)
                    {
                        arr[11] = value_xy;
                    }

                    if (num == 12)
                    {
                        arr[12] = value_xy;
                    }

                    if (num == 13)
                    {
                        arr[13] = value_xy;
                    }

                    if (num == 14)
                    {
                        arr[14] = value_xy;
                    }

                    if (num == 15)
                    {
                        arr[15] = value_xy;
                    }

                    if (num == 16)
                    {
                        arr[16] = value_xy;
                    }

                    if (num == 17)
                    {
                        arr[17] = value_xy;
                    }

                    if (num == 18)
                    {
                        arr[18] = value_xy;
                    }

                    if (num == 19)
                    {
                        arr[19] = value_xy;
                    }

                    if (num == 20)
                    {
                        arr[20] = value_xy;
                    }

                    if (num == 21)
                    {
                        arr[21] = value_xy;
                    }

                    if (num == 22)
                    {
                        arr[22] = value_xy;
                    }

                    if (num == 23)
                    {
                        arr[23] = value_xy;
                    }

                    if (num == 24)
                    {
                        arr[24] = value_xy;
                    }
                }
            }

            conf.output[index] = Median25.median(arr);
        }

        static void Kernel3(Config conf)
        {
            var idx = Grid.IdxX * Group.DimX + Group.IdxX;
            var idy = Grid.IdxY * Group.DimY + Group.IdxY;

            if (idy >= conf.height || idx >= conf.width)
            {
                return;
            }


            Index2D index = new Index2D(idx, idy);
            const int RAD = 3;
            const int kernelSize = 2 * RAD + 1;

            ArrayView<float> arr = LocalMemory.Allocate<float>(49);

            for (int y = -RAD; y <= RAD; y++)
            {
                for (int x = -RAD; x <= RAD; x++)
                {

                    int idy_y = idxClip(idy + y, conf.height);
                    int idx_x = idxClip(idx + x, conf.width);
                    int y1 = y + RAD;
                    int x1 = x + RAD;
                    int num = y1 * kernelSize + x1;

                    float value_xy = conf.input[idx_x, idy_y];
           
                    // unrolling indexes for better performance!!!
                    if (num == 0)
                    {
                        arr[0] = value_xy;
                    }

                    if (num == 1)
                    {
                        arr[1] = value_xy;
                    }

                    if (num == 2)
                    {
                        arr[2] = value_xy;
                    }

                    if (num == 3)
                    {
                        arr[3] = value_xy;
                    }

                    if (num == 4)
                    {
                        arr[4] = value_xy;
                    }

                    if (num == 5)
                    {
                        arr[5] = value_xy;
                    }

                    if (num == 6)
                    {
                        arr[6] = value_xy;
                    }

                    if (num == 7)
                    {
                        arr[7] = value_xy;
                    }

                    if (num == 8)
                    {
                        arr[8] = value_xy;
                    }

                    if (num == 9)
                    {
                        arr[9] = value_xy;
                    }

                    if (num == 10)
                    {
                        arr[10] = value_xy;
                    }

                    if (num == 11)
                    {
                        arr[11] = value_xy;
                    }

                    if (num == 12)
                    {
                        arr[12] = value_xy;
                    }

                    if (num == 13)
                    {
                        arr[13] = value_xy;
                    }

                    if (num == 14)
                    {
                        arr[14] = value_xy;
                    }

                    if (num == 15)
                    {
                        arr[15] = value_xy;
                    }

                    if (num == 16)
                    {
                        arr[16] = value_xy;
                    }

                    if (num == 17)
                    {
                        arr[17] = value_xy;
                    }

                    if (num == 18)
                    {
                        arr[18] = value_xy;
                    }

                    if (num == 19)
                    {
                        arr[19] = value_xy;
                    }

                    if (num == 20)
                    {
                        arr[20] = value_xy;
                    }

                    if (num == 21)
                    {
                        arr[21] = value_xy;
                    }

                    if (num == 22)
                    {
                        arr[22] = value_xy;
                    }

                    if (num == 23)
                    {
                        arr[23] = value_xy;
                    }

                    if (num == 24)
                    {
                        arr[24] = value_xy;
                    }
                    
                    if (num == 25)
                    {
                        arr[25] = value_xy;
                    }

                    if (num == 26)
                    {
                        arr[26] = value_xy;
                    }

                    if (num == 27)
                    {
                        arr[27] = value_xy;
                    }

                    if (num == 28)
                    {
                        arr[28] = value_xy;
                    }

                    if (num == 29)
                    {
                        arr[29] = value_xy;
                    }

                    if (num == 30)
                    {
                        arr[30] = value_xy;
                    }

                    if (num == 31)
                    {
                        arr[31] = value_xy;
                    }

                    if (num == 32)
                    {
                        arr[32] = value_xy;
                    }

                    if (num == 33)
                    {
                        arr[33] = value_xy;
                    }

                    if (num == 34)
                    {
                        arr[34] = value_xy;
                    }

                    if (num == 35)
                    {
                        arr[35] = value_xy;
                    }

                    if (num == 36)
                    {
                        arr[36] = value_xy;
                    }

                    if (num == 37)
                    {
                        arr[37] = value_xy;
                    }

                    if (num == 38)
                    {
                        arr[38] = value_xy;
                    }

                    if (num == 39)
                    {
                        arr[39] = value_xy;
                    }


                    if (num == 40)
                    {
                        arr[40] = value_xy;
                    }


                    if (num == 41)
                    {
                        arr[41] = value_xy;
                    }


                    if (num == 42)
                    {
                        arr[42] = value_xy;
                    }

                    if (num == 43)
                    {
                        arr[43] = value_xy;
                    }

                    if (num == 44)
                    {
                        arr[44] = value_xy;
                    }

                    if (num == 45)
                    {
                        arr[45] = value_xy;
                    }

                    if (num == 46)
                    {
                        arr[46] = value_xy;
                    }

                    if (num == 47)
                    {
                        arr[47] = value_xy;
                    }

                    if (num == 48)
                    {
                        arr[48] = value_xy;
                    }
                }
            }

            conf.output[index] =  Median49.median(arr);
        }
    }
}
