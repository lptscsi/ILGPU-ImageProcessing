using ILGPU;
using ILGPU.Runtime;
using System.Threading.Tasks;

namespace GpuTest.GPU.Core
{
    public class GPUBuffer2D<T> : BaseGpuBuffer<T>
        where T : unmanaged
    {
        public GPUBuffer2D(int x, int y)
            : base(new DimsHelper(x, y))
        {

        }

        public async Task<dBuffer2D<T>> ToDevice(AcceleratorStream stream)
        {
            await InitMemoryBuffer(stream);
            return new dBuffer2D<T>(_memoryBuffer, DIMS);
        }

        public T Get(int x, int y)
        {
            int index = DIMS.Get2DIndex(x, y);
            return _cpuData[index];
        }

        public void Set(T value, int x, int y)
        {
            int index = DIMS.Get2DIndex(x, y);
            _cpuData[index] = value;
        }
    }

    public struct dBuffer2D<T>
        where T : unmanaged
    {
        public readonly DimsHelper DIMS;
        public ArrayView1D<T, Stride1D.Dense> data;

        public dBuffer2D(MemoryBuffer1D<T, Stride1D.Dense> data,
                        DimsHelper dims)
        {
            DIMS = dims;
            this.data = data;
        }

        public T Get(int x, int y)
        {
            int index = DIMS.Get2DIndex(x, y);
            return data[index];
        }

        public void Set(T value, int x, int y)
        {
            int index = DIMS.Get2DIndex(x, y);
            data[index] = value;
        }
    }
}
