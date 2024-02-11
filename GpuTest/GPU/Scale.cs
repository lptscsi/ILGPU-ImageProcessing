using GpuTest.GPU.Core;
using GpuTest.Utils.Color;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Runtime;
using OpenCvSharp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GpuTest.GPU
{
    /// <summary>
    /// Downscaling filter (not yet completed)
    /// </summary>
    public static class Scale
    {

        /// <summary>
        /// Structure for storing information on the target size and downscale factor
        /// </summary>
        /// <param name="size"></param>
        /// <param name="scale"></param>
        public record struct DownscaleInfo(Size size, float scale);


        public struct DownscaleData
        {
            public readonly float downscaleFactor;
            public readonly int radius;

            public readonly dImage<RGBA32> input;
            public readonly ArrayView2D<RGBA32, Stride2D.DenseX> output;
            public readonly ArrayView1D<float, Stride1D.Dense> cGaussian;

            public DownscaleData(dImage<RGBA32> input, ArrayView2D<RGBA32, Stride2D.DenseX> output, ArrayView1D<float, Stride1D.Dense> cGaussian, float downscaleFactor)
            {
                this.downscaleFactor = downscaleFactor;
                this.input = input;
                this.output = output;
                this.cGaussian = cGaussian;
                this.radius = ((int)XMath.Floor(downscaleFactor)) / 2;
            }

        }

        public struct ResampleData
        {
            public readonly CubicResampler resampler;

            public readonly dImage<RGBA32> input;
            public readonly ArrayView2D<RGBA32, Stride2D.DenseX> output;

            public ResampleData(dImage<RGBA32> input, ArrayView2D<RGBA32, Stride2D.DenseX> output, CubicResampler resampler)
            {
                this.input = input;
                this.output = output;
                this.resampler = resampler;
            }
        }

        public struct Tex2Data
        {
            public readonly dImage<RGBA32> input;
            public readonly ArrayView2D<RGBA32, Stride2D.DenseX> output;

            public Tex2Data(dImage<RGBA32> input, ArrayView2D<RGBA32, Stride2D.DenseX> output)
            {
                this.input = input;
                this.output = output;
            }
        }

        #region Fields

        private static Action<KernelConfig, DownscaleData> _kernel1;

        private static Action<KernelConfig, ResampleData> _kernel2;

        private static Action<KernelConfig, Tex2Data> _kernel3;

        #endregion

        public static void WarmUp(Accelerator device)
        {
            _ = LoadKernel1(device);
            _ = LoadKernel2(device);
            _ = LoadKernel3(device);
        }

        static Action<KernelConfig, DownscaleData> LoadKernel1(Accelerator device)
        {
            return LazyInitializer.EnsureInitialized(ref _kernel1, () =>
            {
                return device.LoadStreamKernel<DownscaleData>(DownscaleKernel);
            });
        }

        static Action<KernelConfig, ResampleData> LoadKernel2(Accelerator device)
        {
            return LazyInitializer.EnsureInitialized(ref _kernel2, () =>
            {
                return device.LoadStreamKernel<ResampleData>(ResampleKernel);
            });
        }

        static Action<KernelConfig, Tex2Data> LoadKernel3(Accelerator device)
        {
            return LazyInitializer.EnsureInitialized(ref _kernel3, () =>
            {
                return device.LoadStreamKernel<Tex2Data>(Tex2DKernel);
            });
        }

        #region Helper methods

        /// <summary>
        /// Calculates new image size
        /// </summary>
        /// <param name="targetSize"></param>
        /// <param name="sourceSize"></param>
        /// <returns></returns>
        public static DownscaleInfo CalculateDownscaleInfo(Size targetSize, Size sourceSize)
        {
            Size size = RecalculateWithAspectRatio(targetSize, sourceSize);
            float scale = CalcDownscaleFactor(targetSize, sourceSize);
            return new DownscaleInfo(size, scale);
        }

        /// <summary>
        /// Calculates downscale factor
        /// </summary>
        /// <param name="targetSize"></param>
        /// <param name="sourceSize"></param>
        /// <returns></returns>
        static float CalcDownscaleFactor(Size targetSize, Size sourceSize)
        {
            double ratioX = (double)sourceSize.Width / targetSize.Width;
            double ratioY = (double)sourceSize.Height / targetSize.Height;
            float ratio = (float)(Math.Max(ratioX, ratioY));
            return MathF.Max(1, ratio);
        }

        /// <summary>
        /// Calculates a new image size respecting aspect ratio
        /// </summary>
        /// <param name="targetSize"></param>
        /// <param name="sourceSize"></param>
        /// <returns></returns>
        static Size RecalculateWithAspectRatio(Size targetSize, Size sourceSize)
        {
            int width;
            int height;
            if (sourceSize.Width > targetSize.Width || sourceSize.Height > targetSize.Height)
            {
                // Сохранение пропорций изображения
                double ratioX = (double)targetSize.Width / sourceSize.Width;
                double ratioY = (double)targetSize.Height / sourceSize.Height;
                double ratio = Math.Min(ratioX, ratioY);
                width = (int)(sourceSize.Width * ratio);
                height = (int)(sourceSize.Height * ratio);
            }
            else
            {
                width = sourceSize.Width;
                height = sourceSize.Height;
            }

            return new Size()
            {
                Width = width,
                Height = height
            };
        }


        static int divUp(int a, int b) => (a + b - 1) / b;


        #endregion

        /// <summary>
        /// Execute downscale  with fixed weights with a factor [2, 100] 
        /// </summary>
        /// <param name="device"></param>
        /// <param name="inputImage"></param>
        /// <param name="downScaleFactor"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static async Task<GPUImage<RGBA32>> ExecuteDownScale(Accelerator device, GPUImage<RGBA32> inputImage, float downScaleFactor, float sigma = 3f)
        {
            if (downScaleFactor < 2 || downScaleFactor > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(downScaleFactor));
            }

            int resultWidth = (int)Math.Floor(inputImage.Extent2D.X / downScaleFactor);
            int resultHeight = (int)Math.Floor(inputImage.Extent2D.Y / downScaleFactor);

            int maxThreadsPerGroup = device.MaxNumThreadsPerGroup;
            int groupXDim = 16;
            int groupYDim = maxThreadsPerGroup / groupXDim;
            int gridXDim = divUp(resultWidth, groupXDim);
            int gridYDim = divUp(resultHeight, groupYDim);
            KernelConfig kernelConfig = new KernelConfig(new Index2D(gridXDim, gridYDim), new Index2D(groupXDim, groupYDim));

            GPUImage<RGBA32> outputImage = new GPUImage<RGBA32>(resultWidth, resultHeight);

            dImage<RGBA32> inputView = await inputImage.ToDevice(device.DefaultStream);
            ArrayView2D<RGBA32, Stride2D.DenseX> outputView = await outputImage.ToDevice2D(device.DefaultStream);

            // precalculation of gaussian coefficients 
            int radius = (int)Math.Floor(downScaleFactor / 2);
            int BUFF_SIZE = radius * 2 + 1;
            float[] cGaussian = new float[BUFF_SIZE];
            for (int i = 0; i < BUFF_SIZE; ++i)
            {
                float x = i - radius;
                cGaussian[i] = XMath.Exp(-(x * x) / (2 * sigma * sigma));
            }
            using var gaussianBuffer = device.Allocate1D<float>(BUFF_SIZE);
            ArrayViewExtensions.CopyFromCPU<float>(gaussianBuffer, device.DefaultStream, cGaussian);

            var kernel = LoadKernel1(device);
         
            var conf = new DownscaleData(inputView, outputView, gaussianBuffer.View, downScaleFactor);

            kernel(kernelConfig, conf);
            device.Synchronize();
            return outputImage;
        }

        /// <summary>
        /// Execute scale or downscale with resampling (usually with a factor [0.5, 10.0] )
        /// </summary>
        /// <param name="device"></param>
        /// <param name="inputImage"></param>
        /// <param name="scaleFactor"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static async Task<GPUImage<RGBA32>> ExecuteResample(Accelerator device, GPUImage<RGBA32> inputImage, float scaleFactor)
        {
            if (scaleFactor <= 0 || scaleFactor > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(scaleFactor));
            }

            int resultWidth = (int)Math.Round(inputImage.Extent2D.X * scaleFactor);
            int resultHeight = (int)Math.Round(inputImage.Extent2D.Y * scaleFactor);

            int maxThreadsPerGroup = device.MaxNumThreadsPerGroup;
            int groupXDim = 16;
            int groupYDim = maxThreadsPerGroup / groupXDim;
            int gridXDim = divUp(resultWidth, groupXDim);
            int gridYDim = divUp(resultHeight, groupYDim);
            KernelConfig kernelConfig = new KernelConfig(new Index2D(gridXDim, gridYDim), new Index2D(groupXDim, groupYDim));

            GPUImage<RGBA32> outputImage = new GPUImage<RGBA32>(resultWidth, resultHeight);

            dImage<RGBA32> inputView = await inputImage.ToDevice(device.DefaultStream);
            ArrayView2D<RGBA32, Stride2D.DenseX> outputView = await outputImage.ToDevice2D(device.DefaultStream);

            var kernel = LoadKernel2(device);

            CubicResampler resampler = CubicResampler.Hermite;


            var conf = new ResampleData(inputView, outputView, resampler);

            kernel(kernelConfig, conf);
            device.Synchronize();
            return outputImage;
        }

        /// <summary>
        /// Execute scaling with bilinear or bicubic interpolation
        /// </summary>
        /// <param name="device"></param>
        /// <param name="inputImage"></param>
        /// <param name="scaleFactor"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static async Task<GPUImage<RGBA32>> ExecuteTex2D(Accelerator device, GPUImage<RGBA32> inputImage, float scaleFactor)
        {
            if (scaleFactor <= 0 || scaleFactor > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(scaleFactor));
            }

            int resultWidth = (int)Math.Round(inputImage.Extent2D.X * scaleFactor);
            int resultHeight = (int)Math.Round(inputImage.Extent2D.Y * scaleFactor);

            int maxThreadsPerGroup = device.MaxNumThreadsPerGroup;
            int groupXDim = 16;
            int groupYDim = maxThreadsPerGroup / groupXDim;
            int gridXDim = divUp(resultWidth, groupXDim);
            int gridYDim = divUp(resultHeight, groupYDim);
            KernelConfig kernelConfig = new KernelConfig(new Index2D(gridXDim, gridYDim), new Index2D(groupXDim, groupYDim));

            GPUImage<RGBA32> outputImage = new GPUImage<RGBA32>(resultWidth, resultHeight);

            dImage<RGBA32> inputView = await inputImage.ToDevice(device.DefaultStream);
            ArrayView2D<RGBA32, Stride2D.DenseX> outputView = await outputImage.ToDevice2D(device.DefaultStream);

            var kernel = LoadKernel3(device);

            var conf = new Tex2Data(inputView, outputView);

            kernel(kernelConfig, conf);
            device.Synchronize();
            return outputImage;
        }

        /// <summary>
        /// Better for downscale
        /// </summary>
        /// <param name="data"></param>
        static void DownscaleKernel(DownscaleData data)
        {
            var idx = Grid.IdxX * Group.DimX + Group.IdxX;
            var idy = Grid.IdxY * Group.DimY + Group.IdxY;
            var outputExtent = data.output.Extent.ToIntIndex();
            int output_width = outputExtent.X;
            int output_height = outputExtent.Y;

            if (idy >= output_height || idx >= output_width)
                return;

            int RAD = data.radius;
            Vec3 color = new Vec3(0, 0, 0);
            int width = data.input.Width;
            int height = data.input.Height;

           
            float sum = 0;
            int startX = (int)XMath.Floor(idx * data.downscaleFactor);
            int startY = (int)XMath.Floor(idy * data.downscaleFactor);

            for (int rd = -RAD; rd <= RAD; rd++)
            {
                for (int cd = -RAD; cd <= RAD; cd++)
                {
                    int ix = startX + cd;
                    int iy = startY + rd;

                    if (iy < 0 || iy >= height || ix < 0 || ix >= width)
                        continue;

                    Vec3 curPix = data.input.Get(ix, iy);
                    float weight = data.cGaussian[rd + RAD] * data.cGaussian[cd + RAD];

                    color += weight * curPix;
                    sum += weight;
                }
            }

            color = color / sum;
            data.output[idx, idy] = color;
        }

        #region Resampling

        /// <summary>
        /// Better for upscale
        /// </summary>
        /// <param name="data"></param>
        static void ResampleKernel(ResampleData data)
        {
            var idx = Grid.IdxX * Group.DimX + Group.IdxX;
            var idy = Grid.IdxY * Group.DimY + Group.IdxY;
            var outputExtent = data.output.Extent.ToIntIndex();
            int output_width = outputExtent.X;
            int output_height = outputExtent.Y;

            if (idy >= output_height || idx >= output_width)
                return;

            // relative coordinates [0,1] 
            float u = (float)idx / (float)output_width;
            float v = (float)idy / (float)output_height;

            data.output[idx, idy] = GetInterpolatedColor(u, v, in data.input, in data.resampler);
        }

        private static int GetRangeStart(float radius, float center, int min, int max)
            => XMath.Clamp((int)XMath.Ceiling(center - radius), min, max);

        private static int GetRangeEnd(float radius, float center, int min, int max)
            => XMath.Clamp((int)XMath.Floor(center + radius), min, max);

        private static RGBA32 GetInterpolatedColor(float x, float y, in dImage<RGBA32> input, in CubicResampler resampler)
        {
            Vec3 color = new Vec3();

            int width = input.Width;
            int height = input.Height;
            float idx = (x * width);
            float idy = (y * height);


            float radius = resampler.Radius;

            int top = GetRangeStart(radius, idy, 0, height);
            int bottom = GetRangeEnd(radius, idy, 0, height);
            int left = GetRangeStart(radius, idx, 0, width);
            int right = GetRangeEnd(radius, idx, 0, width);


            for (int yK = top; yK <= bottom; yK++)
            {
                if (yK < 0 || yK >= height)
                {
                    continue;
                }

                float yWeight = resampler.GetValue(yK - idy);

                for (int xK = left; xK <= right; xK++)
                {
                    if (xK < 0 || xK >= width)
                    {
                        continue;
                    }
                    float xWeight = resampler.GetValue(xK - idx);

                    Vec3 current = (Vec3)input.Get(xK, yK);
                    color += current * xWeight * yWeight;
                }
            }

            return new RGBA32(color);
        }

        #endregion

        /// <summary>
        /// Using interpolation (bilinea or bicubic)
        /// </summary>
        /// <param name="data"></param>
        static void Tex2DKernel(Tex2Data data)
        {
            var idx = Grid.IdxX * Group.DimX + Group.IdxX;
            var idy = Grid.IdxY * Group.DimY + Group.IdxY;
            var outputExtent = data.output.Extent.ToIntIndex();
            int output_width = outputExtent.X;
            int output_height = outputExtent.Y;

            if (idy >= output_height || idx >= output_width)
                return;

            int width = data.input.Width;
            int height = data.input.Height;

            float scaleX = (float)output_width / (float)width;
            float scaleY = (float)output_height / (float)height;
            float j2 = idy / scaleY;
            float i2 = idx / scaleX;

            data.output[idx, idy] = data.input.Tex2Dcub(i2, j2);
            //data.output[idx, idy] = data.input.Tex2D(i2, j2);
            //data.output[idx, idy] = data.input.Tex2Dlin(i2, j2);
        }
    }
}
