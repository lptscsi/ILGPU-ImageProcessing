using GpuTest.GPU.Core;
using GpuTest.Utils.Color;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GpuTest.GPU
{
    /// <summary>
    /// Implementation for testing processing in different GPU streams with Bilateral filter
    /// it uses small chunks of image for parallel processing
    /// </summary>
    public static class StreamsTest
    {
        private static Action<AcceleratorStream, KernelConfig, Data1> _kernel1;

        private static Action<AcceleratorStream, KernelConfig, Data2> _kernel2;

        public struct Data1
        {
            public readonly int frameIndex;
            public readonly int frameHeight;

            public readonly dImage<RGBA32> input;
            public readonly dImage<RGBA32> output;

            public Data1(int frameIndex, int frameHeight, dImage<RGBA32> input, dImage<RGBA32> output)
            {
                this.frameIndex = frameIndex;
                this.frameHeight = frameHeight;
                this.input = input;
                this.output = output;
            }
        }

        public struct Data2
        {
            public readonly int frameIndex;
            public readonly int frameHeight;

            public readonly int radius;
            public readonly float sigmaColor;
            public readonly float sigmaSpace;
            public readonly dImage<RGBA32> input;
            public readonly dImage<RGBA32> output;
            public readonly ArrayView1D<float, Stride1D.Dense> cGaussian;

            public Data2(int frameIndex, int frameHeight, dImage<RGBA32> input, dImage<RGBA32> output, int radius, float sigmaColor, float sigmaSpace, ArrayView1D<float, Stride1D.Dense> cGaussian)
            {
                this.frameIndex = frameIndex;
                this.frameHeight = frameHeight;
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

        static Action<AcceleratorStream, KernelConfig, Data1> LoadKernel1(Accelerator device)
        {
            return LazyInitializer.EnsureInitialized(ref _kernel1, () =>
            {
                return device.LoadKernel<Data1>(Kernel1);
            });

        }

        static Action<AcceleratorStream, KernelConfig, Data2> LoadKernel2(Accelerator device)
        {
            return LazyInitializer.EnsureInitialized(ref _kernel2, () =>
            {
                return device.LoadKernel<Data2>(Kernel2);
            });
        }

        public record JobInfo(
            int resWidth,
            int resHeight, 
            int frameCount, 
            int frameHeight,
            IEnumerable<CropHelper.ImageBytesResult> frames, 
            int radius, 
            float sigmaColor, 
            float sigmaSpace);

        public record struct InitialInput(int frameIndex, CropHelper.ImageBytesResult frame);
        public record struct FrameOutput(int frameIndex);

        public static async Task<GPUImage<RGBA32>> Execute(Accelerator device, int width, int height, byte[] data)
        {
            Stopwatch sw = Stopwatch.StartNew();

            int frameHeight = 64;
            int frameCount = divUp(height, frameHeight);
            List<CropHelper.ImageBytesResult> frameList = new List<CropHelper.ImageBytesResult>();

            // splits the image into frames
            for(int i = 0; i < frameCount; i++)
            {
                int cropHeight = frameHeight;
                int start = i * frameHeight;
                int end = start + frameHeight;
                if (end > (height -1)) { 
                    end = height - 1;
                    cropHeight = end - start;
                }
                if (cropHeight > 0)
                {
                    var crop = CropHelper.Crop<RGBA32>(data, width, height, new OpenCvSharp.Rect(0, start, width, cropHeight));
                    frameList.Add(crop);
                }
            }

            var job = new JobInfo(width, height, frameList.Count, frameHeight, frameList, radius: 3, sigmaColor: 0.25f, sigmaSpace: 2f);

            sw.Stop();
            Console.WriteLine($"Split into frames Time: {sw.ElapsedMilliseconds}");
            sw.Restart();

            try
            {
                return await ExecuteInternal(device, job);
            }
            finally
            {
                sw.Stop();
                Console.WriteLine($"Streams processing Time: {sw.ElapsedMilliseconds}");
            }
        }

        /// <summary>
        /// Processes frames - combining them into te final image (with some additional processing)
        /// </summary>
        /// <param name="device"></param>
        /// <param name="job"></param>
        /// <returns></returns>
        static async Task<GPUImage<RGBA32>> ExecuteInternal(Accelerator device, JobInfo job)
        {
            int resultWidth = job.resWidth;
            int resultHeight = job.resHeight;

            int maxThreadsPerGroup = device.MaxNumThreadsPerGroup;
            int groupXDim = 16;
            int groupYDim = maxThreadsPerGroup / groupXDim;
            int gridXDim = divUp(resultWidth, groupXDim);
            int gridYDim = divUp(job.frameHeight, groupYDim);
            KernelConfig kernelConfig = new KernelConfig(new Index2D(gridXDim, gridYDim), new Index2D(groupXDim, groupYDim));

            GPUImage<RGBA32> outputImage = new GPUImage<RGBA32>(resultWidth, resultHeight);

            using AcceleratorStream stream1 = device.CreateStream();
            using AcceleratorStream stream2 = device.CreateStream();
         
            dImage<RGBA32> outputView = await outputImage.ToDevice(stream1);

            var kernel1 = LoadKernel1(device);
            var kernel2 = LoadKernel2(device);

            // заранее рассчитываем коэффициенты для разных радиусов
            int BUFF_SIZE = 2 * job.radius + 1;
            using var gaussianBuffer = device.Allocate1D<float>(BUFF_SIZE);
            float[] cGaussian = new float[BUFF_SIZE];
            for (int i = 0; i < BUFF_SIZE; ++i)
            {
                float x = i - job.radius;
                cGaussian[i] = XMath.Exp(-(x * x) / (2 * job.sigmaSpace * job.sigmaSpace));
            }
            ArrayViewExtensions.CopyFromCPU<float>(gaussianBuffer, device.DefaultStream, cGaussian);

            var initialBlock = new TransformBlock<InitialInput, FrameOutput>(async input =>
            {
                int index = input.frameIndex;
                var frame = input.frame;
                using (GPUImage<RGBA32> img = new GPUImage<RGBA32>(frame.Width, frame.Height, frame.Bytes))
                {
                    dImage<RGBA32> inputView = await img.ToDevice(stream1);
                    var data1 = new Data1(index, job.frameHeight, inputView, outputView);
                    kernel1(stream1, kernelConfig, data1);
                    stream1.Synchronize();
                }
                return new FrameOutput(input.frameIndex);
            }, new ExecutionDataflowBlockOptions
            {
                // It produces bad result with MaxDegreeOfParallelism > 1!!!
                MaxDegreeOfParallelism = 1,
                EnsureOrdered = false,
                BoundedCapacity = 3,
            });

            // Performs bilateral filtering
            var actionBlock = new ActionBlock<FrameOutput>(input =>
            {
                var data2 = new Data2(input.frameIndex, job.frameHeight, outputView, outputView, job.radius, job.sigmaColor, job.sigmaSpace, gaussianBuffer.View);
                kernel2(stream2, kernelConfig, data2);
                stream2.Synchronize();
                //return new FrameOutput(input.frameIndex);
            },
            new ExecutionDataflowBlockOptions
            {
                // It produces bad result with MaxDegreeOfParallelism > 1!!!
                MaxDegreeOfParallelism = 1,
                EnsureOrdered = false,
                BoundedCapacity = 3,
            });

            initialBlock.LinkTo(actionBlock, new DataflowLinkOptions
            {
                PropagateCompletion = true
            });
    
            int frameCount =job.frameCount;
            int index = 0;

            // repeating just for a longer processing and then to get average time for one iteration
            for (int repeatNum = 0; repeatNum < 100; repeatNum++)
            {
                foreach (var frame in job.frames)
                {
                    await initialBlock.SendAsync(new InitialInput(index, frame));
                    ++index;
                }

                index = 0;
            }

            initialBlock.Complete();
            await actionBlock.Completion;

            device.Synchronize();
            return outputImage;
        }


        /// <summary>
        /// Copy frame data the whole image
        /// </summary>
        /// <param name="conf"></param>
        static void Kernel1(Data1 conf)
        {
            var x0 = Grid.IdxX * Group.DimX + Group.IdxX;
            var y0 = Grid.IdxY * Group.DimY + Group.IdxY;

            if (x0 >= conf.input.Width || y0 >= conf.frameHeight)
                return;

            int startY = conf.frameHeight * conf.frameIndex;
            int x = x0;
            int y = startY + y0;

            if (y >= conf.output.Height)
            {
                return;
            }

            RGBA32 pix = conf.input.Get(x, y0);
   
            conf.output.Set(x, y, pix);
        }

        /// <summary>
        /// Process using bilateral filter
        /// </summary>
        /// <param name="conf"></param>
        static void Kernel2(Data2 conf)
        {
            var x0 = Grid.IdxX * Group.DimX + Group.IdxX;
            var y0 = Grid.IdxY * Group.DimY + Group.IdxY;
     
            if (x0 >= conf.input.Width || y0 >= conf.frameHeight)
                return;

            int startY = conf.frameHeight * conf.frameIndex;
            int x = x0;
            int y = startY + y0;

            Vec3 center = new Vec3(conf.input.Get(x, y));
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

                    if (iy < 0 || iy >= conf.input.Height || ix < 0 || ix >= conf.input.Width)
                        continue;

                    Vec3 curPix = new Vec3(conf.input.Get(ix, iy));
                    float spaceFactor = conf.cGaussian[rd + RAD] * conf.cGaussian[cd + RAD];
                    factor = spaceFactor * EuclideanLen(curPix, center, conf.sigmaColor);

                    t += factor * curPix;
                    sum += factor;
                }
            }

            t = t / sum;
            RGBA32 v = t;
            conf.output.Set(x, y, v);
        }
    }
}
