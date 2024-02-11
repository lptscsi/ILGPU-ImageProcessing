using GpuTest.GPU.Core;
using GpuTest.Utils.Color;
using ILGPU;
using ILGPU.Runtime;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static GpuTest.Utils.Median25;

namespace GpuTest.GPU
{
    /// <summary>
    /// Median 5x5
    /// </summary>
    public static class Median2
    {
        private static Action<KernelConfig, Config> _kernel;

        public struct Config
        {
            public readonly int width;
            public readonly int height;

            public readonly ArrayView1D<RGBA32, Stride1D.Dense> input;
            public readonly ArrayView1D<RGBA32, Stride1D.Dense> output;

            public Config(int width, int height, ArrayView1D<RGBA32, Stride1D.Dense> input, ArrayView1D<RGBA32, Stride1D.Dense> output)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int flatten(int col, int row, int width, int height) => idxClip(col, width) + idxClip(row, height) * width;

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
          
            ArrayView1D<RGBA32, Stride1D.Dense> inputView = await inputImage.ToDevice1D(device.DefaultStream);
            ArrayView1D<RGBA32, Stride1D.Dense> outputView = await outputImage.ToDevice1D(device.DefaultStream);
         
            int radius = 2;
            var sharedMemConfig = SharedMemoryConfig.RequestDynamic<byte>((groupXDim + 2 * radius) * (groupYDim + 2 * radius) * Vec3.SIZE);
            KernelConfig kernelConfig = new KernelConfig(new Index2D(gridXDim, gridYDim), new Index2D(groupXDim, groupYDim), sharedMemConfig);

            var kernel = LoadKernel(device);
            var conf = new Config(inputImage.Extent2D.X, inputImage.Extent2D.Y, inputView, outputView);
            kernel(kernelConfig, conf);
            device.Synchronize();

            return outputImage;
        }

    
        static void Kernel(Config conf)
        {
            var s_dynamic = SharedMemory.GetDynamic<byte>();
            var s_in = s_dynamic.Cast<Vec3>();

            var x = Grid.IdxX * Group.DimX + Group.IdxX;
            var y = Grid.IdxY * Group.DimY + Group.IdxY;

            if (x >= conf.width || y >= conf.height)
                return;

            const int RAD = 2;

            int idx = flatten(x, y, conf.width, conf.height);
            int s_c = Group.IdxX + RAD;
            int s_r = Group.IdxY + RAD;
            int s_w = Group.DimX + 2 * RAD;
            int s_h = Group.DimY + 2 * RAD;
            int s_i = flatten(s_c, s_r, s_w, s_h);
   
            s_in[s_i] = conf.input[idx];

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
         
            const int kernelSize = 2 * RAD + 1;
    

            ArrayView<float> red = LocalMemory.Allocate<float>(25);
            ArrayView<float> green = LocalMemory.Allocate<float>(25);
            ArrayView<float> blue = LocalMemory.Allocate<float>(25);

            //int s_imgIdx1 = flatten(s_c, s_r, s_w, s_h);
            //Vec3 center = s_in[s_imgIdx1];

            for (int rd = -RAD; rd <= RAD; rd++)
            {
                for (int cd = -RAD; cd <= RAD; cd++)
                {
                    int s_imgIdx = flatten(s_c + cd, s_r + rd, s_w, s_h);
                    Vec3 color_xy = s_in[s_imgIdx];

                    int y1 = rd + RAD;
                    int x1 = cd + RAD;
                    int num = y1 * kernelSize + x1;
                    
                    /*
                    // works 2.37 times slower
                    red[num] = color_xy.X;
                    green[num] = color_xy.Y;
                    blue[num] = color_xy.Z;
                    */

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

                    if (num == 9)
                    {
                        red[9] = color_xy.X;
                        green[9] = color_xy.Y;
                        blue[9] = color_xy.Z;
                    }

                    if (num == 10)
                    {
                        red[10] = color_xy.X;
                        green[10] = color_xy.Y;
                        blue[10] = color_xy.Z;
                    }

                    if (num == 11)
                    {
                        red[11] = color_xy.X;
                        green[11] = color_xy.Y;
                        blue[11] = color_xy.Z;
                    }

                    if (num == 12)
                    {
                        red[12] = color_xy.X;
                        green[12] = color_xy.Y;
                        blue[12] = color_xy.Z;
                    }

                    if (num == 13)
                    {
                        red[13] = color_xy.X;
                        green[13] = color_xy.Y;
                        blue[13] = color_xy.Z;
                    }

                    if (num == 14)
                    {
                        red[14] = color_xy.X;
                        green[14] = color_xy.Y;
                        blue[14] = color_xy.Z;
                    }

                    if (num == 15)
                    {
                        red[15] = color_xy.X;
                        green[15] = color_xy.Y;
                        blue[15] = color_xy.Z;
                    }

                    if (num == 16)
                    {
                        red[16] = color_xy.X;
                        green[16] = color_xy.Y;
                        blue[16] = color_xy.Z;
                    }

                    if (num == 17)
                    {
                        red[17] = color_xy.X;
                        green[17] = color_xy.Y;
                        blue[17] = color_xy.Z;
                    }

                    if (num == 18)
                    {
                        red[18] = color_xy.X;
                        green[18] = color_xy.Y;
                        blue[18] = color_xy.Z;
                    }

                    if (num == 19)
                    {
                        red[19] = color_xy.X;
                        green[19] = color_xy.Y;
                        blue[19] = color_xy.Z;
                    }

                    if (num == 20)
                    {
                        red[20] = color_xy.X;
                        green[20] = color_xy.Y;
                        blue[20] = color_xy.Z;
                    }

                    if (num == 21)
                    {
                        red[21] = color_xy.X;
                        green[21] = color_xy.Y;
                        blue[21] = color_xy.Z;
                    }

                    if (num == 22)
                    {
                        red[22] = color_xy.X;
                        green[22] = color_xy.Y;
                        blue[22] = color_xy.Z;
                    }

                    if (num == 23)
                    {
                        red[23] = color_xy.X;
                        green[23] = color_xy.Y;
                        blue[23] = color_xy.Z;
                    }

                    if (num == 24)
                    {
                        red[24] = color_xy.X;
                        green[24] = color_xy.Y;
                        blue[24] = color_xy.Z;
                    }
                }
            }


            float r = median(red);
            float g = median(green);
            float b = median(blue);

            conf.output[conf.width * y + x] = new RGBA32(r, g, b);
        }
    }
}
