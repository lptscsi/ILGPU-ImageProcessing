using GpuTest.GPU.Core;
using GpuTest.Utils.Color;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Runtime;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GpuTest.GPU
{
    /// <summary>
    /// Bilateral filter implementation
    /// </summary>
    public static class Bilateral
    {
        private static Action<KernelConfig, Config> _kernel1;
        private static Action<KernelConfig, Config> _kernel2;
        public struct Config
        {
            public readonly int width;
            public readonly int height;
            public readonly int radius;
            public readonly float sigmaColor;
            public readonly float sigmaSpace;
            public readonly ArrayView1D<RGBA32, Stride1D.Dense> input;
            public readonly ArrayView1D<RGBA32, Stride1D.Dense> output;
            public readonly ArrayView1D<float, Stride1D.Dense> cGaussian;

            public Config(int width, int height, ArrayView1D<RGBA32, Stride1D.Dense> input, ArrayView1D<RGBA32, Stride1D.Dense> output, int radius, float sigmaColor, float sigmaSpace, ArrayView1D<float, Stride1D.Dense> cGaussian)
            {
                this.width = width;
                this.height = height;
                this.input = input;
                this.output = output;
                this.radius = radius;
                this.sigmaColor = sigmaColor;
                this.sigmaSpace = sigmaSpace;
                this.cGaussian = cGaussian;
            }
        }

        #region Helper methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float EuclideanLen(Vec3 a, Vec3 b, float d)
        {
            float mod = Vec3.dist(a, b);
            return XMath.Exp(-mod / (2f * d * d));
        }

        static int divUp(int a, int b) => (a + b - 1) / b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int idxClip(int idx, int idxMax) => (idx > (idxMax - 1) ? (idxMax - 1) : (idx < 0 ? 0 : idx));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int flatten(int col, int row, int width, int height) => idxClip(col, width) + idxClip(row, height) * width;


        #endregion


        public static void WarmUp(Accelerator device)
        {
            _ = LoadKernel1(device);
            _ = LoadKernel2(device);
        }

        static Action<KernelConfig, Config> LoadKernel1(Accelerator device)
        {
            return LazyInitializer.EnsureInitialized(ref _kernel1, () =>
            {
                return device.LoadStreamKernel<Config>(Kernel1);
            });
        }

        static Action<KernelConfig, Config> LoadKernel2(Accelerator device)
        {
            return LazyInitializer.EnsureInitialized(ref _kernel2, () =>
            {
                return device.LoadStreamKernel<Config>(Kernel2);
            });
        }

        public static async Task<GPUImage<RGBA32>> Execute1(Accelerator device, GPUImage<RGBA32> inputImage, int radius, float sigmaColor, float sigmaSpace)
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
            ArrayView1D<RGBA32, Stride1D.Dense> inputView = await inputImage.ToDevice1D(device.DefaultStream);
            ArrayView1D<RGBA32, Stride1D.Dense> outputView = await outputImage.ToDevice1D(device.DefaultStream);

            var kernel = LoadKernel1(device);

            // заранее рассчитываем коэффициенты для разных радиусов
            int BUFF_SIZE = 2 * radius + 1;
            using var gaussianBuffer = device.Allocate1D<float>(BUFF_SIZE);
            float[] cGaussian = new float[BUFF_SIZE];
            for (int i = 0; i < BUFF_SIZE; ++i)
            {
                float x = i - radius;
                cGaussian[i] = XMath.Exp(-(x * x) / (2 * sigmaSpace * sigmaSpace));
            }
            ArrayViewExtensions.CopyFromCPU<float>(gaussianBuffer, device.DefaultStream, cGaussian);

            var conf = new Config(inputImage.Extent2D.X, inputImage.Extent2D.Y, inputView, outputView, radius, sigmaColor, sigmaSpace, gaussianBuffer.View);

            kernel(kernelConfig, conf);
            device.Synchronize();
            return outputImage;
        }


        public static async Task<GPUImage<RGBA32>> Execute2(Accelerator device, GPUImage<RGBA32> inputImage, int radius, float sigmaColor, float sigmaSpace)
        {
            int resultWidth = (int)inputImage.Extent2D.X;
            int resultHeight = (int)inputImage.Extent2D.Y;
            GPUImage<RGBA32> outputImage = new GPUImage<RGBA32>(resultWidth, resultHeight);

            int maxThreadsPerGroup = device.MaxNumThreadsPerGroup;
            int groupXDim = 16;
            int groupYDim = maxThreadsPerGroup / groupXDim;
            int gridXDim = divUp(inputImage.Extent2D.X, groupXDim);
            int gridYDim = divUp(inputImage.Extent2D.Y, groupYDim);

            var sharedMemConfig = SharedMemoryConfig.RequestDynamic<byte>((groupXDim + 2 * radius) * (groupYDim + 2 * radius) * Vec3.SIZE);
            KernelConfig kernelConfig = new KernelConfig(new Index2D(gridXDim, gridYDim), new Index2D(groupXDim, groupYDim), sharedMemConfig);
   
            ArrayView1D<RGBA32, Stride1D.Dense> inputView = await inputImage.ToDevice1D(device.DefaultStream);
            ArrayView1D<RGBA32, Stride1D.Dense> outputView = await outputImage.ToDevice1D(device.DefaultStream);

            var kernel = LoadKernel2(device);

            // precalculation of gaussian coefficients 
            int BUFF_SIZE = 2 * radius + 1;
            using var gaussianBuffer = device.Allocate1D<float>(BUFF_SIZE);
            float[] cGaussian = new float[BUFF_SIZE];
            for (int i = 0; i < BUFF_SIZE; ++i)
            {
                float x = i - radius;
                cGaussian[i] = XMath.Exp(-(x * x) / (2 * sigmaSpace * sigmaSpace));
            }
            ArrayViewExtensions.CopyFromCPU<float>(gaussianBuffer, device.DefaultStream, cGaussian);

            var conf = new Config(inputImage.Extent2D.X, inputImage.Extent2D.Y, inputView, outputView, radius, sigmaColor, sigmaSpace, gaussianBuffer.View);

            kernel(kernelConfig, conf);
            device.Synchronize();
            return outputImage;
        }

        /// <summary>
        /// Simple kernel
        /// </summary>
        /// <param name="conf"></param>
        static void Kernel1(Config conf)
        {
            var x = Grid.IdxX * Group.DimX + Group.IdxX;
            var y = Grid.IdxY * Group.DimY + Group.IdxY;
     
            if (x >= conf.width || y >= conf.height)
                return;

            Vec3 center = conf.input[conf.width * y + x];
            float sum = 0;
            float factor;
            Vec3 t = new Vec3();
            int RAD = conf.radius;

            for (int rd = -RAD; rd <= RAD; rd++)
            {
                for (int cd = -RAD; cd <= RAD; cd++)
                {
                    int ix = x + cd;
                    int iy = y + rd;

                    if (iy < 0 || iy >= conf.height || ix < 0 || ix >= conf.width)
                        continue;

                    Vec3 curPix = conf.input[conf.width * iy + ix];
                    float spaceFactor = conf.cGaussian[rd + RAD] * conf.cGaussian[cd + RAD];
                    factor = spaceFactor * EuclideanLen(curPix, center, conf.sigmaColor);

                    t += factor * curPix;
                    sum += factor;
                }
            }

            t = t / sum;
            conf.output[conf.width * y + x] = new RGBA32(t.X, t.Y, t.Z);
        }

        /// <summary>
        /// Kernel uses shared memory (but it seems it is not faster than simple kernel)
        /// </summary>
        /// <param name="conf"></param>
        static void Kernel2(Config conf)
        {
            var s_dynamic = SharedMemory.GetDynamic<byte>();
            var s_in = s_dynamic.Cast<Vec3>();
            //var s_in = SharedMemory.Allocate1D<Vec3, Stride1D.Dense>(484, new Stride1D.Dense());

            var x = Grid.IdxX * Group.DimX + Group.IdxX;
            var y = Grid.IdxY * Group.DimY + Group.IdxY;

            int RAD = conf.radius;

            int idx = flatten(x, y, conf.width, conf.height);
            int s_c = Group.IdxX + RAD;
            int s_r = Group.IdxY + RAD;
            int s_w = Group.DimX + 2 * RAD;
            int s_h = Group.DimY + 2 * RAD;
            int s_i = flatten(s_c, s_r, s_w, s_h);

            s_in[s_i] = (Vec3)conf.input[idx];

            if (Group.IdxX < RAD && Group.IdxY < RAD)
            {
                s_in[flatten(s_c - RAD, s_r - RAD, s_w, s_h)] = conf.input[flatten(x - RAD, y - RAD, conf.width, conf.height)];
                s_in[flatten(s_c + Group.DimX, s_r - RAD, s_w, s_h)] = conf.input[flatten(x + Group.DimX, y - RAD, conf.width, conf.height)];
                s_in[flatten(s_c - RAD, s_r + Group.DimY, s_w, s_h)] = conf.input[flatten(x - RAD, y + Group.DimY, conf.width, conf.height)];
                s_in[flatten(s_c + Group.DimX, s_r + Group.DimY, s_w, s_h)] = conf.input[flatten(x + Group.DimX, y + Group.DimY, conf.width, conf.height)];
            }

            if (Group.IdxX < RAD)
            {
                s_in[flatten(s_c - RAD, s_r, s_w, s_h)] = conf.input[flatten(x - RAD, y, conf.width, conf.height)];
                s_in[flatten(s_c + Group.DimX, s_r, s_w, s_h)] = conf.input[flatten(x + Group.DimX, y, conf.width, conf.height)];
            }

            if (Group.IdxY < RAD)
            {
                s_in[flatten(s_c, s_r - RAD, s_w, s_h)] = conf.input[flatten(x, y - RAD, conf.width, conf.height)];
                s_in[flatten(s_c, s_r + Group.DimY, s_w, s_h)] = conf.input[flatten(x, y + Group.DimY, conf.width, conf.height)];
            }

            Group.Barrier();

            if (x >= conf.width || y >= conf.height)
                return;


            int s_idxCenter = flatten(s_c, s_r, s_w, s_h);
            Vec3 center = s_in[s_idxCenter];

            float sum = 0;
            Vec3 t = new Vec3();
         

            for (int rd = -RAD; rd <= RAD; rd++)
            {
                for (int cd = -RAD; cd <= RAD; cd++)
                {
                    int s_imgIdx = flatten(s_c + cd, s_r + rd, s_w, s_h);
                    Vec3 curPix = s_in[s_imgIdx];

                    float spaceFactor = conf.cGaussian[rd + RAD] * conf.cGaussian[cd + RAD];
                    float colorFactor = EuclideanLen(curPix, center, conf.sigmaColor);
                    float factor = spaceFactor * colorFactor;

                    t += factor * curPix;
                    sum += factor;
                }
            }

            t = t / sum;
            conf.output[conf.width * y + x] = t; // implicit conversion new RGBA32(t.X, t.Y, t.Z);
        }
    }
}
