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
    /// NLM denoise implementation (not good and slow)
    /// </summary>
    public static class NLM
    {
        const int BLOCK_SIZE = 8;

        private static Action<KernelConfig, Config> _kernel
            ;
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

            public readonly int BLOCK_RADIUS;

            public readonly ArrayView2D<RGBA32, Stride2D.DenseX> input;

            public readonly ArrayView2D<RGBA32, Stride2D.DenseX> output;

            public Config(int width, int height, ArrayView2D<RGBA32, Stride2D.DenseX> input, ArrayView2D<RGBA32, Stride2D.DenseX> output, int radius, int block_radius)
            {
                this.width = width;
                this.height = height;
                this.input = input;
                this.output = output;
                this.radius = radius;
                this.BLOCK_RADIUS = block_radius;
                FILTER_AREA = ((2 * radius + 1) * (2 * radius + 1));
                INV_FILTER_AREA = (1.0f / (float)FILTER_AREA);
                WEIGHT_THRESHOLD = 0.1f;
                LERP_THRESHOLD = 0.1f;
                LERPC = 0.2f;
                NOISE_VAL = 1.45f;
                NOISE = (1.0f / (NOISE_VAL * NOISE_VAL));
            }
        }

        #region Helper methods

        static int divUp(int a, int b) => (a + b - 1) / b;

        static float ColorDistance(in RGB a, in RGB b)
        {
            return (b.R - a.R) * (b.R - a.R) + (b.G - a.G) * (b.G - a.G) + (b.B - a.B) * (b.B - a.B);
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

        static Action<KernelConfig, Config> LoadKernel(Accelerator device)
        {
            return LazyInitializer.EnsureInitialized(ref _kernel, () =>
            {
                return device.LoadStreamKernel<Config>(KernelOptimized);
            });
        }
       
        /// <summary>
        /// Executes NLM filter
        /// </summary>
        /// <param name="device"></param>
        /// <param name="inputImage"></param>
        /// <param name="radius"></param>
        public static async Task<GPUImage<RGBA32>> Execute(Accelerator device, GPUImage<RGBA32> inputImage, int radius)
        {
            int resultWidth = (int)inputImage.Extent2D.X;
            int resultHeight = (int)inputImage.Extent2D.Y;
            GPUImage<RGBA32> outputImage = new GPUImage<RGBA32>(resultWidth, resultHeight);

            int maxThreadsPerGroup = device.MaxNumThreadsPerGroup;
            int groupXDim = BLOCK_SIZE;
            int groupYDim = BLOCK_SIZE;
            int gridXDim = divUp(inputImage.Extent2D.X, groupXDim);
            int gridYDim = divUp(inputImage.Extent2D.Y, groupYDim);
            KernelConfig kernelConfig = new KernelConfig(new Index2D(gridXDim, gridYDim), new Index2D(groupXDim, groupYDim));

            ArrayView2D<RGBA32, Stride2D.DenseX> inputView = await inputImage.ToDevice2D(device.DefaultStream);
            ArrayView2D<RGBA32, Stride2D.DenseX> outputView =await outputImage.ToDevice2D(device.DefaultStream);

            var kernel = LoadKernel(device);
            var conf = new Config(inputImage.Extent2D.X, inputImage.Extent2D.Y, inputView, outputView, radius, block_radius: 2);
            kernel(kernelConfig, conf);
            device.Synchronize();

            return outputImage;
        }

     
        /// <summary>
        /// Optimized kernel
        /// </summary>
        /// <param name="index"></param>
        /// <param name="conf"></param>
        static void KernelOptimized(Config conf)
        {
            var fWeights = SharedMemory.Allocate1D<float, Stride1D.Dense>(new Index1D(BLOCK_SIZE * BLOCK_SIZE), new Stride1D.Dense());
            var x = Grid.IdxX * Group.DimX + Group.IdxX;
            var y = Grid.IdxY * Group.DimY + Group.IdxY;

            if (y >= conf.height || x >= conf.width)
                return;

            int RAD = conf.radius;

            int cx = Grid.IdxX * Group.DimX + RAD;
            int cy = Grid.IdxY * Group.DimY + RAD;

            //Find color distance from current texel to the center of NLM window
            float weight = 0;

            int BLOCK_RAD = conf.BLOCK_RADIUS;

            for (int n = -BLOCK_RAD; n <= BLOCK_RAD; n++)
                for (int m = -BLOCK_RAD; m <= BLOCK_RAD; m++)
                {

                    int x1 = cx + m, y1 = cy + n;
                    int x2 = x + m, y2 = y + n;
         
                    Vec3 color1 = new Vec3(conf.input[x1, y1]);
                    Vec3 color2 = new Vec3(conf.input[x2, y2]);
                    weight += Vec3.dist(color1, color2);
                }

            //Geometric distance from current texel to the center of NLM window
            float dist =
                (Group.IdxX - RAD) * (Group.IdxX - RAD) +
                (Group.IdxY - RAD) * (Group.IdxY - RAD);

            //Derive final weight from color and geometric distance
            weight = XMath.Exp(-(weight * conf.NOISE + dist * conf.INV_FILTER_AREA));

            //Write the result to shared memory
            fWeights[Group.IdxY * BLOCK_SIZE + Group.IdxX] = weight;

            //Wait until all the weights are ready
            Group.Barrier();

            // Normalized counter for the weight threshold
            float fCount = 0;

            // Total sum of pixel weights
            float sumWeights = 0;

            // Result accumulator
            Vec3 clr = new Vec3(0);
            int idx = 0;

            for (int i = -RAD; i <= RAD; i++)
            {
                for (int j = -RAD; j <= RAD; j++)
                {
                    //Load precomputed weight
                    float weightIJ = fWeights[idx++];

                    int x1 = x + j, y1 = y + i;
                   
                    Vec3 clrIJ = new Vec3(conf.input[x1, y1]);

                    clr += (clrIJ * weightIJ);

                    sumWeights += weightIJ;
                    fCount += (weightIJ > conf.WEIGHT_THRESHOLD) ? conf.INV_FILTER_AREA : 0;
                }
            }

            // Normalize result color
            sumWeights = 1.0f / sumWeights;
            clr = clr * sumWeights;

            Vec3 clr00 = new Vec3(conf.input[x, y]);
            float lerpQ = (fCount > conf.LERP_THRESHOLD) ? conf.LERPC : 1.0f - conf.LERPC;

            float R = Lerpf(clr.X, clr00.X, lerpQ);
            float G = Lerpf(clr.Y, clr00.Y, lerpQ);
            float B = Lerpf(clr.Z, clr00.Z, lerpQ);

            conf.output[x, y] = new RGBA32(R, G, B);
        }

        /// <summary>
        /// UnOptimized kernel
        /// </summary>
        /// <param name="index"></param>
        /// <param name="conf"></param>
        static void KernelNaive(Index2D index, Config conf)
        {
            var x = index.X;
            var y = index.Y;

            int RAD = conf.radius; 
            int BLOCK_RAD = conf.BLOCK_RADIUS;

            // Normalized counter for the weight threshold
            float fCount = 0;

            // Total sum of pixel weights
            float sum_weights = 0;

            // Result accumulator
            float R = 0, G = 0, B = 0;
            
            for (int i = -RAD; i <= RAD; i++)
            {
                for (int j = -RAD; j <= RAD; j++)
                {
                    //Find color distance from (x, y) to (x + j, y + i)
                    float weightIJ = 0;

                    for (int n = -BLOCK_RAD; n <= BLOCK_RAD; n++)
                        for (int m = -BLOCK_RAD; m <= BLOCK_RAD; m++)
                        {
                            int x1 = x + j + m, y1 = y + i + n;
                            int x2 = x + m, y2 = y + n;

                            RGB color1 = conf.input[x1, y1];
                            RGB color2 = conf.input[x2, y2];
                            weightIJ += ColorDistance(color1, color2);
                        }

                    //Derive final weight from color and geometric distance
                    weightIJ = XMath.Exp(-(weightIJ * conf.NOISE + (i * i + j * j) * conf.INV_FILTER_AREA));

                    // Center of the filter
                    RGB colorIJ = conf.input[x + j, y + i];

                    R += colorIJ.R * weightIJ;
                    G += colorIJ.G * weightIJ;
                    B += colorIJ.B * weightIJ;

                    sum_weights += weightIJ;
                    fCount += (weightIJ > conf.WEIGHT_THRESHOLD) ? conf.INV_FILTER_AREA : 0;
                }
            }

            // Normalize result color
            sum_weights = 1.0f / sum_weights;
            R *= sum_weights;
            G *= sum_weights;
            B *= sum_weights;

            RGB clr00 = conf.input[x, y];
            float lerpQ = (fCount > conf.LERP_THRESHOLD) ? conf.LERPC : 1.0f - conf.LERPC;

            R = Lerpf(R, clr00.R, lerpQ);
            G = Lerpf(G, clr00.G, lerpQ);
            B = Lerpf(B, clr00.B, lerpQ);

            conf.output[index] = new RGBA32(R, G, B);
        }
    }
}
