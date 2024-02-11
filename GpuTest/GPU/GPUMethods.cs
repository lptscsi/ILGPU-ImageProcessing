using GpuTest.GPU.Core;
using GpuTest.Utils.Color;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.OpenCL;
using ImageMagick;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace GpuTest.GPU
{
    public class GPUMethods : IDisposable
    {
        public Context context;
        public Accelerator device;

        public GPUMethods()
        {
            context = Context.Create(builder => builder.
                                                      OpenCL().
                                                      Release().
                                                      StaticFields(StaticFieldMode.IgnoreStaticFieldStores).
                                                      EnableAlgorithms().
                                                      Math(MathMode.Fast32BitOnly).
                                                      Inlining(InliningMode.Aggressive).
                                                      Caching(CachingMode.Default));
            //device = context.CreateCPUAccelerator(0, CPUAcceleratorMode.Parallel);

            device = context.CreateCLAccelerator(0);
            Console.WriteLine(device.ToString());
        }

        public async Task TestMiscAlgorithms(string imagePath, string resName)
        {
            string dir = Path.GetDirectoryName(imagePath);
            string resPath = Path.Combine(dir, resName);

            byte[] rgbBytes;
            int width;
            int height;
     
            using (MagickImage image = new MagickImage(imagePath))
            {
                width = image.Width;
                height = image.Height;
                image.Depth = 8;

                Stopwatch sw1 = Stopwatch.StartNew();
                ChunkedMemoryStream chunkedMemoryStream = new ChunkedMemoryStream();
                image.Write(chunkedMemoryStream, MagickFormat.Rgba);
                rgbBytes = chunkedMemoryStream.ToArray();
                //rgbBytes = image.ToByteArray(MagickFormat.Rgba);
                sw1.Stop();
                Console.WriteLine($"MagickImage ToByte Time: {sw1.ElapsedMilliseconds}");
            }

            using GPUImage<RGBA32> inputImage = new GPUImage<RGBA32>(width, height, rgbBytes);
              
            Stopwatch sw = Stopwatch.StartNew();

            //Bilateral.WarmUp(device);
            //Median1.WarmUp(device);
            //Median2.WarmUp(device);
            //MedianAdaptive.WarmUp(device);
            //GuidedFilter.WarmUp(device);
            //ConvolutionFilter.WarmUp(device);
            //KNN.WarmUp(device);
            //Scale.WarmUp(device);
            MedianLAB.WarmUp(device);

            sw.Stop();
            Console.WriteLine($"LoadKernel Time: {sw.ElapsedMilliseconds}");

            sw.Restart();

            //using GPUImage<RGBA32> outputImage = await Median1.Execute(device, inputImage);
            //using GPUImage<RGBA32> outputImage = await Median2.Execute(device, inputImage);
            //using GPUImage<RGBA32> outputImage = await GuidedFilter.Execute(device, inputImage, radius: 2, eps: 0.1f);
            //using GPUImage<RGBA32> outputImage = await MedianAdaptive.Execute(device, inputImage);
            //using GPUImage<RGBA32> outputImage = await Bilateral.Execute1(device, inputImage, 5, 0.25f, 2f);
            //using GPUImage<RGBA32> outputImage = await KNN.Execute(device, inputImage, 3);
            //using GPUImage<RGBA32> outputImage = await Scale.ExecuteDownScale(device, outputImage, downScaleFactor: 8.563f);
            //using GPUImage<RGBA32> outputImage = await Scale.ExecuteTex2D(device, inputImage, scaleFactor: 1.55f);
            //using GPUImage<RGBA32> outputImage = await Scale.ExecuteResample(device, inputImage, scaleFactor: 14.55f);
            //using GPUImage<RGBA32> outputImage = await ConvolutionFilter.Execute(device, FilterType.GaussianBlur, inputImage, 11, 2);
            using GPUImage<RGBA32> outputImage = await MedianLAB.Execute(device, inputImage);

            sw.Stop();
            Console.WriteLine($"Kernel Execution Time: {sw.ElapsedMilliseconds}");
            
            GPUImage<RGBA32> result = outputImage;

            byte[] bytes = await result.ToCPU(device.DefaultStream);
  
            using (MagickImage resImg = new MagickImage(bytes, new PixelReadSettings(result.Width, result.Height, StorageType.Char, PixelMapping.RGBA)))
            {
                resImg.Write(resPath, MagickFormat.Jpeg);
            }
        }

        public async Task TestHistogram(string imagePath)
        {
            byte[] rgbBytes;
            int width;
            int height;

            using (MagickImage image = new MagickImage(imagePath))
            {
                width = image.Width;
                height = image.Height;
                image.Depth = 8;

                Stopwatch sw1 = Stopwatch.StartNew();
                ChunkedMemoryStream chunkedMemoryStream = new ChunkedMemoryStream();
                image.Write(chunkedMemoryStream, MagickFormat.Rgba);
                rgbBytes = chunkedMemoryStream.ToArray();
                //rgbBytes = image.ToByteArray(MagickFormat.Rgba);
                sw1.Stop();
                Console.WriteLine($"MagickImage ToByte Time: {sw1.ElapsedMilliseconds}");
            }

            using GPUImage<RGBA32> inputImage = new GPUImage<RGBA32>(width, height, rgbBytes);

            Stopwatch sw = Stopwatch.StartNew();

            Histogram.WarmUp(device);

            sw.Stop();
            Console.WriteLine($"LoadKernel Time: {sw.ElapsedMilliseconds}");

            sw.Restart();
     
            int[] hist = await Histogram.Execute(device, inputImage);
            sw.Stop();
            Console.WriteLine($"Kernel Execution Time: {sw.ElapsedMilliseconds}");
        }

        public async Task TestStreamProcessing(string imagePath, string resName)
        {
            string dir = Path.GetDirectoryName(imagePath);
            string resPath = Path.Combine(dir, resName);

            byte[] rgbBytes;
            int width;
            int height;


            using (MagickImage image = new MagickImage(imagePath))
            {
                width = image.Width;
                height = image.Height;
                image.Depth = 8;

                Stopwatch sw1 = Stopwatch.StartNew();
                ChunkedMemoryStream chunkedMemoryStream = new ChunkedMemoryStream();
                image.Write(chunkedMemoryStream, MagickFormat.Rgba);
                rgbBytes = chunkedMemoryStream.ToArray();
                //rgbBytes = image.ToByteArray(MagickFormat.Rgba);
                sw1.Stop();
                Console.WriteLine($"MagickImage ToByte Time: {sw1.ElapsedMilliseconds}");
            }

            Stopwatch sw = Stopwatch.StartNew();

            StreamsTest.WarmUp(device);
 
            sw.Stop();
            Console.WriteLine($"LoadKernel Time: {sw.ElapsedMilliseconds}");

            using GPUImage<RGBA32> outputImage = await StreamsTest.Execute(device, width, height, rgbBytes);

            GPUImage<RGBA32> result = outputImage;

            sw.Restart();

            byte[] bytes = await result.ToCPU(device.DefaultStream);

            sw.Stop();
            Console.WriteLine($"ToCPU Time: {sw.ElapsedMilliseconds}");

            using (MagickImage resImg = new MagickImage(bytes, new PixelReadSettings(result.Width, result.Height, StorageType.Char, PixelMapping.RGBA)))
            {
                resImg.Write(resPath, MagickFormat.Jpeg);
            }
        }

        public async Task Test3Dlut(string imagePath, string lutPath, string resName)
        {
            string dir = Path.GetDirectoryName(imagePath);
            string resPath = Path.Combine(dir, resName);

            using CubeLUT cubeLUT = new CubeLUT();
            cubeLUT.LoadFile(lutPath);
           
            byte[] rgbBytes;
            int width;
            int height;

            using (MagickImage image = new MagickImage(imagePath))
            {
                width = image.Width;
                height = image.Height;
                image.Depth = 8;

                Stopwatch sw1 = Stopwatch.StartNew();
                ChunkedMemoryStream chunkedMemoryStream = new ChunkedMemoryStream();
                image.Write(chunkedMemoryStream, MagickFormat.Rgba);
                rgbBytes = chunkedMemoryStream.ToArray();
                sw1.Stop();
                Console.WriteLine($"MagickImageToByte Time: {sw1.ElapsedMilliseconds}");
            }

            using GPUImage<RGBA32> inputImage = new GPUImage<RGBA32>(width, height, rgbBytes);

            Stopwatch sw = Stopwatch.StartNew();

            LUT3D.WarmUp(device);
   
            sw.Stop();
            Console.WriteLine($"LoadKernel Time: {sw.ElapsedMilliseconds}");

            sw.Restart();

            using GPUImage<RGBA32> outputImage = await LUT3D.Execute(device, inputImage, cubeLUT.LutBuffer, cubeLUT.LutSize);
       
            sw.Stop();
            Console.WriteLine($"Kernel Execution Time: {sw.ElapsedMilliseconds}");

            GPUImage<RGBA32> result = outputImage;

            byte[] bytes = await result.ToCPU(device.DefaultStream);

            using (MagickImage resImg = new MagickImage(bytes, new PixelReadSettings(result.Width, result.Height, StorageType.Char, PixelMapping.RGBA)))
            {
                resImg.Write(resPath, MagickFormat.Jpeg);
            }
        }


        public void Dispose()
        {
            device.Dispose();
            context.Dispose();
        }
    }
}
