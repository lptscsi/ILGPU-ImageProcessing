using GpuTest.GPU.Core;
using GpuTest.Utils.Color;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Runtime;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GpuTest.GPU
{
    /// <summary>
    /// 3D LUT implementation
    /// </summary>
    public static class LUT3D
    {
        const float EPS = 0.001f;
        private static Action<Index2D, Config> _kernel;

        /// <summary>
        /// struct to provide the data to the kernel for processing
        /// (it must be struct - fields readonly)
        /// </summary>
        public struct Config
        {
            public readonly int width;
            public readonly int height;
            public readonly int lutSize;
         

            public readonly ArrayView2D<RGBA32, Stride2D.DenseX> input;
            public readonly dBuffer4D<float> lut;
            public readonly ArrayView2D<RGBA32, Stride2D.DenseX> output;

            /// <summary>
            /// constructor
            /// </summary>
            /// <param name="width"></param>
            /// <param name="height"></param>
            /// <param name="input"></param>
            /// <param name="output"></param>
            /// <param name="lut"></param>
            /// <param name="lutSize"></param>
            public Config(int width, int height, ArrayView2D<RGBA32, Stride2D.DenseX> input, 
                ArrayView2D<RGBA32, Stride2D.DenseX> output,
                dBuffer4D<float> lut,
                int lutSize)
            {
                this.width = width;
                this.height = height;
                this.input = input;
                this.output = output;
                this.lut = lut;
                this.lutSize = lutSize;
            }
        }

        #region Helper methods

        static int divUp(int a, int b) => (a + b - 1) / b;
 
        static float ColorDistance(in RGB a, in RGB b)
        {
            return (b.R - a.R) * (b.R - a.R) + (b.G - a.G) * (b.G - a.G) + (b.B - a.B) * (b.B - a.B);
        }

        static float PixelDistance(in float x, in float y)
        {
            return x * x + y * y;
        }

        static float Lerpf(in float a, in float b, in float c)
        {
            return a + (b - a) * c;
        }


        #endregion

        /// <summary>
        /// preloads kernel
        /// </summary>
        /// <param name="device"></param>
        public static void WarmUp(Accelerator device)
        {
            _ = LoadKernel(device);
        }

        static Action<Index2D, Config> LoadKernel(Accelerator device)
        {
            return LazyInitializer.EnsureInitialized(ref _kernel, () =>
            {
               return device.LoadAutoGroupedStreamKernel<Index2D, Config>(Kernel);
            });
        }

        /// <summary>
        /// Executes 3D LUT filter
        /// </summary>
        /// <param name="device"></param>
        /// <param name="inputImage"></param>
        public static async Task<GPUImage<RGBA32>> Execute(Accelerator device, GPUImage<RGBA32> inputImage, GPUBuffer4D<float> lut, int lutSize)
        {
            int resultWidth = (int)inputImage.Extent2D.X;
            int resultHeight = (int)inputImage.Extent2D.Y;
            GPUImage<RGBA32> outputImage = new GPUImage<RGBA32>(resultWidth, resultHeight);

            ArrayView2D<RGBA32, Stride2D.DenseX> inputView = await inputImage.ToDevice2D(device.DefaultStream);
            ArrayView2D<RGBA32, Stride2D.DenseX> outputView = await outputImage.ToDevice2D(device.DefaultStream);
            dBuffer4D<float> lutView = await lut.ToDevice(device.DefaultStream);

            var kernel = LoadKernel(device);
            var conf = new Config(inputImage.Extent2D.X, inputImage.Extent2D.Y, inputView, outputView, lutView, lutSize);
            kernel(inputImage.Extent2D, conf);
            device.Synchronize();

            return outputImage;
        }

        static void Kernel(Index2D index, Config conf)
        {
            dBuffer4D<float> lut = conf.lut;
            float lutSize = conf.lutSize;

            // Center of the filter
            Vec3 color_xy = conf.input[index];

            // Get the real 3D index to be interpolated
            float r_o = color_xy.X * (lutSize - 1);
            float g_o = color_xy.Y * (lutSize - 1);
            float b_o = color_xy.Z * (lutSize - 1);

            // Implementation of a formula in the "Method" section: https://en.wikipedia.org/wiki/Trilinear_interpolation
            // Get the min/max indices of the interpolated 3D "box"
            int r1 = (int)XMath.Ceiling(r_o);
            int r0 = (int)XMath.Floor(r_o);
            int g1 = (int)XMath.Ceiling(g_o);
            int g0 = (int)XMath.Floor(g_o);
            int b1 = (int)XMath.Ceiling(b_o);
            int b0 = (int)XMath.Floor(b_o);
          

            float delta_r = (XMath.Abs(r_o - r0) < EPS || XMath.Abs(r1 - r0) < EPS ? 0 : (r_o - r0) / (r1 - r0));
            float delta_g = (XMath.Abs(g_o - g0) < EPS || XMath.Abs(g1 - g0) < EPS ? 0 : (g_o - g0) / (g1 - g0));
            float delta_b = (XMath.Abs(b_o - b0) < EPS || XMath.Abs(b1 - b0) < EPS ? 0 : (b_o - b0) / (b1 - b0));
     
            // 1st pass
            Vec3 v1 = new Vec3(lut.Get(r0, g0, b0, 0), lut.Get(r0, g0, b0, 1), lut.Get(r0, g0, b0, 2)) * (1 - delta_r) +
                  new Vec3(lut.Get(r1, g0, b0, 0), lut.Get(r1, g0, b0, 1), lut.Get(r1, g0, b0, 2)) * delta_r;

            Vec3 v2 = new Vec3(lut.Get(r0, g0, b1, 0), lut.Get(r0, g0, b1, 1), lut.Get(r0, g0, b1, 2)) * (1 - delta_r) +
                 new Vec3(lut.Get(r1, g0, b1, 0), lut.Get(r1, g0, b1, 1), lut.Get(r1, g0, b1, 2)) * delta_r;

            Vec3 v3 = new Vec3(lut.Get(r0, g1, b0, 0), lut.Get(r0, g1, b0, 1), lut.Get(r0, g1, b0, 2)) * (1 - delta_r) +
                new Vec3(lut.Get(r1, g1, b0, 0), lut.Get(r1, g1, b0, 1), lut.Get(r1, g1, b0, 2)) * delta_r;

            Vec3 v4 = new Vec3(lut.Get(r0, g1, b1, 0), lut.Get(r0, g1, b1, 1), lut.Get(r0, g1, b1, 2)) * (1 - delta_r) +
                new Vec3(lut.Get(r1, g1, b1, 0), lut.Get(r1, g1, b1, 1), lut.Get(r1, g1, b1, 2)) * delta_r;

            // 2nd step
            v1 = v1 * (1 - delta_g) + v3 * delta_g;
            v2 = v2 * (1 - delta_g) + v4 * delta_g;

            // 3rd step
            v1 = v1 * (1 - delta_b) + v2 * delta_b;

            conf.output[index] = v1;
        }
    }
}
