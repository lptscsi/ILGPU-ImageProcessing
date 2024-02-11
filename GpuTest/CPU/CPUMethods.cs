using GpuTest.Utils.Color;
using ImageMagick;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static GpuTest.CropHelper;

namespace GpuTest.CPU
{
    public static class CPUMethods
    {

        public class Histogram
        {
            #region Поля

            private byte[] bytes;

            #endregion

            public Histogram(byte[] rgb24)
            {
                this.bytes = rgb24;
            }

            #region Методы

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static byte Get8BitBT709Luminance(byte r, byte g, byte b)
                  => (byte)((r * .2126F) + (g * .7152F) + (b * .0722F) + 0.5F);

            public async Task<int[]> GetHistogram()
            {
                int[] res = new int[256];
                res.AsSpan().Fill(0);

                int K = Math.Max(8, Environment.ProcessorCount);
                int[][] t = new int[K][];

                for (int i = 0; i < K; ++i)
                {
                    t[i] = new int[256];
                    t[i].AsSpan().Fill(0);
                }

                Task[] tasks = new Task[K];

                for(int i = 0; i < K; ++i)
                {
                    int taskIdx = i;

                    tasks[i] = Task.Run(() =>
                    {
                        ReadOnlySpan<RGB24> rgbs = MemoryMarshal.Cast<byte, RGB24>(bytes);
                        int length = rgbs.Length;
                        int stride = (length / K);
                        var data = taskIdx == (K - 1) ?
                         rgbs.Slice(taskIdx * stride) :
                         rgbs.Slice(taskIdx * stride, stride);

                        int[] hist = t[taskIdx];

                        for (int index = 0; index < data.Length; index++)
                        {
                            RGB24 value = data[index];
                            byte luminance = Get8BitBT709Luminance(value.R, value.G, value.B);
                            hist[luminance] += 1;
                        }
                    });
                }

                await Task.WhenAll(tasks);
            

                for (int j = 0; j < K; ++j)
                {
                    for (int i = 0; i < res.Length; ++i)
                    {
                        res[i] += t[j][i];
                    }
                }

                //int cnt = res.Sum();

                return res;
            }

            #endregion
        }


        /// <summary>
        /// Apply CPU version of the algorithm
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        public static void TestMiscAlgorithms(string imagePath, string resName)
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
                rgbBytes = image.ToByteArray(MagickFormat.Rgb);
            }

            ImageBytesResult rgbBytes2 = new ImageBytesResult()
            {
                Bytes = rgbBytes,
                Width = width,
                Height = height,
            };

            Stopwatch sw = Stopwatch.StartNew();

            //ImageBytesResult imgBytes = GaussianBlur.Execute(rgbBytes, width, height, 2, 11);
            Histogram histogram = new Histogram(rgbBytes);
            int[] hist = histogram.GetHistogram().GetAwaiter().GetResult();

            //var imgBytes = ScaleRGB2.Scale2(rgbBytes2, new OpenCvSharp.Size(width * 1.55, height * 1.55));

            //var imgBytes = RgbScaleHelper.DownScaleRgb(rgbBytes, width, height, 8.5f, ColorDepths.Bits8);
            sw.Stop();
            Console.WriteLine($"CPU Test Elapsed time: {sw.ElapsedMilliseconds}");
          
            /*
            using (MagickImage resImg = new MagickImage(imgBytes.Bytes, new PixelReadSettings(imgBytes.Width, imgBytes.Height, StorageType.Char, PixelMapping.RGB)))
            {
                resImg.Write(resPath, MagickFormat.Jpeg);
            }
            */
        }
    }
}
