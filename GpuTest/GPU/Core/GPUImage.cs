using GpuTest.Utils.Color;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Runtime;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace GpuTest.GPU.Core
{
    public class GPUImage<T> : IDisposable
        where T : unmanaged, IVec3
    {
        private readonly int pixelSize;
        private byte[] _data;

        public readonly int Width;
        public readonly int Height;

        public MemoryBuffer1D<T, Stride1D.Dense> _memoryBuffer;

        public GPUImage(int width, int height)
            : this(width, height, null)
        {

        }

        public GPUImage(int width, int height, byte[] data)
        {
            pixelSize = Marshal.SizeOf(typeof(T));
            Width = width;
            Height = height;

            int len = width * height * pixelSize;
            if (data != null && data.Length < len)
            {
                throw new ArgumentException($"Not enough size of data for width: {width} height: {height}");
            }

            _data = data;
        }

        public Index2D Extent2D => new Index2D(Width, Height);

        public int Extent => Width * Height;

        public int Size => Width * Height * pixelSize;

        public async Task<ArrayView1D<T, Stride1D.Dense>> ToDevice1D(AcceleratorStream stream)
        {
            if (_memoryBuffer == null)
            {
                Accelerator device = stream.Accelerator;
                _memoryBuffer = device.Allocate1D<T>(new Index1D(Extent));
                if (_data != null)
                {
                    var gcHandle = GCHandle.Alloc(_data, GCHandleType.Pinned);
                    try
                    {
                        using var pageLockScope = device.CreatePageLockFromPinned<T>(gcHandle.AddrOfPinnedObject(), Extent);

                        _memoryBuffer.View.CopyFromPageLockedAsync(stream, pageLockScope);
                        await stream.SynchronizeAsync();
                    }
                    finally
                    {
                        gcHandle.Free();
                    }
                }
                else
                {
                    _memoryBuffer.MemSetToZero();
                }

            }
            return _memoryBuffer.View;
        }

        public async Task<ArrayView2D<T, Stride2D.DenseX>> ToDevice2D(AcceleratorStream stream)
        {
            ArrayView1D<T, Stride1D.Dense> arrayView1D = await ToDevice1D(stream);
            return arrayView1D.As2DDenseXView(new LongIndex2D(Width, Height));
        }

        public async Task<dImage<T>> ToDevice(AcceleratorStream stream)
        {
            ArrayView1D<T, Stride1D.Dense> arrayView1D = await ToDevice1D(stream);
            var data = arrayView1D.As2DDenseXView(new LongIndex2D(Width, Height));
            return new dImage<T>(data, Width, Height);
        }

        public async Task<byte[]> ToCPU(AcceleratorStream stream)
        {
            if (_data != null)
            {
                await ToCPUPageLocked(stream, _data);
                return _data;
            }
            else
            {
                byte[] bytes = new byte[Size];
                await ToCPUPageLocked(stream, bytes);
                return bytes;
            }
        }

        public async Task ToCPUPageLocked(AcceleratorStream stream, byte[] bytes)
        {
            if (bytes.Length < Size)
            {
                throw new ArgumentException($"Not enough size to copy buffer to CPU data");
            }

            var gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                Accelerator device = stream.Accelerator;
                using var pageLockScope = device.CreatePageLockFromPinned<T>(gcHandle.AddrOfPinnedObject(), Extent);
                _memoryBuffer.View.CopyToPageLockedAsync(stream, pageLockScope);
                await stream.SynchronizeAsync();
            }
            finally
            {
                gcHandle.Free();
            }
        }


        public void Dispose()
        {
            _memoryBuffer?.Dispose();
            _memoryBuffer = null;
            _data = null;
        }
    }

    public struct dImage<T>
       where T : unmanaged, IVec3
    {
        public readonly int Width;
        public readonly int Height;
        public ArrayView2D<T, Stride2D.DenseX> data;

        public dImage(ArrayView2D<T, Stride2D.DenseX> data,
                       int width, int height)
        {
            this.Width = width;
            this.Height = height;
            this.data = data;
        }

        #region bilinear

        // weighting bilinear Function
        private static float wb(float x, float y)
        {
            return x * y;
        }

        #endregion

        #region fake simplified bicubic

        // weighting Function (The bicubic algorithm can be modified by forcing the partial derivatives and cross derivatives to be 0 at each data point.)
        private static float wf(float x, float y)
        {
            return x * x * y * y * (9 - 6 * x - 6 * y + 4 * x * y);
        }

        #endregion

        #region bicubic

        // w0, w1, w2, and w3 are the four cubic B-spline basis functions
        static float w0(float a)
        {
            //    return (1.0f/6.0f)*(-a*a*a + 3.0f*a*a - 3.0f*a + 1.0f);
            return (1.0f / 6.0f) * (a * (a * (-a + 3.0f) - 3.0f) + 1.0f);  // optimized
        }

        static float w1(float a)
        {
            //    return (1.0f/6.0f)*(3.0f*a*a*a - 6.0f*a*a + 4.0f);
            return (1.0f / 6.0f) * (a * a * (3.0f * a - 6.0f) + 4.0f);
        }

        static float w2(float a)
        {
            //    return (1.0f/6.0f)*(-3.0f*a*a*a + 3.0f*a*a + 3.0f*a + 1.0f);
            return (1.0f / 6.0f) * (a * (a * (-3.0f * a + 3.0f) + 3.0f) + 1.0f);
        }

        static float w3(float a) { return (1.0f / 6.0f) * (a * a * a); }

        // filter 4 values using cubic splines
        static float cubicFilter(float x, float c0, float c1, float c2, float c3)
        {
            float r;
            r = c0 * w0(x);
            r += c1 * w1(x);
            r += c2 * w2(x);
            r += c3 * w3(x);
            return r;
        }

        #endregion

        /// <summary>
        /// Bicubic
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Vec3 Tex2Dcub(float x, float y)
        {
            int y0 = (int)XMath.Floor(y);
            if (y0 >= Height)
            {
                y0 = Height - 1;
            }
            int y1 = y0 - 1;
            if (y1 < 0)
            {
                y1 = 0;
            }
            int y2 = y0 + 1;
            if (y2 >= Height)
            {
                y2 = Height - 1;
            }
            int y3 = y0 + 2;
            if (y3 >= Height)
            {
                y3 = Height - 1;
            }

            int x0 = (int)XMath.Floor(x);
            if (x0 >= Width)
            {
                x0 = Width - 1;
            }
            int x1 = x0 - 1;
            if (x1 < 0)
            {
                x1 = 0;
            }
            int x2 = x0 + 1;
            if (x2 >= Width)
            {
                x2 = Width - 1;
            }
            int x3 = x0 + 2;
            if (x3 >= Width)
            {
                x3 = Width - 1;
            }

            float fx = x - x0;
            float fy = y - y0;

            Vec3 v11 = data[x1, y1].ToVec3();
            Vec3 v01 = data[x0, y1].ToVec3();
            Vec3 v21 = data[x2, y1].ToVec3();
            Vec3 v31 = data[x3, y1].ToVec3();

            Vec3 v10 = data[x1, y0].ToVec3();
            Vec3 v00 = data[x0, y0].ToVec3();
            Vec3 v20 = data[x2, y0].ToVec3();
            Vec3 v30 = data[x3, y0].ToVec3();

            Vec3 v12 = data[x1, y2].ToVec3();
            Vec3 v02 = data[x0, y2].ToVec3();
            Vec3 v22 = data[x2, y2].ToVec3();
            Vec3 v32 = data[x3, y2].ToVec3();

            Vec3 v13 = data[x1, y3].ToVec3();
            Vec3 v03 = data[x0, y3].ToVec3();
            Vec3 v23 = data[x2, y3].ToVec3();
            Vec3 v33 = data[x3, y3].ToVec3();

            float r = cubicFilter(
                  fy, cubicFilter(fx, v11.X, v01.X, v21.X, v31.X),
                      cubicFilter(fx, v10.X, v00.X, v20.X, v30.X),
                      cubicFilter(fx, v12.X, v02.X, v22.X, v32.X),
                      cubicFilter(fx, v13.X, v03.X, v23.X, v33.X)
                     );

            float g = cubicFilter(
                 fy, cubicFilter(fx, v11.Y, v01.Y, v21.Y, v31.Y),
                     cubicFilter(fx, v10.Y, v00.Y, v20.Y, v30.Y),
                     cubicFilter(fx, v12.Y, v02.Y, v22.Y, v32.Y),
                     cubicFilter(fx, v13.Y, v03.Y, v23.Y, v33.Y)
                    );

            float b = cubicFilter(
                 fy, cubicFilter(fx, v11.Z, v01.Z, v21.Z, v31.Z),
                     cubicFilter(fx, v10.Z, v00.Z, v20.Z, v30.Z),
                     cubicFilter(fx, v12.Z, v02.Z, v22.Z, v32.Z),
                     cubicFilter(fx, v13.Z, v03.Z, v23.Z, v33.Z)
                    );

            return new Vec3(r, g, b);
        }

        /// <summary>
        /// Bilinear
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Vec3 Tex2Dlin(float x, float y)
        {
            if (y < 0)
            {
                y = 0;
            }
            if (x < 0)
            {
                x = 0;
            }

            int y0 = (int)XMath.Floor(y);
            if (y0 >= Height)
            {
                y0 = Height - 1;
            }
            int y1 = y0 + 1;
            if (y1 >= Height)
            {
                y1 = Height - 1;
            }
            int x0 = (int)XMath.Floor(x);
            if (x0 >= Width)
            {
                x0 = Width - 1;
            }
            int x1 = x0 + 1;
            if (x1 >= Width)
            {
                x1 = Width - 1;
            }

            float fx = x - x0;
            float fy = y - y0;

            Vec3 v00 = data[x0, y0].ToVec3();
            Vec3 v10 = data[x1, y0].ToVec3();
            Vec3 v01 = data[x0, y1].ToVec3();
            Vec3 v11 = data[x1, y1].ToVec3();

            /*
            float r = MathUtils.Blerp(v00.X, v10.X, v01.X, v11.X, fx, fy);
            float g = MathUtils.Blerp(v00.Y, v10.Y, v01.Y, v11.Y, fx, fy);
            float b = MathUtils.Blerp(v00.Z, v10.Z, v01.Z, v11.Z, fx, fy);
            */

            float r = wb(1 - fx, 1 - fy) * v00.X + wb(1 - fx, fy) * v01.X + wb(fx, 1 - fy) * v10.X + wb(fx, fy) * v11.X;
            float g = wb(1 - fx, 1 - fy) * v00.Y + wb(1 - fx, fy) * v01.Y + wb(fx, 1 - fy) * v10.Y + wb(fx, fy) * v11.Y;
            float b = wb(1 - fx, 1 - fy) * v00.Z + wb(1 - fx, fy) * v01.Z + wb(fx, 1 - fy) * v10.Z + wb(fx, fy) * v11.Z;

            return new Vec3(r, g, b);
        }

        /// <summary>
        /// Simplified bicubic (better than bilinear)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Vec3 Tex2D(float x, float y)
        {
            if (y < 0)
            {
                y = 0;
            }
            if (x < 0)
            {
                x = 0;
            }

            int y0 = (int)XMath.Floor(y);
            if (y0 >= Height)
            {
                y0 = Height - 1;
            }
            int y1 = y0 + 1;
            if (y1 >= Height)
            {
                y1 = Height - 1;
            }
            int x0 = (int)XMath.Floor(x);
            if (x0 >= Width)
            {
                x0 = Width - 1;
            }
            int x1 = x0 + 1;
            if (x1 >= Width)
            {
                x1 = Width - 1;
            }

            float fx = x - x0;
            float fy = y - y0;

            Vec3 v00 = data[x0, y0].ToVec3();
            Vec3 v10 = data[x1, y0].ToVec3();
            Vec3 v01 = data[x0, y1].ToVec3();
            Vec3 v11 = data[x1, y1].ToVec3();

            float r = wf(1 - fx, 1 - fy) * v00.X + wf(1 - fx, fy) * v01.X + wf(fx, 1 - fy) * v10.X + wf(fx, fy) * v11.X;
            float g = wf(1 - fx, 1 - fy) * v00.Y + wf(1 - fx, fy) * v01.Y + wf(fx, 1 - fy) * v10.Y + wf(fx, fy) * v11.Y;
            float b = wf(1 - fx, 1 - fy) * v00.Z + wf(1 - fx, fy) * v01.Z + wf(fx, 1 - fy) * v10.Z + wf(fx, fy) * v11.Z;

            return new Vec3(r, g, b);
        }

        public T Get(int x, int y)
        {
            return data[x, y];
        }

        public void Set(int x, int y, T value)
        {
            data[x, y] = value;
        }
    }
}
