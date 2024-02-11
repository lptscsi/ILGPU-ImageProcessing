using ILGPU;
using ILGPU.Runtime;
using System.Threading.Tasks;

namespace GpuTest.GPU.Core
{
    public class GPUBuffer3D<T> : BaseGpuBuffer<T>
        where T : unmanaged
    {
        public GPUBuffer3D(int x, int y, int z)
            : base(new DimsHelper(x, y, z))
        {

        }

        public async Task<dBuffer3D<T>> ToDevice(AcceleratorStream stream)
        {
            await InitMemoryBuffer(stream);
            return new dBuffer3D<T>(_memoryBuffer, DIMS);
        }

        public T Get(int x, int y, int z)
        {
            int index = DIMS.Get3DIndex(x, y, z);
            return _cpuData[index];
        }

        public void Set(T value, int x, int y, int z)
        {
            int index = DIMS.Get3DIndex(x, y, z);
            _cpuData[index] = value;
        }
    }

    public struct dBuffer3D<T>
        where T : unmanaged
    {
        public readonly DimsHelper DIMS;
        public ArrayView1D<T, Stride1D.Dense> data;

        public dBuffer3D(MemoryBuffer1D<T, Stride1D.Dense> data,
                        DimsHelper dims)
        {
            DIMS = dims;
            this.data = data;
        }

        public T Get(int x, int y, int z)
        {
            int index = DIMS.Get3DIndex(x, y, z);
            return data[index];
        }

        public void Set(T value, int x, int y, int z)
        {
            int index = DIMS.Get3DIndex(x, y, z);
            data[index] = value;
        }
    }
}
