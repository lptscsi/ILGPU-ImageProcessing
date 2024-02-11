using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static GpuTest.CropHelper;


namespace GpuTest.Utils.Color
{
    public static class GaussianBlur
    {
        const int maxKernelSize = 11;

        // Creates a Gaussian kernel with the given sigma and size
        static float[] CreateGaussianKernel(float sigma, int radius)
        {
            float[] kernel = new float[maxKernelSize * maxKernelSize];

            float sum = 0;

            int size = 2 * radius + 1;
            int halfSize = radius;
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

        public static ImageBytesResult Execute(byte[] img, int width, int height, float sigma, int kernelSize)
        {
            int radius = kernelSize / 2;
            float[] weights = CreateGaussianKernel(sigma, radius);
            byte[] output = new byte[img.Length];
       
            Parallel.For(0, height, idy =>
            {
                ReadOnlySpan<RGB24> inputSpan = MemoryMarshal.Cast<byte, RGB24>(img.AsSpan());
                Span<RGB24> outputSpan = MemoryMarshal.Cast<byte, RGB24>(output.AsSpan());

                for (int idx = 0; idx < width; idx++)
                {
                    // Center of the filter
                    int pos = (idy * width + idx);

                    Vec3 sum = new Vec3();

                    // Convolve the kernel with the surrounding pixels
                    int RAD = radius;
                    int kernelSize = 2 * RAD + 1;

                    for (int ky = -RAD; ky <= RAD; ky++)
                    {
                        for (int kx = -RAD; kx <= RAD; kx++)
                        {
                            int ix = idx + kx;
                            int iy = idy + ky;

                            // Clamp the coordinates to the image bounds
                            ix = Math.Clamp(ix, 0, width - 1);
                            iy = Math.Clamp(iy, 0, height - 1);

                            Vec3 color = inputSpan[iy * width + ix].ToVec3();

                            int s_idx = (ky + RAD) * kernelSize + (kx + RAD);
                            float weight = weights[s_idx];
                            sum += color * weight;
                        }
                    }

                    outputSpan[pos] = sum;
                }
            });

            return new ImageBytesResult() { Bytes = output, Width = width, Height = height };
        }
    }
}
