using GpuTest.GPU.Core;
using GpuTest.Utils.Color;
using ILGPU;
using ILGPU.Runtime;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static GpuTest.Utils.Median81;

namespace GpuTest.GPU
{
    /// <summary>
    /// Median using LAB colorspace 
    /// uses only a and b channels (the L channel is not modified)
    /// </summary>
    public static class MedianLAB
    {
        private static Action<AcceleratorStream, KernelConfig, LABConfig> _labKernel;
        private static Action<AcceleratorStream, KernelConfig, RGBConfig> _rgbKernel;
        private static Action<AcceleratorStream, KernelConfig, MedianConfig> _medianKernel1;
        private static Action<AcceleratorStream, KernelConfig, MedianConfig> _medianKernel2;

        #region Config

        /// <summary>
        /// Used for conversion from RGB to LAB colorspace
        /// </summary>
        public struct LABConfig
        {
            public readonly dImage<RGBA32> input;
            public readonly dImage<LAB> output;
            public readonly RGBToLABConverter Converter;

            public LABConfig(
                dImage<RGBA32> input,
                dImage<LAB> output
                )
            {
                this.input = input;
                this.output = output;
                this.Converter = new RGBToLABConverter();
            }
        }

        /// <summary>
        /// Used for applying median filter
        /// </summary>
        public struct MedianConfig
        {
            public readonly dImage<LAB> input;
            public readonly dImage<LAB> output;

            public MedianConfig(
                dImage<LAB> input,
                dImage<LAB> output
                )
            {
                this.input = input;
                this.output = output;
            }
        }

        /// <summary>
        /// Used for conversion from LAB to RGB colorspace
        /// </summary>
        public struct RGBConfig
        {
            public readonly dImage<LAB> input;
            public readonly dImage<RGBA32> output;
            public readonly LABToRGBConverter Converter;

            public RGBConfig(
                 dImage<LAB> input,
                 dImage<RGBA32> output)
            {
                this.input = input;
                this.output = output;
                this.Converter = new LABToRGBConverter();
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
            _ = LoadLabKernel(device);
            _ = LoadRgbKernel(device);
            _ = LoadMedianKernel1(device);
            _ = LoadMedianKernel2(device);
        }

   
        static Action<AcceleratorStream, KernelConfig, LABConfig> LoadLabKernel(Accelerator device)
        {
            return LazyInitializer.EnsureInitialized(ref _labKernel, () =>
            {
                return device.LoadKernel<LABConfig>(LabKernel);
            });
        }

        static Action<AcceleratorStream, KernelConfig, RGBConfig> LoadRgbKernel(Accelerator device)
        {
            return LazyInitializer.EnsureInitialized(ref _rgbKernel, () =>
            {
                return device.LoadKernel<RGBConfig>(RGBKernel);
            });
        }

        static Action<AcceleratorStream, KernelConfig, MedianConfig> LoadMedianKernel1(Accelerator device)
        {
            return LazyInitializer.EnsureInitialized(ref _medianKernel1, () =>
            {
                return device.LoadKernel<MedianConfig>(MedianKernel1);
            });
        }

        static Action<AcceleratorStream, KernelConfig, MedianConfig> LoadMedianKernel2(Accelerator device)
        {
            return LazyInitializer.EnsureInitialized(ref _medianKernel2, () =>
            {
                return device.LoadKernel<MedianConfig>(MedianKernel2);
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

            int maxThreadsPerGroup = device.MaxNumThreadsPerGroup;
            int groupXDim = 16;
            int groupYDim = maxThreadsPerGroup / groupXDim;
            int gridXDim = divUp(inputImage.Extent2D.X, groupXDim);
            int gridYDim = divUp(inputImage.Extent2D.Y, groupYDim);
            KernelConfig kernelConfig = new KernelConfig(new Index2D(gridXDim, gridYDim), new Index2D(groupXDim, groupYDim));
           
            GPUImage<LAB> imageLAB1 = new GPUImage<LAB>(resultWidth, resultHeight);
            GPUImage<LAB> imageLAB2 = new GPUImage<LAB>(resultWidth, resultHeight);
            GPUImage<RGBA32> outputImage = new GPUImage<RGBA32>(resultWidth, resultHeight);

            using AcceleratorStream stream = device.CreateStream();
       
            dImage<RGBA32> inputRGB = await inputImage.ToDevice(stream);
            dImage<LAB> lab1 = await imageLAB1.ToDevice(stream);
            dImage<LAB> lab2 = await imageLAB2.ToDevice(stream);
            dImage<RGBA32> outputRGB = await outputImage.ToDevice(stream);

            var labKernel = LoadLabKernel(device);
            var rgbKernel = LoadRgbKernel(device);

            Action<AcceleratorStream, KernelConfig, MedianConfig> medianKernel1 = LoadMedianKernel1(device);
            Action<AcceleratorStream, KernelConfig, MedianConfig> medianKernel2 = LoadMedianKernel2(device);

            var labConf = new LABConfig(inputRGB, lab1);
            labKernel(stream, kernelConfig, labConf);
            device.Synchronize();

            var medianConf1 = new MedianConfig(lab1, lab2);
            medianKernel1(stream, kernelConfig, medianConf1);
            device.Synchronize();

            var medianConf2 = new MedianConfig(lab2, lab1);
            medianKernel2(stream, kernelConfig, medianConf2);
            device.Synchronize();

            var rgbConf = new RGBConfig(lab1, outputRGB);
            rgbKernel(stream, kernelConfig, rgbConf);
            device.Synchronize();
            return outputImage;
        }

        static void LabKernel(LABConfig conf)
        {
            var idx = Grid.IdxX * Group.DimX + Group.IdxX;
            var idy = Grid.IdxY * Group.DimY + Group.IdxY;

            if (idy >= conf.input.Height || idx >= conf.input.Width)
            {
                return;
            }

            RGB color_xy = conf.input.Get(idx, idy);
            LAB lab = conf.Converter.Convert(color_xy);
            conf.output.Set(idx, idy, lab);
        }

        static void RGBKernel(RGBConfig conf)
        {
            var idx = Grid.IdxX * Group.DimX + Group.IdxX;
            var idy = Grid.IdxY * Group.DimY + Group.IdxY;

            if (idy >= conf.input.Height || idx >= conf.input.Width)
            {
                return;
            }

            LAB color = conf.input.Get(idx, idy);
            RGB rgb = conf.Converter.Convert(color);
            conf.output.Set(idx, idy, rgb);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void setVal<T>(ArrayView<T> arr, int num, T val)
            where T : unmanaged, INumber<T>
        {
            // unrolling indexes for better performance!!!
            if (num == 0)
            {
                arr[0] = val;
            }

            if (num == 1)
            {
                arr[1] = val;
            }

            if (num == 2)
            {
                arr[2] = val;
            }

            if (num == 3)
            {
                arr[3] = val;
            }

            if (num == 4)
            {
                arr[4] = val;
            }

            if (num == 5)
            {
                arr[5] = val;
            }

            if (num == 6)
            {
                arr[6] = val;
            }

            if (num == 7)
            {
                arr[7] = val;
            }

            if (num == 8)
            {
                arr[8] = val;
            }

            if (num == 9)
            {
                arr[9] = val;
            }

            if (num == 10)
            {
                arr[10] = val;
            }

            if (num == 11)
            {
                arr[11] = val;
            }

            if (num == 12)
            {
                arr[12] = val;
            }

            if (num == 13)
            {
                arr[13] = val;
            }

            if (num == 14)
            {
                arr[14] = val;
            }

            if (num == 15)
            {
                arr[15] = val;
            }

            if (num == 16)
            {
                arr[16] = val;
            }

            if (num == 17)
            {
                arr[17] = val;
            }

            if (num == 18)
            {
                arr[18] = val;
            }

            if (num == 19)
            {
                arr[19] = val;
            }

            if (num == 20)
            {
                arr[20] = val;
            }

            if (num == 21)
            {
                arr[21] = val;
            }

            if (num == 22)
            {
                arr[22] = val;
            }

            if (num == 23)
            {
                arr[23] = val;
            }

            if (num == 24)
            {
                arr[24] = val;
            }

            if (num == 25)
            {
                arr[25] = val;
            }

            if (num == 26)
            {
                arr[26] = val;
            }

            if (num == 27)
            {
                arr[27] = val;
            }

            if (num == 28)
            {
                arr[28] = val;
            }

            if (num == 29)
            {
                arr[29] = val;
            }

            if (num == 30)
            {
                arr[30] = val;
            }

            if (num == 31)
            {
                arr[31] = val;
            }

            if (num == 32)
            {
                arr[32] = val;
            }

            if (num == 33)
            {
                arr[33] = val;
            }

            if (num == 34)
            {
                arr[34] = val;
            }

            if (num == 35)
            {
                arr[35] = val;
            }

            if (num == 36)
            {
                arr[36] = val;
            }

            if (num == 37)
            {
                arr[37] = val;
            }

            if (num == 38)
            {
                arr[38] = val;
            }

            if (num == 39)
            {
                arr[39] = val;
            }


            if (num == 40)
            {
                arr[40] = val;
            }


            if (num == 41)
            {
                arr[41] = val;
            }


            if (num == 42)
            {
                arr[42] = val;
            }

            if (num == 43)
            {
                arr[43] = val;
            }

            if (num == 44)
            {
                arr[44] = val;
            }

            if (num == 45)
            {
                arr[45] = val;
            }

            if (num == 46)
            {
                arr[46] = val;
            }

            if (num == 47)
            {
                arr[47] = val;
            }

            if (num == 48)
            {
                arr[48] = val;
            }

            if (num == 49)
            {
                arr[49] = val;
            }

            if (num == 50)
            {
                arr[50] = val;
            }

            if (num == 51)
            {
                arr[51] = val;
            }

            if (num == 52)
            {
                arr[52] = val;
            }

            if (num == 53)
            {
                arr[53] = val;
            }

            if (num == 54)
            {
                arr[54] = val;
            }

            if (num == 55)
            {
                arr[55] = val;
            }

            if (num == 56)
            {
                arr[56] = val;
            }

            if (num == 57)
            {
                arr[57] = val;
            }

            if (num == 58)
            {
                arr[58] = val;
            }

            if (num == 59)
            {
                arr[59] = val;
            }

            if (num == 60)
            {
                arr[60] = val;
            }

            if (num == 61)
            {
                arr[61] = val;
            }

            if (num == 62)
            {
                arr[62] = val;
            }

            if (num == 63)
            {
                arr[63] = val;
            }

            if (num == 64)
            {
                arr[64] = val;
            }

            if (num == 65)
            {
                arr[65] = val;
            }

            if (num == 66)
            {
                arr[66] = val;
            }

            if (num == 67)
            {
                arr[67] = val;
            }

            if (num == 68)
            {
                arr[68] = val;
            }

            if (num == 69)
            {
                arr[69] = val;
            }

            if (num == 70)
            {
                arr[70] = val;
            }

            if (num == 71)
            {
                arr[71] = val;
            }

            if (num == 72)
            {
                arr[72] = val;
            }

            if (num == 73)
            {
                arr[73] = val;
            }

            if (num == 74)
            {
                arr[74] = val;
            }

            if (num == 75)
            {
                arr[75] = val;
            }

            if (num == 76)
            {
                arr[76] = val;
            }

            if (num == 77)
            {
                arr[77] = val;
            }

            if (num == 78)
            {
                arr[78] = val;
            }

            if (num == 79)
            {
                arr[79] = val;
            }

            if (num == 80)
            {
                arr[80] = val;
            }
        }

        static void MedianKernel1(MedianConfig conf)
        {
            var idx = Grid.IdxX * Group.DimX + Group.IdxX;
            var idy = Grid.IdxY * Group.DimY + Group.IdxY;
            int width = conf.input.Width;
            int height = conf.input.Height;

            if (idy >= height || idx >= width)
            {
                return;
            }

            LAB color_xy = conf.input.Get(idx, idy);

            const int RAD = 4;
            const int kernelSize = 2 * RAD + 1;
            const int size = kernelSize * kernelSize;

            ArrayView<float> arr = LocalMemory.Allocate<float>(size);
 
            for (int y = -RAD; y <= RAD; y++)
            {
                for (int x = -RAD; x <= RAD; x++)
                {

                    int idy_y = idxClip(idy + y, height);
                    int idx_x = idxClip(idx + x, width);
                    int y1 = y + RAD;
                    int x1 = x + RAD;
                    int num = y1 * kernelSize + x1;

                    LAB pix = conf.input.Get(idx_x, idy_y);
                    setVal(arr, num, pix.a);
                }
            }

            float a = median(arr);

            conf.output.Set(idx, idy, new LAB(color_xy.L, a, color_xy.b));
        }

        static void MedianKernel2(MedianConfig conf)
        {
            var idx = Grid.IdxX * Group.DimX + Group.IdxX;
            var idy = Grid.IdxY * Group.DimY + Group.IdxY;
            int width = conf.input.Width;
            int height = conf.input.Height;

            if (idy >= height || idx >= width)
            {
                return;
            }

            LAB color_xy = conf.input.Get(idx, idy);

            const int RAD = 4;
            const int kernelSize = 2 * RAD + 1;
            const int size = kernelSize * kernelSize;

            ArrayView<float> arr = LocalMemory.Allocate<float>(size);

            for (int y = -RAD; y <= RAD; y++)
            {
                for (int x = -RAD; x <= RAD; x++)
                {

                    int idy_y = idxClip(idy + y, height);
                    int idx_x = idxClip(idx + x, width);
                    int y1 = y + RAD;
                    int x1 = x + RAD;
                    int num = y1 * kernelSize + x1;

                    LAB pix = conf.input.Get(idx_x, idy_y);
                    setVal(arr, num, pix.b);
                }
            }

            float b = median(arr);

            conf.output.Set(idx, idy, new LAB(color_xy.L, color_xy.a, b));
        }
    }
}
