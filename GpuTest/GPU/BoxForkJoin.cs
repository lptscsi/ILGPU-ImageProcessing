using GpuTest.GPU.Core;
using GpuTest.Utils.Color;
using ILGPU;
using ILGPU.Runtime;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GpuTest.GPU
{
    /// <summary>
    /// Box filter (splits input into individiual channels)
    /// </summary>
    public static class BoxForkJoin
    {
        private static Action<AcceleratorStream, KernelConfig, ForkConfig> _forkKernel;
        private static Action<AcceleratorStream, KernelConfig, JoinConfig> _joinKernel;
        private static Action<AcceleratorStream, KernelConfig, BoxConfig> _boxKernel;


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

        public struct BoxConfig
        {
            public readonly int width;
            public readonly int height;
            public readonly int radius;

            public readonly ArrayView2D<float, Stride2D.DenseX> input;
            public readonly ArrayView2D<float, Stride2D.DenseX> output;

            public BoxConfig(int width, int height, ArrayView2D<float, Stride2D.DenseX> input, ArrayView2D<float, Stride2D.DenseX> output, int radius)
            {
                this.width = width;
                this.height = height;
                this.input = input;
                this.output = output;
                this.radius = radius;
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
            _ = LoadBoxKernel(device);
        }

   
        static Action<AcceleratorStream, KernelConfig, BoxConfig> LoadBoxKernel(Accelerator device)
        {
            return LazyInitializer.EnsureInitialized(ref _boxKernel, () =>
            {
                return device.LoadKernel<BoxConfig>(BoxKernel);
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
        /// Executes Box filter
        /// </summary>
        /// <param name="device"></param>
        /// <param name="inputImage"></param>
        /// <param name="radius"></param>
        public static async Task<GPUImage<RGBA32>> Execute(Accelerator device, GPUImage<RGBA32> inputImage, int radius = 1)
        {
            int resultWidth = (int)inputImage.Extent2D.X;
            int resultHeight = (int)inputImage.Extent2D.Y;

            int maxThreadsPerGroup = device.MaxNumThreadsPerGroup;
            int groupXDim = 16;
            int groupYDim = maxThreadsPerGroup / groupXDim;
            int gridXDim = divUp(inputImage.Extent2D.X, groupXDim);
            int gridYDim = divUp(inputImage.Extent2D.Y, groupYDim);
            KernelConfig kernelConfig = new KernelConfig(new Index2D(gridXDim, gridYDim), new Index2D(groupXDim, groupYDim));
            GPUImage<RGBA32> outputImage = new GPUImage<RGBA32>(resultWidth, resultHeight);

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

            Action<AcceleratorStream, KernelConfig, BoxConfig> boxKernel = LoadBoxKernel(device);
  
            var forkConf = new ForkConfig(inputImage.Extent2D.X, inputImage.Extent2D.Y, input2DView, bufferR.View, bufferG.View, bufferB.View);

            var confR = new BoxConfig(inputImage.Extent2D.X, inputImage.Extent2D.Y, bufferR.View, buffer2R.View, radius);
            var confG = new BoxConfig(inputImage.Extent2D.X, inputImage.Extent2D.Y, bufferG.View, buffer2G.View, radius);
            var confB = new BoxConfig(inputImage.Extent2D.X, inputImage.Extent2D.Y, bufferB.View, buffer2B.View, radius);

            var joinConf = new JoinConfig(inputImage.Extent2D.X, inputImage.Extent2D.Y, buffer2R.View, buffer2G.View, buffer2B.View, output2DView);
         
            forkKernel(stream, kernelConfig, forkConf);
            device.Synchronize();
            boxKernel(stream, kernelConfig, confR);
            boxKernel(stream, kernelConfig, confG);
            boxKernel(stream, kernelConfig, confB);
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

        static void BoxKernel(BoxConfig conf)
        {
            var idx = Grid.IdxX * Group.DimX + Group.IdxX;
            var idy = Grid.IdxY * Group.DimY + Group.IdxY;

            if (idy >= conf.height || idx >= conf.width)
            {
                return;
            }

            Index2D index = new Index2D(idx, idy);
            int RAD = conf.radius;
            int kernelSize = 2 * RAD + 1;
            float scale = 1f / (float) (kernelSize * kernelSize);

            float sum = 0;
            for (int y = -RAD; y <= RAD; y++)
            {
                for (int x = -RAD; x <= RAD; x++)
                {
                    int idy_y = idxClip(idy + y, conf.height);
                    int idx_x = idxClip(idx + x, conf.width);
   
                    float value_xy = conf.input[idx_x, idy_y];
                    sum += value_xy;
                }
            }

            conf.output[index] = sum * scale;
        }
    }
}
