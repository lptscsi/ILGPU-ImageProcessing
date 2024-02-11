using CrossImage.Common.Imaging;
using GpuTest.GPU.Core;
using GpuTest.Utils.Color;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Runtime;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GpuTest.GPU
{
    public class ConvolutionFilter
    {
        public const int maxKernelSize = 11;

        private static Action<KernelConfig, Data> _kernel;

        public struct Data
        {
            public readonly int radius;
   
            public readonly ArrayView2D<RGBA32, Stride2D.DenseX> input;
            public readonly ArrayView2D<RGBA32, Stride2D.DenseX> output;
            public readonly ArrayView1D<float, Stride1D.Dense> weights;

            public Data(ArrayView2D<RGBA32, Stride2D.DenseX> input, ArrayView2D<RGBA32, Stride2D.DenseX> output, ArrayView1D<float, Stride1D.Dense> weights, int kernelSize)
            {
                this.radius = kernelSize / 2;
                this.input = input;
                this.output = output;
                this.weights = weights;
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int divUp(int a, int b) => (a + b - 1) / b;

        private static float[] GetFilterWeights(FilterType filterType, int kernelSize, float sigma)
        {
            switch (filterType)
            {
                case FilterType.GaussianBlur:
                    return CreateGaussianKernel(sigma, kernelSize);
                case FilterType.BoxBlur:
                    return CreateBoxBlurKernel(kernelSize);
                case FilterType.LaplacianOfGaussianBlur:
                    return CreateLaplacianOfGaussianKernel(sigma, kernelSize);
                case FilterType.LaplacianBlur:
                    return CreateLaplacianKernel();
                case FilterType.CreateSobelXKernel:
                    return CreateSobelXKernel();
                case FilterType.CreateSobelYKernel:
                    return CreateSobelYKernel();
                case FilterType.HighPass:
                    return CreateHighPassKernel(kernelSize);
                case FilterType.LowPass:
                    return CreateLowPassKernel(kernelSize);
                case FilterType.Median:
                    return CreateMedianKernel(kernelSize);
                case FilterType.Emposs:
                    return CreateEmbossKernel();
                case FilterType.MotionBlur:
                    return CreateMotionBlurKernel(kernelSize, sigma % 1);
                case FilterType.PrewittX:
                    return CreatePrewittXKernel();
                case FilterType.PrewittY:
                    return CreatePrewittYKernel();
                case FilterType.RobertsX:
                    return CreateRobertsXKernel();
                case FilterType.RobertsY:
                    return CreateRobertsYKernel();
                case FilterType.FreiChenX:
                    return CreateFreiChenXKernel();
                case FilterType.FreiChenY:
                    return CreateFreiChenYKernel();
                case FilterType.HorizontalBlur:
                    return CreateHorizontalBlurKernel(kernelSize);
                case FilterType.Sharpness:
                    return CreateSharpnessKernel();
                default:
                    throw new ArgumentOutOfRangeException(nameof(filterType));
            }
        }

        public static void WarmUp(Accelerator device)
        {
            _ = CreateSharpnessKernel(1);
            _ = LoadKernel(device);
        }

        static Action<KernelConfig, Data> LoadKernel(Accelerator device)
        {
            return LazyInitializer.EnsureInitialized(ref _kernel, () =>
            {
                return device.LoadStreamKernel<Data>(GpuKernel);
            });
        }
       
        static void GpuKernel(Data data)
        {
            int width = data.input.IntExtent.X;
            int height = data.input.IntExtent.Y;
           
            var x = Grid.IdxX * Group.DimX + Group.IdxX;
            var y = Grid.IdxY * Group.DimY + Group.IdxY;

            if (x >= width || y >= height)
                return;

            Vec3 sum = new Vec3();

            // Convolve the kernel with the surrounding pixels
            int RAD = data.radius;
            int kernelSize = 2 * RAD + 1;

            for (int ky = -RAD; ky <= RAD; ky++)
            {
                for (int kx = -RAD; kx <= RAD; kx++)
                {
                    int ix = x + kx;
                    int iy = y + ky;

                    // Clamp the coordinates to the image bounds
                    ix = XMath.Clamp(ix, 0, width - 1);
                    iy = XMath.Clamp(iy, 0, height - 1);

                    Vec3 color = data.input[ix, iy];

                    int s_idx = (ky + RAD) * kernelSize + (kx + RAD);
                    float weight = data.weights[s_idx];
                    sum += color * weight;
                }
            }

            data.output[x, y] = new RGBA32(sum);
        }

        #region Filter Kerns


        public static float[] CreateLaplacianOfGaussianKernel(float sigma, int size)
        {
            float[] kernel = new float[maxKernelSize * maxKernelSize];

            int halfSize = size / 2;
            for (int y = -halfSize; y <= halfSize; y++)
            {
                for (int x = -halfSize; x <= halfSize; x++)
                {
                    float value = (float)((x * x + y * y - 2 * sigma * sigma) / (sigma * sigma * sigma * sigma)) * (float)Math.Exp(-(x * x + y * y) / (2.0f * sigma * sigma));
                    kernel[((y + halfSize) * size) + (x + halfSize)] = value;
                }
            }

            return kernel;
        }

        private static float[] CreateHorizontalBlurKernel(int kernelSize)
        {
            float[] kernel = new float[kernelSize * kernelSize];
            float value = 1f / kernelSize;

            for (int y = 0; y < kernelSize; y++)
            {
                for (int x = 0; x < kernelSize; x++)
                {
                    if (y == kernelSize / 2)
                    {
                        kernel[y * kernelSize + x] = value;
                    }
                    else
                    {
                        kernel[y * kernelSize + x] = 0;
                    }
                }
            }

            return kernel;
        }

        // Creates a Gaussian kernel with the given sigma and size
        public static float[] CreateGaussianKernel(float sigma, int size)
        {
            float[] kernel = new float[maxKernelSize * maxKernelSize];

            float sum = 0;

            int halfSize = size / 2;
            for (int y = -halfSize; y <= halfSize; y++)
            {
                for (int x = -halfSize; x <= halfSize; x++)
                {
                    float value = (float)(1.0f / (2.0f * Math.PI * sigma * sigma) * Math.Exp(-(x * x + y * y) / (2.0f * sigma * sigma)));
                    kernel[((y + halfSize) * size) + (x + halfSize)] = value;
                    sum += value;
                }
            }

            // Normalize the kernel to ensure the sum of all values is 1
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    kernel[(y * size) + x] = kernel[(y * size) + x] / sum;
                }
            }

            return kernel;
        }

        public static float[] CreateBoxBlurKernel(int size)
        {
            float[] kernel = new float[size * size];

            for (int i = 0; i < kernel.Length; i++)
            {
                kernel[i] = 1.0f / (float)(size * size);
            }

            return kernel;
        }


        public static float[] CreateSharpnessKernel(float sharpness = 0.7f)
        {
            //return new float[] { -1, -1, -1, -1, 9, -1, -1, -1, -1 };
            float[,] kern = RawSharpHelper.GetCoeff(sharpness);
            return RawSharpHelper.Pack(kern);
        }

        public static float[] CreateLaplacianKernel()
        {
            return new float[] { 0, 1, 0, 1, -4, 1, 0, 1, 0 };
        }


        public static float[] CreateSobelXKernel()
        {
            return new float[] { -1, 0, 1, -2, 0, 2, -1, 0, 1 };
        }

        public static float[] CreateSobelYKernel()
        {
            return new float[] { -1, -2, -1, 0, 0, 0, 1, 2, 1 };
        }

        public static float[] CreateHighPassKernel(int size)
        {
            float[] kernel = new float[size * size];
            int center = size / 2;

            for (int i = 0; i < kernel.Length; i++)
            {
                kernel[i] = -1.0f / (float)(size * size);
            }

            kernel[(center * size) + center] = 1.0f - kernel.Length;

            return kernel;
        }

        public static float[] CreateLowPassKernel(int size)
        {
            float[] kernel = new float[size * size];
            float weight = 1.0f / (size * size);

            for (int i = 0; i < kernel.Length; i++)
            {
                kernel[i] = weight;
            }

            return kernel;
        }

        public static float[] CreateMedianKernel(int size)
        {
            float[] kernel = new float[size * size];

            for (int i = 0; i < kernel.Length; i++)
            {
                kernel[i] = 1.0f / (float)(size * size);
            }

            return kernel;
        }

        public static float[] CreateEmbossKernel()
        {
            return new float[] { -2, -1, 0, -1, 1, 1, 0, 1, 2 };
        }

        public static float[] CreateMotionBlurKernel(int size, float angle)
        {
            float[] kernel = new float[size * size];
            int center = size / 2;

            for (int i = 0; i < kernel.Length; i++)
            {
                kernel[i] = 0.0f;
            }

            float radians = angle * (float)Math.PI / 180.0f;
            float distance = (float)Math.Sqrt(2.0 * center * center);
            float xStep = (float)Math.Cos(radians) / distance;
            float yStep = (float)Math.Sin(radians) / distance;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dotProduct = dx * xStep + dy * yStep;

                    if (dotProduct >= 0.0f && dotProduct <= 1.0f)
                    {
                        kernel[(y * size) + x] = 1.0f / (dotProduct + 1.0f);
                    }
                }
            }

            // Normalize the kernel to ensure the sum of all values is 1
            float sum = kernel.Sum();
            for (int i = 0; i < kernel.Length; i++)
            {
                kernel[i] = kernel[i] / sum;
            }

            return kernel;
        }
     
        public static float[] CreatePrewittXKernel()
        {
            return new float[] { -1, 0, 1, -1, 0, 1, -1, 0, 1 };
        }

        public static float[] CreatePrewittYKernel()
        {
            return new float[] { -1, -1, -1, 0, 0, 0, 1, 1, 1 };
        }

        public static float[] CreateRobertsXKernel()
        {
            return new float[] { 1, 0, 0, 0, -1, 0, 0, 0, 0 };
        }

        public static float[] CreateRobertsYKernel()
        {
            return new float[] { 0, 1, 0, -1, 0, 0, 0, 0, 0 };
        }

        public static float[] CreateFreiChenXKernel()
        {
            return new float[] { -1, 0, 1, -XMath.Sqrt(2), 0, XMath.Sqrt(2), -1, 0, 1 };
        }

        public static float[] CreateFreiChenYKernel()
        {
            return new float[] { -1, -XMath.Sqrt(2), -1, 0, 0, 0, 1, XMath.Sqrt(2), 1 };
        }

        #endregion

        /// <summary>
        /// Executes filter
        /// </summary>
        /// <param name="device"></param>
        /// <param name="inputImage"></param>
        /// <param name="outputImage"></param>
        /// <param name="kernelSize"></param>
        public static async Task<GPUImage<RGBA32>> Execute(Accelerator device, FilterType filterType, GPUImage<RGBA32> inputImage, int kernelSize, int sigma = 3)
        {
            int resultWidth = (int)inputImage.Extent2D.X;
            int resultHeight = (int)inputImage.Extent2D.Y;

            int maxThreadsPerGroup = device.MaxNumThreadsPerGroup;
            int groupXDim = 16;
            int groupYDim = maxThreadsPerGroup / groupXDim;
            int gridXDim = divUp(inputImage.Extent2D.X, groupXDim);
            int gridYDim = divUp(inputImage.Extent2D.Y, groupYDim);
            GPUImage<RGBA32> outputImage = new GPUImage<RGBA32>(resultWidth, resultHeight);

            ArrayView2D<RGBA32, Stride2D.DenseX> inputView = await inputImage.ToDevice2D(device.DefaultStream);
            ArrayView2D<RGBA32, Stride2D.DenseX> outputView = await outputImage.ToDevice2D(device.DefaultStream);

            float[] weights = GetFilterWeights(filterType, kernelSize, sigma);
            using var weightsBuffer = device.Allocate1D<float>(weights.Length);
            ArrayViewExtensions.CopyFromCPU<float>(weightsBuffer, device.DefaultStream, weights);

            KernelConfig kernelConfig = new KernelConfig(new Index2D(gridXDim, gridYDim), new Index2D(groupXDim, groupYDim));

            var kernel = LoadKernel(device);
            var data = new Data(inputView, outputView, weightsBuffer.View, kernelSize);
            kernel(kernelConfig, data);
            device.Synchronize();

            return outputImage;
        }
    }
}
