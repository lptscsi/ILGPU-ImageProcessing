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
    /// KNN denoise implementation
    /// </summary>
    public static class KNN
    {
        private static Action<Index2D, Config> _kernel;

        public struct Config
        {
            public readonly int width;
            public readonly int height;
            public readonly int radius;

            public readonly float FILTER_AREA;

            public readonly float INV_FILTER_AREA;

            public readonly float WEIGHT_THRESHOLD;

            // The blending quotient lerpC,  has working range[0.00, 0.33]
            public readonly float LERPC;

            // Usually 66%
            public readonly float LERP_THRESHOLD;

            public readonly float NOISE_VAL;

            public readonly float NOISE;

            public readonly ArrayView2D<RGBA32, Stride2D.DenseX> input;
            public readonly ArrayView2D<RGBA32, Stride2D.DenseX> output;

            public Config(int width, int height, ArrayView2D<RGBA32, Stride2D.DenseX> input, ArrayView2D<RGBA32, Stride2D.DenseX> output, int radius)
            {
                this.width = width;
                this.height = height;
                this.input = input;
                this.output = output;
                this.radius = radius;
                FILTER_AREA = ((2 * radius + 1) * (2 * radius + 1));
                INV_FILTER_AREA = (1.0f / (float)FILTER_AREA);
                WEIGHT_THRESHOLD = 0.02f;
                LERP_THRESHOLD = 0.79f;
                LERPC = 0.2f;
                NOISE_VAL = 0.32f;
                NOISE = (1.0f / (NOISE_VAL * NOISE_VAL));
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
        /// Executes KNN filter
        /// </summary>
        /// <param name="device"></param>
        /// <param name="inputImage"></param>
        /// <param name="radius"></param>
        public static async Task<GPUImage<RGBA32>> Execute(Accelerator device, GPUImage<RGBA32> inputImage, int radius)
        {
            int resultWidth = (int)inputImage.Extent2D.X;
            int resultHeight = (int)inputImage.Extent2D.Y;
            GPUImage<RGBA32> outputImage = new GPUImage<RGBA32>(resultWidth, resultHeight);

            ArrayView2D<RGBA32, Stride2D.DenseX> inputView = await inputImage.ToDevice2D(device.DefaultStream);
            ArrayView2D<RGBA32, Stride2D.DenseX> outputView = await outputImage.ToDevice2D(device.DefaultStream);

            var kernel = LoadKernel(device);
            var conf = new Config(inputImage.Extent2D.X, inputImage.Extent2D.Y, inputView, outputView, radius);
            kernel(inputImage.Extent2D, conf);
            device.Synchronize();

            return outputImage;
        }

        static void Kernel(Index2D index, Config conf)
        {
            var idx = index.X;
            var idy = index.Y;
            int RAD = conf.radius;
            // Normalized counter for the weight threshold
            float fCount = 0;

            // Total sum of pixel weights
            float sum_weights = 0;

            // Result accumulator
            float R = 0, G = 0, B = 0;

            // Center of the filter
            RGB color_center = conf.input[index];
            
            for (int y = -RAD; y <= RAD; y++)
            {
                for (int x = -RAD; x <= RAD; x++)
                {
                    int idy_y = idy + y;
                    int idx_x = idx + x;

                    if (idy_y < 0 || idy_y >= conf.height || idx_x < 0 || idx_x >= conf.width)
                        continue;

                    RGB color_xy = conf.input[idx_x, idy_y];

                    float pixel_distance = PixelDistance((float)x, (float)y);

                    float color_distance = ColorDistance(color_center, color_xy);

                    // Denoising
                    float weight_xy = XMath.Exp(-(pixel_distance * conf.INV_FILTER_AREA + color_distance * conf.NOISE));

                    R += color_xy.R * weight_xy;
                    G += color_xy.G * weight_xy;
                    B += color_xy.B * weight_xy;

                    sum_weights += weight_xy;
                    fCount += (weight_xy > conf.WEIGHT_THRESHOLD) ? conf.INV_FILTER_AREA : 0;
                }
            }

            // Normalize result color
            sum_weights = 1.0f / sum_weights;
            R *= sum_weights;
            G *= sum_weights;
            B *= sum_weights;

            float lerpQ = (fCount > conf.LERP_THRESHOLD) ? conf.LERPC : 1.0f - conf.LERPC;
            
            R = Lerpf(R, color_center.R, lerpQ);
            G = Lerpf(G, color_center.G, lerpQ);
            B = Lerpf(B, color_center.B, lerpQ);
         
            conf.output[index] = new RGBA32(R, G, B);
        }
    }
}
