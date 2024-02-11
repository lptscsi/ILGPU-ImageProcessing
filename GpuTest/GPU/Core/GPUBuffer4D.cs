using ILGPU;
using ILGPU.Runtime;
using System.Threading.Tasks;

namespace GpuTest.GPU.Core
{
    public class GPUBuffer4D<T> : BaseGpuBuffer<T>
        where T : unmanaged
    {
        public GPUBuffer4D(int x, int y, int z, int w)
            : base(new DimsHelper(x, y, z, w))
        {
        }

        public async Task<dBuffer4D<T>> ToDevice(AcceleratorStream stream)
        {
            await InitMemoryBuffer(stream);
            return new dBuffer4D<T>(_memoryBuffer, DIMS);
        }

        public T Get(int x, int y, int z, int w)
        {
            int index = DIMS.Get4DIndex(x, y, z, w);
            return _cpuData[index];
        }

        public void Set(T value, int x, int y, int z, int w)
        {
            int index = DIMS.Get4DIndex(x, y, z, w);
            _cpuData[index] = value;
        }
    }

    public struct dBuffer4D<T>
        where T : unmanaged
    {
        public readonly DimsHelper DIMS;
        public ArrayView1D<T, Stride1D.Dense> data;

        public dBuffer4D(MemoryBuffer1D<T, Stride1D.Dense> data,
                       DimsHelper dims)
        {
            DIMS = dims;
            this.data = data;
        }

        public T Get(int x, int y, int z, int w)
        {
            int index = DIMS.Get4DIndex(x, y, z, w);
            return data[index];
        }

        public void Set(T value, int x, int y, int z, int w)
        {
            int index = DIMS.Get4DIndex(x, y, z, w);
            data[index] = value;
        }
    }
}
