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
    /// Guided filter implementation
    /// </summary>
    public static class GuidedFilter
    {
        private static Action<AcceleratorStream, KernelConfig, ForkConfig> _forkKernel;
        private static Action<AcceleratorStream, KernelConfig, JoinConfig> _joinKernel;
        private static Action<AcceleratorStream, KernelConfig, BoxConfig> _boxKernel;
        private static Action<AcceleratorStream, KernelConfig, MulConfig> _mulKernel;
        private static Action<AcceleratorStream, KernelConfig, MinusConfig> _minusKernel;
        private static Action<AcceleratorStream, KernelConfig, Plus3MatsConfig> _plus3MatsKernel;
        private static Action<AcceleratorStream, KernelConfig, Plus4MatsConfig> _plus4MatsKernel;
        private static Action<AcceleratorStream, KernelConfig, DelConfig> _delKernel;

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

        public struct MulConfig
        {
            public readonly int width;
            public readonly int height;
  
            public readonly ArrayView2D<float, Stride2D.DenseX> input1;
            public readonly ArrayView2D<float, Stride2D.DenseX> input2;
            public readonly ArrayView2D<float, Stride2D.DenseX> output;

            public MulConfig(int width, int height, ArrayView2D<float, Stride2D.DenseX> input1, ArrayView2D<float, Stride2D.DenseX> input2, ArrayView2D<float, Stride2D.DenseX> output)
            {
                this.width = width;
                this.height = height;
                this.input1 = input1;
                this.input2 = input2;
                this.output = output;
             }
        }

        public struct MinusConfig
        {
            public readonly int width;
            public readonly int height;
            public readonly float eps;

            public readonly ArrayView2D<float, Stride2D.DenseX> input1;
            public readonly ArrayView2D<float, Stride2D.DenseX> input2;
            public readonly ArrayView2D<float, Stride2D.DenseX> output;

            public MinusConfig(int width, int height, ArrayView2D<float, Stride2D.DenseX> input1, ArrayView2D<float, Stride2D.DenseX> input2, ArrayView2D<float, Stride2D.DenseX> output, float eps = 0)
            {
                this.width = width;
                this.height = height;
                this.input1 = input1;
                this.input2 = input2;
                this.output = output;
                this.eps = eps;
            }
        }

        public struct DelConfig
        {
            public readonly int width;
            public readonly int height;
  
            public readonly ArrayView2D<float, Stride2D.DenseX> input1;
            public readonly ArrayView2D<float, Stride2D.DenseX> input2;
   
            public DelConfig(int width, int height, ArrayView2D<float, Stride2D.DenseX> input1, ArrayView2D<float, Stride2D.DenseX> input2)
            {
                this.width = width;
                this.height = height;
                this.input1 = input1;
                this.input2 = input2;
            }
        }


        public struct Plus3MatsConfig
        {
            public readonly int width;
            public readonly int height;
    
            public readonly ArrayView2D<float, Stride2D.DenseX> input1;
            public readonly ArrayView2D<float, Stride2D.DenseX> input2;
            public readonly ArrayView2D<float, Stride2D.DenseX> input3;
            public readonly ArrayView2D<float, Stride2D.DenseX> output;

            public Plus3MatsConfig(int width, int height, 
                ArrayView2D<float, Stride2D.DenseX> input1, 
                ArrayView2D<float, Stride2D.DenseX> input2,
                ArrayView2D<float, Stride2D.DenseX> input3,
                ArrayView2D<float, Stride2D.DenseX> output)
            {
                this.width = width;
                this.height = height;
                this.input1 = input1;
                this.input2 = input2;
                this.input3 = input3;
                this.output = output;
            }
        }

        public struct Plus4MatsConfig
        {
            public readonly int width;
            public readonly int height;

            public readonly ArrayView2D<float, Stride2D.DenseX> input1;
            public readonly ArrayView2D<float, Stride2D.DenseX> input2;
            public readonly ArrayView2D<float, Stride2D.DenseX> input3;
            public readonly ArrayView2D<float, Stride2D.DenseX> input4;
            public readonly ArrayView2D<float, Stride2D.DenseX> output;

            public Plus4MatsConfig(int width, int height,
                ArrayView2D<float, Stride2D.DenseX> input1,
                ArrayView2D<float, Stride2D.DenseX> input2,
                ArrayView2D<float, Stride2D.DenseX> input3,
                ArrayView2D<float, Stride2D.DenseX> input4,
                ArrayView2D<float, Stride2D.DenseX> output)
            {
                this.width = width;
                this.height = height;
                this.input1 = input1;
                this.input2 = input2;
                this.input3 = input3;
                this.input4 = input4;
                this.output = output;
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

        #region Load methods

        /// <summary>
        /// Preloads (compiles) all kernels
        /// </summary>
        /// <param name="device"></param>
        public static void WarmUp(Accelerator device)
        {
            _ = LoadForkKernel(device);
            _ = LoadJoinKernel(device);
            _ = LoadBoxKernel(device);
            _ = LoadMulKernel(device);
            _ = LoadMinusKernel(device);
            _ = LoadPlus3MatsKernel(device);
            _ = LoadPlus4MatsKernel(device);
            _ = LoadDelKernel(device);
        }

   
        static Action<AcceleratorStream, KernelConfig, BoxConfig> LoadBoxKernel(Accelerator device)
        {
            return LazyInitializer.EnsureInitialized(ref _boxKernel, () =>
            {
                return device.LoadKernel<BoxConfig>(BoxKernel);
            });
        }

        static Action<AcceleratorStream, KernelConfig, MulConfig> LoadMulKernel(Accelerator device)
        {
            return LazyInitializer.EnsureInitialized(ref _mulKernel, () =>
            {
                return device.LoadKernel<MulConfig>(MulKernel);
            });
        }

        static Action<AcceleratorStream, KernelConfig, MinusConfig> LoadMinusKernel(Accelerator device)
        {
            return LazyInitializer.EnsureInitialized(ref _minusKernel, () =>
            {
                return device.LoadKernel<MinusConfig>(MinusKernel);
            });
        }


        static Action<AcceleratorStream, KernelConfig, Plus3MatsConfig> LoadPlus3MatsKernel(Accelerator device)
        {
            return LazyInitializer.EnsureInitialized(ref _plus3MatsKernel, () =>
            {
                return device.LoadKernel<Plus3MatsConfig>(Plus3MatsKernel);
            });
        }

        static Action<AcceleratorStream, KernelConfig, Plus4MatsConfig> LoadPlus4MatsKernel(Accelerator device)
        {
            return LazyInitializer.EnsureInitialized(ref _plus4MatsKernel, () =>
            {
                return device.LoadKernel<Plus4MatsConfig>(Plus4MatsKernel);
            });
        }

        static Action<AcceleratorStream, KernelConfig, DelConfig> LoadDelKernel(Accelerator device)
        {
            return LazyInitializer.EnsureInitialized(ref _delKernel, () =>
            {
                return device.LoadKernel<DelConfig>(DelKernel);
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

        #endregion

        #region Records

        record RGBData(
          MemoryBuffer2D<float, Stride2D.DenseX> bufferR,
          MemoryBuffer2D<float, Stride2D.DenseX> bufferG,
          MemoryBuffer2D<float, Stride2D.DenseX> bufferB
        );

        record OutData(
           MemoryBuffer2D<float, Stride2D.DenseX> outR,
           MemoryBuffer2D<float, Stride2D.DenseX> outG,
           MemoryBuffer2D<float, Stride2D.DenseX> outB
         );

        record MeanData(
           MemoryBuffer2D<float, Stride2D.DenseX> mean_I_R,
           MemoryBuffer2D<float, Stride2D.DenseX> mean_I_G,
           MemoryBuffer2D<float, Stride2D.DenseX> mean_I_B
        );

        record InvData(
          MemoryBuffer2D<float, Stride2D.DenseX> invrr,
          MemoryBuffer2D<float, Stride2D.DenseX> invrg,
          MemoryBuffer2D<float, Stride2D.DenseX> invrb,
          MemoryBuffer2D<float, Stride2D.DenseX> invgg,
          MemoryBuffer2D<float, Stride2D.DenseX> invgb,
          MemoryBuffer2D<float, Stride2D.DenseX> invbb
          );

        record FilterChannelData(
            MemoryBuffer2D<float, Stride2D.DenseX> mean_p,
            MemoryBuffer2D<float, Stride2D.DenseX> mean_Ip_r,
            MemoryBuffer2D<float, Stride2D.DenseX> mean_Ip_g,
            MemoryBuffer2D<float, Stride2D.DenseX> mean_Ip_b,
            MemoryBuffer2D<float, Stride2D.DenseX> cov_Ip_r,
            MemoryBuffer2D<float, Stride2D.DenseX> cov_Ip_g,
            MemoryBuffer2D<float, Stride2D.DenseX> cov_Ip_b,
            MemoryBuffer2D<float, Stride2D.DenseX> a_r,
            MemoryBuffer2D<float, Stride2D.DenseX> a_g,
            MemoryBuffer2D<float, Stride2D.DenseX> a_b,
            MemoryBuffer2D<float, Stride2D.DenseX> mat_b
         );


        record TempData(
          MemoryBuffer2D<float, Stride2D.DenseX> temp1,
          MemoryBuffer2D<float, Stride2D.DenseX> temp2,
          MemoryBuffer2D<float, Stride2D.DenseX> temp3
          );

        #endregion

       /// <summary>
       /// Executes Guided filter
       /// </summary>
       /// <param name="device"></param>
       /// <param name="inputImage"></param>
       /// <param name="outputImage"></param>
       /// <param name="radius"></param>
       /// <param name="eps"></param>
       /// <returns></returns>
        public static async Task<GPUImage<RGBA32>> Execute(Accelerator device, GPUImage<RGBA32> inputImage, int radius, float eps)
        {
            eps = eps * eps;

            int resultWidth = (int)inputImage.Extent2D.X;
            int resultHeight = (int)inputImage.Extent2D.Y;
            int width = inputImage.Extent2D.X;
            int height = inputImage.Extent2D.Y;
            GPUImage<RGBA32> outputImage = new GPUImage<RGBA32>(resultWidth, resultHeight);

            int maxThreadsPerGroup = device.MaxNumThreadsPerGroup;
            int groupXDim = 16;
            int groupYDim = maxThreadsPerGroup / groupXDim;
            int gridXDim = divUp(inputImage.Extent2D.X, groupXDim);
            int gridYDim = divUp(inputImage.Extent2D.Y, groupYDim);
            KernelConfig kernelConfig = new KernelConfig(new Index2D(gridXDim, gridYDim), new Index2D(groupXDim, groupYDim));
     
            using AcceleratorStream stream = device.CreateStream();
       
            ArrayView2D<RGBA32, Stride2D.DenseX> input2DView = await inputImage.ToDevice2D(stream);

         
            using MemoryBuffer2D<float, Stride2D.DenseX> bufferR = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));
            using MemoryBuffer2D<float, Stride2D.DenseX> bufferG = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));
            using MemoryBuffer2D<float, Stride2D.DenseX> bufferB = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));

            using MemoryBuffer2D<float, Stride2D.DenseX> temp1 = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));
            using MemoryBuffer2D<float, Stride2D.DenseX> temp2 = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));
            using MemoryBuffer2D<float, Stride2D.DenseX> temp3 = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));

            using MemoryBuffer2D<float, Stride2D.DenseX> mean_I_R = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));
            using MemoryBuffer2D<float, Stride2D.DenseX> mean_I_G = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));
            using MemoryBuffer2D<float, Stride2D.DenseX> mean_I_B = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));

            using MemoryBuffer2D<float, Stride2D.DenseX> invrr = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));
            using MemoryBuffer2D<float, Stride2D.DenseX> invrg = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));
            using MemoryBuffer2D<float, Stride2D.DenseX> invrb = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));
            using MemoryBuffer2D<float, Stride2D.DenseX> invgg = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));
            using MemoryBuffer2D<float, Stride2D.DenseX> invgb = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));
            using MemoryBuffer2D<float, Stride2D.DenseX> invbb = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));

            using MemoryBuffer2D<float, Stride2D.DenseX> outR = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));
            using MemoryBuffer2D<float, Stride2D.DenseX> outG = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));
            using MemoryBuffer2D<float, Stride2D.DenseX> outB = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));

            ArrayView2D<RGBA32, Stride2D.DenseX> output2DView = await outputImage.ToDevice2D(stream);

            var forkKernel = LoadForkKernel(device);
            var joinKernel = LoadJoinKernel(device);

            var forkConf = new ForkConfig(inputImage.Extent2D.X, inputImage.Extent2D.Y, input2DView, bufferR.View, bufferG.View, bufferB.View);
            forkKernel(stream, kernelConfig, forkConf);
            device.Synchronize();

            RGBData rgbData = new RGBData(bufferR, bufferG, bufferB);
            MeanData meanData = new MeanData(mean_I_R, mean_I_G, mean_I_B);
            InvData invData = new InvData(invrr, invrg, invrb, invgg, invgb, invbb);
            TempData tempData = new TempData(temp1, temp2, temp3);

            InitStep(device, stream, kernelConfig, width, height, radius, eps, rgbData, meanData, invData, tempData);

            OutData outData = new OutData(outR, outG, outB);
            FilterChannels(device, stream, kernelConfig, width, height, radius, rgbData, meanData, invData, tempData, outData);
  
            var joinConf = new JoinConfig(inputImage.Extent2D.X, inputImage.Extent2D.Y, outR.View, outG.View, outB.View, output2DView);
            joinKernel(stream, kernelConfig, joinConf);
            device.Synchronize();

            return outputImage;
        }

      
        static void InitStep(
            Accelerator device,
            AcceleratorStream stream,
            KernelConfig kernelConfig,
            int width, 
            int height,
            int radius,
            float eps,
            RGBData rgbData,
            MeanData meanData,
            InvData invData,
            TempData tempData)
        {
            Action<AcceleratorStream, KernelConfig, BoxConfig> boxKernel = LoadBoxKernel(device);
            Action<AcceleratorStream, KernelConfig, MulConfig> mulKernel = LoadMulKernel(device);
            Action<AcceleratorStream, KernelConfig, MinusConfig> minusKernel = LoadMinusKernel(device);
            Action<AcceleratorStream, KernelConfig, Plus3MatsConfig> plus3MatsKernel = LoadPlus3MatsKernel(device);
            Action<AcceleratorStream, KernelConfig, DelConfig> delKernel = LoadDelKernel(device);

            using MemoryBuffer2D<float, Stride2D.DenseX> var_I_rr = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));
            using MemoryBuffer2D<float, Stride2D.DenseX> var_I_rg = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));
            using MemoryBuffer2D<float, Stride2D.DenseX> var_I_rb = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));
            using MemoryBuffer2D<float, Stride2D.DenseX> var_I_gg = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));
            using MemoryBuffer2D<float, Stride2D.DenseX> var_I_gb = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));
            using MemoryBuffer2D<float, Stride2D.DenseX> var_I_bb = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));

            var (bufferR, bufferG, bufferB) = rgbData;
            var (mean_I_R, mean_I_G, mean_I_B) = meanData;
            var (temp1, temp2, temp3) = tempData;

            #region mean calc

            {
                var confR = new BoxConfig(width, height, bufferR.View, mean_I_R.View, radius);
                var confG = new BoxConfig(width, height, bufferG.View, mean_I_G.View, radius);
                var confB = new BoxConfig(width, height, bufferB.View, mean_I_B.View, radius); ;
                boxKernel(stream, kernelConfig, confR);
                device.Synchronize();
                boxKernel(stream, kernelConfig, confG);
                device.Synchronize();
                boxKernel(stream, kernelConfig, confB);
                device.Synchronize();
            }


            /*
                // variance of I in each local patch: the matrix Sigma in Eqn (14).
                // Note the variance in each local patch is a 3x3 symmetric matrix:
                //           rr, rg, rb
                //   Sigma = rg, gg, gb
                //           rb, gb, bb
                using Mat var_I_rr = boxfilter(Ichannels[0].Mul(Ichannels[0]), r) - mean_I_r.Mul(mean_I_r) + eps;
                using Mat var_I_rg = boxfilter(Ichannels[0].Mul(Ichannels[1]), r) - mean_I_r.Mul(mean_I_g);
                using Mat var_I_rb = boxfilter(Ichannels[0].Mul(Ichannels[2]), r) - mean_I_r.Mul(mean_I_b);
                using Mat var_I_gg = boxfilter(Ichannels[1].Mul(Ichannels[1]), r) - mean_I_g.Mul(mean_I_g) + eps;
                using Mat var_I_gb = boxfilter(Ichannels[1].Mul(Ichannels[2]), r) - mean_I_g.Mul(mean_I_b);
                using Mat var_I_bb = boxfilter(Ichannels[2].Mul(Ichannels[2]), r) - mean_I_b.Mul(mean_I_b) + eps;
             */

            {
                //using Mat var_I_rr = boxfilter(Ichannels[0].Mul(Ichannels[0]), r) - mean_I_r.Mul(mean_I_r) + eps;
                var mulConf1 = new MulConfig(width, height, bufferR.View, bufferR.View, temp1.View);
                mulKernel(stream, kernelConfig, mulConf1);
                device.Synchronize();
                var confBox = new BoxConfig(width, height, temp1.View, temp3.View, radius);
                boxKernel(stream, kernelConfig, confBox);
                device.Synchronize();
                var mulConf2 = new MulConfig(width, height, mean_I_R.View, mean_I_R.View, temp2.View);
                mulKernel(stream, kernelConfig, mulConf2);
                device.Synchronize();
                var minusConf = new MinusConfig(width, height, temp3.View, temp2.View, var_I_rr.View, eps);
                minusKernel(stream, kernelConfig, minusConf);
                device.Synchronize();
            }

            {
                // using Mat var_I_rg = boxfilter(Ichannels[0].Mul(Ichannels[1]), r) - mean_I_r.Mul(mean_I_g);
                var mulConf1 = new MulConfig(width, height, bufferR.View, bufferG.View, temp1.View);
                mulKernel(stream, kernelConfig, mulConf1);
                device.Synchronize();
                var confBox = new BoxConfig(width, height, temp1.View, temp3.View, radius);
                boxKernel(stream, kernelConfig, confBox);
                device.Synchronize();
                var mulConf2 = new MulConfig(width, height, mean_I_R.View, mean_I_G.View, temp2.View);
                mulKernel(stream, kernelConfig, mulConf2);
                device.Synchronize();
                var minusConf = new MinusConfig(width, height, temp3.View, temp2.View, var_I_rg.View, 0);
                minusKernel(stream, kernelConfig, minusConf);
                device.Synchronize();
            }

            {
                //using Mat var_I_rb = boxfilter(Ichannels[0].Mul(Ichannels[2]), r) - mean_I_r.Mul(mean_I_b);
                var mulConf1 = new MulConfig(width, height, bufferR.View, bufferB.View, temp1.View);
                mulKernel(stream, kernelConfig, mulConf1);
                device.Synchronize();
                var confBox = new BoxConfig(width, height, temp1.View, temp3.View, radius);
                boxKernel(stream, kernelConfig, confBox);
                device.Synchronize();
                var mulConf2 = new MulConfig(width, height, mean_I_R.View, mean_I_B.View, temp2.View);
                mulKernel(stream, kernelConfig, mulConf2);
                device.Synchronize();
                var minusConf = new MinusConfig(width, height, temp3.View, temp2.View, var_I_rb.View, 0);
                minusKernel(stream, kernelConfig, minusConf);
                device.Synchronize();
            }

            {
                //using Mat var_I_gg = boxfilter(Ichannels[1].Mul(Ichannels[1]), r) - mean_I_g.Mul(mean_I_g) + eps;
                var mulConf1 = new MulConfig(width, height, bufferG.View, bufferG.View, temp1.View);
                mulKernel(stream, kernelConfig, mulConf1);
                device.Synchronize();
                var confBox = new BoxConfig(width, height, temp1.View, temp3.View, radius);
                boxKernel(stream, kernelConfig, confBox);
                device.Synchronize();
                var mulConf2 = new MulConfig(width, height, mean_I_G.View, mean_I_G.View, temp2.View);
                mulKernel(stream, kernelConfig, mulConf2);
                device.Synchronize();
                var minusConf = new MinusConfig(width, height, temp3.View, temp2.View, var_I_gg.View, eps);
                minusKernel(stream, kernelConfig, minusConf);
                device.Synchronize();
            }

            {
                //using Mat var_I_gb = boxfilter(Ichannels[1].Mul(Ichannels[2]), r) - mean_I_g.Mul(mean_I_b);
                var mulConf1 = new MulConfig(width, height, bufferG.View, bufferB.View, temp1.View);
                mulKernel(stream, kernelConfig, mulConf1);
                device.Synchronize();
                var confBox = new BoxConfig(width, height, temp1.View, temp3.View, radius);
                boxKernel(stream, kernelConfig, confBox);
                device.Synchronize();
                var mulConf2 = new MulConfig(width, height, mean_I_G.View, mean_I_B.View, temp2.View);
                mulKernel(stream, kernelConfig, mulConf2);
                device.Synchronize();
                var minusConf = new MinusConfig(width, height, temp3.View, temp2.View, var_I_gb.View, 0);
                minusKernel(stream, kernelConfig, minusConf);
                device.Synchronize();
            }

            {
                //using Mat var_I_bb = boxfilter(Ichannels[2].Mul(Ichannels[2]), r) - mean_I_b.Mul(mean_I_b) + eps;
                var mulConf1 = new MulConfig(width, height, bufferB.View, bufferB.View, temp1.View);
                mulKernel(stream, kernelConfig, mulConf1);
                device.Synchronize();
                var confBox = new BoxConfig(width, height, temp1.View, temp3.View, radius);
                boxKernel(stream, kernelConfig, confBox);
                device.Synchronize();
                var mulConf2 = new MulConfig(width, height, mean_I_B.View, mean_I_B.View, temp2.View);
                mulKernel(stream, kernelConfig, mulConf2);
                device.Synchronize();
                var minusConf = new MinusConfig(width, height, temp3.View, temp2.View, var_I_gb.View, eps);
                minusKernel(stream, kernelConfig, minusConf);
                device.Synchronize();
            }

            #endregion

            var (invrr, invrg, invrb, invgg, invgb, invbb) = invData;

            #region inverse calc

            /*
              // Inverse of Sigma + eps * I
              invrr = var_I_gg.Mul(var_I_bb) - var_I_gb.Mul(var_I_gb);
              invrg = var_I_gb.Mul(var_I_rb) - var_I_rg.Mul(var_I_bb);
              invrb = var_I_rg.Mul(var_I_gb) - var_I_gg.Mul(var_I_rb);
              invgg = var_I_rr.Mul(var_I_bb) - var_I_rb.Mul(var_I_rb);
              invgb = var_I_rb.Mul(var_I_rg) - var_I_rr.Mul(var_I_gb);
              invbb = var_I_rr.Mul(var_I_gg) - var_I_rg.Mul(var_I_rg);
            */

            {
                // invrr = var_I_gg.Mul(var_I_bb) - var_I_gb.Mul(var_I_gb);
                var mulConf1 = new MulConfig(width, height, var_I_gg.View, var_I_bb.View, temp1.View);
                mulKernel(stream, kernelConfig, mulConf1);
                device.Synchronize();
           
                var mulConf2 = new MulConfig(width, height, var_I_gb.View, var_I_gb.View, temp2.View);
                mulKernel(stream, kernelConfig, mulConf2);
                device.Synchronize();

                var minusConf = new MinusConfig(width, height, temp1.View, temp2.View, invrr.View, 0);
                minusKernel(stream, kernelConfig, minusConf);
                device.Synchronize();
            }

            {
                // invrg = var_I_gb.Mul(var_I_rb) - var_I_rg.Mul(var_I_bb);
                var mulConf1 = new MulConfig(width, height, var_I_gb.View, var_I_rb.View, temp1.View);
                mulKernel(stream, kernelConfig, mulConf1);
                device.Synchronize();

                var mulConf2 = new MulConfig(width, height, var_I_rg.View, var_I_bb.View, temp2.View);
                mulKernel(stream, kernelConfig, mulConf2);
                device.Synchronize();

                var minusConf = new MinusConfig(width, height, temp1.View, temp2.View, invrg.View, 0);
                minusKernel(stream, kernelConfig, minusConf);
                device.Synchronize();
            }

            {
                // invrb = var_I_rg.Mul(var_I_gb) - var_I_gg.Mul(var_I_rb);
                var mulConf1 = new MulConfig(width, height, var_I_rg.View, var_I_gb.View, temp1.View);
                mulKernel(stream, kernelConfig, mulConf1);
                device.Synchronize();

                var mulConf2 = new MulConfig(width, height, var_I_gg.View, var_I_rb.View, temp2.View);
                mulKernel(stream, kernelConfig, mulConf2);
                device.Synchronize();

                var minusConf = new MinusConfig(width, height, temp1.View, temp2.View, invrb.View, 0);
                minusKernel(stream, kernelConfig, minusConf);
                device.Synchronize();
            }

            {
                // invgg = var_I_rr.Mul(var_I_bb) - var_I_rb.Mul(var_I_rb);
                var mulConf1 = new MulConfig(width, height, var_I_rr.View, var_I_bb.View, temp1.View);
                mulKernel(stream, kernelConfig, mulConf1);
                device.Synchronize();

                var mulConf2 = new MulConfig(width, height, var_I_rb.View, var_I_rb.View, temp2.View);
                mulKernel(stream, kernelConfig, mulConf2);
                device.Synchronize();

                var minusConf = new MinusConfig(width, height, temp1.View, temp2.View, invgg.View, 0);
                minusKernel(stream, kernelConfig, minusConf);
                device.Synchronize();
            }

            {
                // invgb = var_I_rb.Mul(var_I_rg) - var_I_rr.Mul(var_I_gb);
                var mulConf1 = new MulConfig(width, height, var_I_rb.View, var_I_rg.View, temp1.View);
                mulKernel(stream, kernelConfig, mulConf1);
                device.Synchronize();

                var mulConf2 = new MulConfig(width, height, var_I_rr.View, var_I_gb.View, temp2.View);
                mulKernel(stream, kernelConfig, mulConf2);
                device.Synchronize();

                var minusConf = new MinusConfig(width, height, temp1.View, temp2.View, invgb.View, 0);
                minusKernel(stream, kernelConfig, minusConf);
                device.Synchronize();
            }

            {
                // invbb = var_I_rr.Mul(var_I_gg) - var_I_rg.Mul(var_I_rg);
                var mulConf1 = new MulConfig(width, height, var_I_rr.View, var_I_gg.View, temp1.View);
                mulKernel(stream, kernelConfig, mulConf1);
                device.Synchronize();

                var mulConf2 = new MulConfig(width, height, var_I_rg.View, var_I_rg.View, temp2.View);
                mulKernel(stream, kernelConfig, mulConf2);
                device.Synchronize();

                var minusConf = new MinusConfig(width, height, temp1.View, temp2.View, invbb.View, 0);
                minusKernel(stream, kernelConfig, minusConf);
                device.Synchronize();
            }

            #endregion

            #region CovDet

            using MemoryBuffer2D<float, Stride2D.DenseX> covDet = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));
            // using Mat covDet = invrr.Mul(var_I_rr) + invrg.Mul(var_I_rg) + invrb.Mul(var_I_rb);

            {
                var mulConf1 = new MulConfig(width, height, invrr.View, var_I_rr.View, temp1.View);
                mulKernel(stream, kernelConfig, mulConf1);
                device.Synchronize();

                var mulConf2 = new MulConfig(width, height, invrg.View, var_I_rg.View, temp2.View);
                mulKernel(stream, kernelConfig, mulConf2);
                device.Synchronize();

                var mulConf3 = new MulConfig(width, height, invrb.View, var_I_rb.View, temp3.View);
                mulKernel(stream, kernelConfig, mulConf3);
                device.Synchronize();

                var plusConf = new Plus3MatsConfig(width, height, temp1.View, temp2.View, temp3.View, covDet.View);
                plus3MatsKernel(stream, kernelConfig, plusConf);
                device.Synchronize();
            }

            #endregion

            /*
                invrr /= covDet;
                invrg /= covDet;
                invrb /= covDet;
                invgg /= covDet;
                invgb /= covDet;
                invbb /= covDet;
             */
            #region Del by covDet
            {
                var delConf1 = new DelConfig(width, height, invrr.View, covDet.View);
                delKernel(stream, kernelConfig, delConf1);
                device.Synchronize();
            }
            {
                var delConf1 = new DelConfig(width, height, invrg.View, covDet.View);
                delKernel(stream, kernelConfig, delConf1);
                device.Synchronize();
            }
            {
                var delConf1 = new DelConfig(width, height, invrb.View, covDet.View);
                delKernel(stream, kernelConfig, delConf1);
                device.Synchronize();
            }
            {
                var delConf1 = new DelConfig(width, height, invgg.View, covDet.View);
                delKernel(stream, kernelConfig, delConf1);
                device.Synchronize();
            }
            {
                var delConf1 = new DelConfig(width, height, invgb.View, covDet.View);
                delKernel(stream, kernelConfig, delConf1);
                device.Synchronize();
            }
            {
                var delConf1 = new DelConfig(width, height, invbb.View, covDet.View);
                delKernel(stream, kernelConfig, delConf1);
                device.Synchronize();
            }
            #endregion
        }

        static void FilterChannels(
          Accelerator device,
          AcceleratorStream stream,
          KernelConfig kernelConfig,
          int width,
          int height,
          int radius,
          RGBData rgbData,
          MeanData meanData,
          InvData invData,
          TempData tempData,
          OutData outData)
        {
            var (outR, outG, outB) = outData;

            using MemoryBuffer2D<float, Stride2D.DenseX> mean_p = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));

            using MemoryBuffer2D<float, Stride2D.DenseX> mean_Ip_r = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));
            using MemoryBuffer2D<float, Stride2D.DenseX> mean_Ip_g = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));
            using MemoryBuffer2D<float, Stride2D.DenseX> mean_Ip_b = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));

            using MemoryBuffer2D<float, Stride2D.DenseX> cov_Ip_r = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));
            using MemoryBuffer2D<float, Stride2D.DenseX> cov_Ip_g = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));
            using MemoryBuffer2D<float, Stride2D.DenseX> cov_Ip_b = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));

            using MemoryBuffer2D<float, Stride2D.DenseX> a_r = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));
            using MemoryBuffer2D<float, Stride2D.DenseX> a_g = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));
            using MemoryBuffer2D<float, Stride2D.DenseX> a_b = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));

            using MemoryBuffer2D<float, Stride2D.DenseX> mat_b = device.Allocate2DDenseX<float>(new LongIndex2D(width, height));

            // reusing allocated memory for each channel
            FilterChannelData channelData = new FilterChannelData(
               mean_p,
               mean_Ip_r,
               mean_Ip_g,
               mean_Ip_b,
               cov_Ip_r,
               cov_Ip_g,
               cov_Ip_b,
               a_r,
               a_g,
               a_b,
               mat_b
            );

            FilterSingleChannel(device, stream, kernelConfig, width, height, radius, rgbData, rgbData.bufferR, meanData, invData, tempData, channelData, outR);
            FilterSingleChannel(device, stream, kernelConfig, width, height, radius, rgbData, rgbData.bufferG, meanData, invData, tempData, channelData, outG);
            FilterSingleChannel(device, stream, kernelConfig, width, height, radius, rgbData, rgbData.bufferB, meanData, invData, tempData, channelData, outB);
        }


        static void FilterSingleChannel(
            Accelerator device,
            AcceleratorStream stream,
            KernelConfig kernelConfig,
            int width,
            int height,
            int radius,
            RGBData rgbData,
            MemoryBuffer2D<float, Stride2D.DenseX> p,
            MeanData meanData,
            InvData invData,
            TempData tempData,
            FilterChannelData channelData,
            MemoryBuffer2D<float, Stride2D.DenseX> outBuffer)
        {
            var (bufferR, bufferG, bufferB) = rgbData;
            var (mean_I_R, mean_I_G, mean_I_B) = meanData;
            var (invrr, invrg, invrb, invgg, invgb, invbb) = invData;
            var (temp1, temp2, temp3) = tempData;
            var (
               mean_p,
               mean_Ip_r,
               mean_Ip_g,
               mean_Ip_b,
               cov_Ip_r,
               cov_Ip_g,
               cov_Ip_b,
               a_r,
               a_g,
               a_b,
               mat_b
            ) = channelData;

            Action<AcceleratorStream, KernelConfig, BoxConfig> boxKernel = LoadBoxKernel(device);
            Action<AcceleratorStream, KernelConfig, MulConfig> mulKernel = LoadMulKernel(device);
            Action<AcceleratorStream, KernelConfig, MinusConfig> minusKernel = LoadMinusKernel(device);
            Action<AcceleratorStream, KernelConfig, Plus3MatsConfig> plus3MatsKernel = LoadPlus3MatsKernel(device);
            Action<AcceleratorStream, KernelConfig, Plus4MatsConfig> plus4MatsKernel = LoadPlus4MatsKernel(device);

            // mean_p
            {
                var conf = new BoxConfig(width, height, p.View, mean_p.View, radius);
                boxKernel(stream, kernelConfig, conf);
                device.Synchronize();
            }

            /*
              using Mat mean_Ip_r = boxfilter(Ichannels[0].Mul(p), r);
              using Mat mean_Ip_g = boxfilter(Ichannels[1].Mul(p), r);
              using Mat mean_Ip_b = boxfilter(Ichannels[2].Mul(p), r);
            */
            #region mean_Ip calc

            {
                var mulConf1 = new MulConfig(width, height, bufferR.View, p.View, temp1.View);
                mulKernel(stream, kernelConfig, mulConf1);
                device.Synchronize();
                var confBox = new BoxConfig(width, height, temp1.View, mean_Ip_r.View, radius);
                boxKernel(stream, kernelConfig, confBox);
                device.Synchronize();
            }
            {
                var mulConf1 = new MulConfig(width, height, bufferG.View, p.View, temp1.View);
                mulKernel(stream, kernelConfig, mulConf1);
                device.Synchronize();
                var confBox = new BoxConfig(width, height, temp1.View, mean_Ip_g.View, radius);
                boxKernel(stream, kernelConfig, confBox);
                device.Synchronize();
            }
            {
                var mulConf1 = new MulConfig(width, height, bufferB.View, p.View, temp1.View);
                mulKernel(stream, kernelConfig, mulConf1);
                device.Synchronize();
                var confBox = new BoxConfig(width, height, temp1.View, mean_Ip_b.View, radius);
                boxKernel(stream, kernelConfig, confBox);
                device.Synchronize();
            }

            #endregion

            /*
               // covariance of (I, p) in each local patch.
                using Mat cov_Ip_r = mean_Ip_r - mean_I_r.Mul(mean_p);
                using Mat cov_Ip_g = mean_Ip_g - mean_I_g.Mul(mean_p);
                using Mat cov_Ip_b = mean_Ip_b - mean_I_b.Mul(mean_p);
            */

            #region cov Ip

            {
                var mulConf1 = new MulConfig(width, height, mean_I_R.View, mean_p.View, temp1.View);
                mulKernel(stream, kernelConfig, mulConf1);
                device.Synchronize();
                var confBox = new MinusConfig(width, height, mean_Ip_r, temp1.View, cov_Ip_r.View);
                minusKernel(stream, kernelConfig, confBox);
                device.Synchronize();
            }

            {
                var mulConf1 = new MulConfig(width, height, mean_I_G.View, mean_p.View, temp1.View);
                mulKernel(stream, kernelConfig, mulConf1);
                device.Synchronize();
                var confBox = new MinusConfig(width, height, mean_Ip_g, temp1.View, cov_Ip_g.View);
                minusKernel(stream, kernelConfig, confBox);
                device.Synchronize();
            }

            {
                var mulConf1 = new MulConfig(width, height, mean_I_B.View, mean_p.View, temp1.View);
                mulKernel(stream, kernelConfig, mulConf1);
                device.Synchronize();
                var confBox = new MinusConfig(width, height, mean_Ip_b, temp1.View, cov_Ip_b.View);
                minusKernel(stream, kernelConfig, confBox);
                device.Synchronize();
            }

            #endregion

            /*
               using Mat a_r = invrr.Mul(cov_Ip_r) + invrg.Mul(cov_Ip_g) + invrb.Mul(cov_Ip_b);
               using Mat a_g = invrg.Mul(cov_Ip_r) + invgg.Mul(cov_Ip_g) + invgb.Mul(cov_Ip_b);
               using Mat a_b = invrb.Mul(cov_Ip_r) + invgb.Mul(cov_Ip_g) + invbb.Mul(cov_Ip_b);
            */

            #region a calc

            {
                var mulConf1 = new MulConfig(width, height, invrr.View, cov_Ip_r.View, temp1.View);
                mulKernel(stream, kernelConfig, mulConf1);
                device.Synchronize();

                var mulConf2 = new MulConfig(width, height, invrg.View, cov_Ip_g.View, temp2.View);
                mulKernel(stream, kernelConfig, mulConf2);
                device.Synchronize();

                var mulConf3 = new MulConfig(width, height, invrb.View, cov_Ip_b.View, temp3.View);
                mulKernel(stream, kernelConfig, mulConf3);
                device.Synchronize();

                var plusConf = new Plus3MatsConfig(width, height, temp1.View, temp2.View, temp3.View, a_r.View);
                plus3MatsKernel(stream, kernelConfig, plusConf);
                device.Synchronize();
            }

            {
                var mulConf1 = new MulConfig(width, height, invrg.View, cov_Ip_r.View, temp1.View);
                mulKernel(stream, kernelConfig, mulConf1);
                device.Synchronize();

                var mulConf2 = new MulConfig(width, height, invgg.View, cov_Ip_g.View, temp2.View);
                mulKernel(stream, kernelConfig, mulConf2);
                device.Synchronize();

                var mulConf3 = new MulConfig(width, height, invgb.View, cov_Ip_b.View, temp3.View);
                mulKernel(stream, kernelConfig, mulConf3);
                device.Synchronize();

                var plusConf = new Plus3MatsConfig(width, height, temp1.View, temp2.View, temp3.View, a_g.View);
                plus3MatsKernel(stream, kernelConfig, plusConf);
                device.Synchronize();
            }

            {
                var mulConf1 = new MulConfig(width, height, invrb.View, cov_Ip_r.View, temp1.View);
                mulKernel(stream, kernelConfig, mulConf1);
                device.Synchronize();

                var mulConf2 = new MulConfig(width, height, invgb.View, cov_Ip_g.View, temp2.View);
                mulKernel(stream, kernelConfig, mulConf2);
                device.Synchronize();

                var mulConf3 = new MulConfig(width, height, invbb.View, cov_Ip_b.View, temp3.View);
                mulKernel(stream, kernelConfig, mulConf3);
                device.Synchronize();

                var plusConf = new Plus3MatsConfig(width, height, temp1.View, temp2.View, temp3.View, a_b.View);
                plus3MatsKernel(stream, kernelConfig, plusConf);
                device.Synchronize();
            }

            #endregion

            //  using Mat b = mean_p - a_r.Mul(mean_I_r) - a_g.Mul(mean_I_g) - a_b.Mul(mean_I_b); // Eqn. (15) in the paper;
            #region b calc

            {
                var mulConf1 = new MulConfig(width, height, a_r.View, mean_I_R.View, temp1.View);
                mulKernel(stream, kernelConfig, mulConf1);
                device.Synchronize();

                var mulConf2 = new MulConfig(width, height, a_g.View, mean_I_G.View, temp2.View);
                mulKernel(stream, kernelConfig, mulConf2);
                device.Synchronize();

                var mulConf3 = new MulConfig(width, height, a_b.View, mean_I_B.View, temp3.View);
                mulKernel(stream, kernelConfig, mulConf3);
                device.Synchronize();
            }

            {

                var minusConf = new MinusConfig(width, height, mean_p.View, temp1.View, mat_b.View);
                minusKernel(stream, kernelConfig, minusConf);
                device.Synchronize();

            }

            {
                var minusConf = new MinusConfig(width, height, mat_b.View, temp2.View, mat_b.View);
                minusKernel(stream, kernelConfig, minusConf);
                device.Synchronize();
            }

            {
                var minusConf = new MinusConfig(width, height, mat_b.View, temp3.View, mat_b.View);
                minusKernel(stream, kernelConfig, minusConf);
                device.Synchronize();
            }

            #endregion

            /*
                 return (boxfilter(a_r, r).Mul(Ichannels[0])
                      + boxfilter(a_g, r).Mul(Ichannels[1])
                      + boxfilter(a_b, r).Mul(Ichannels[2])
                      + boxfilter(b, r));  // Eqn. (16) in the paper;
            */

            #region result calc

            {
                var confBox = new BoxConfig(width, height, a_r.View, temp1.View, radius);
                boxKernel(stream, kernelConfig, confBox);
                device.Synchronize();

                var mulConf1 = new MulConfig(width, height, temp1.View, bufferR.View, a_r.View);
                mulKernel(stream, kernelConfig, mulConf1);
                device.Synchronize();
            }

            {
                var confBox = new BoxConfig(width, height, a_g.View, temp1.View, radius);
                boxKernel(stream, kernelConfig, confBox);
                device.Synchronize();

                var mulConf1 = new MulConfig(width, height, temp1.View, bufferG.View, a_g.View);
                mulKernel(stream, kernelConfig, mulConf1);
                device.Synchronize();
            }

            {
                var confBox = new BoxConfig(width, height, a_b.View, temp1.View, radius);
                boxKernel(stream, kernelConfig, confBox);
                device.Synchronize();

                var mulConf1 = new MulConfig(width, height, temp1.View, bufferB.View, a_b.View);
                mulKernel(stream, kernelConfig, mulConf1);
                device.Synchronize();
            }

            {
                var confBox = new BoxConfig(width, height, mat_b.View, temp1.View, radius);
                boxKernel(stream, kernelConfig, confBox);
                device.Synchronize();
            }

            {
                var confBox = new Plus4MatsConfig(width, height, a_r.View, a_g.View, a_b.View, temp1.View, outBuffer.View);
                plus4MatsKernel(stream, kernelConfig, confBox);
                device.Synchronize();
            }
            #endregion
        }

        #region Kernels

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
            float scale = 1f / (float)(kernelSize * kernelSize);

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

        static void MulKernel(MulConfig conf)
        {
            var idx = Grid.IdxX * Group.DimX + Group.IdxX;
            var idy = Grid.IdxY * Group.DimY + Group.IdxY;

            if (idy >= conf.height || idx >= conf.width)
            {
                return;
            }

            conf.output[idx, idy] = conf.input1[idx, idy] * conf.input2[idx, idy];
        }

        static void MinusKernel(MinusConfig conf)
        {
            var idx = Grid.IdxX * Group.DimX + Group.IdxX;
            var idy = Grid.IdxY * Group.DimY + Group.IdxY;

            if (idy >= conf.height || idx >= conf.width)
            {
                return;
            }

            conf.output[idx, idy] = conf.input1[idx, idy] - conf.input2[idx, idy] + conf.eps;
        }

        static void DelKernel(DelConfig conf)
        {
            var idx = Grid.IdxX * Group.DimX + Group.IdxX;
            var idy = Grid.IdxY * Group.DimY + Group.IdxY;

            if (idy >= conf.height || idx >= conf.width)
            {
                return;
            }

            conf.input1[idx, idy] = conf.input1[idx, idy] / conf.input2[idx, idy];
        }


        static void Plus3MatsKernel(Plus3MatsConfig conf)
        {
            var idx = Grid.IdxX * Group.DimX + Group.IdxX;
            var idy = Grid.IdxY * Group.DimY + Group.IdxY;

            if (idy >= conf.height || idx >= conf.width)
            {
                return;
            }

            conf.output[idx, idy] = conf.input1[idx, idy] + conf.input2[idx, idy] + conf.input3[idx, idy];
        }

        static void Plus4MatsKernel(Plus4MatsConfig conf)
        {
            var idx = Grid.IdxX * Group.DimX + Group.IdxX;
            var idy = Grid.IdxY * Group.DimY + Group.IdxY;

            if (idy >= conf.height || idx >= conf.width)
            {
                return;
            }

            conf.output[idx, idy] = conf.input1[idx, idy] + conf.input2[idx, idy] + conf.input3[idx, idy] + conf.input4[idx, idy];
        }

        #endregion
    }
}
