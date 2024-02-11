using ILGPU;
using ILGPU.Runtime;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace GpuTest.GPU.Core
{
    public class BaseGpuBuffer<T> : IDisposable
           where T : unmanaged
    {
        public readonly DimsHelper DIMS;
        protected T[] _cpuData;
        protected MemoryBuffer1D<T, Stride1D.Dense> _memoryBuffer;

        public T[] CpuData { get => _cpuData; }

        public BaseGpuBuffer(DimsHelper dims)
        {
            DIMS = dims;
            _cpuData = new T[DIMS.Size];
        }

        protected virtual async Task InitMemoryBuffer(AcceleratorStream stream)
        {
            if (_memoryBuffer == null)
            {
                Accelerator device = stream.Accelerator;

                _memoryBuffer = device.Allocate1D<T>(DIMS.Size);
                var gcHandle = GCHandle.Alloc(_cpuData, GCHandleType.Pinned);
                try
                {
                    using var pageLockScope = device.CreatePageLockFromPinned<T>(gcHandle.AddrOfPinnedObject(), DIMS.Size);

                    _memoryBuffer.View.CopyFromPageLockedAsync(stream, pageLockScope);
                    await stream.SynchronizeAsync();
                }
                finally
                {
                    gcHandle.Free();
                }
            }
        }

        public async Task<T[]> ToCPU(AcceleratorStream stream)
        {
            if (_cpuData != null)
            {
                await ToCPUPageLocked(stream, _cpuData);
                return _cpuData;
            }
            else
            {
                T[] cpuData = new T[DIMS.Size];
                await ToCPUPageLocked(stream, cpuData);
                return cpuData;
            }
        }

        public async Task ToCPUPageLocked(AcceleratorStream stream, T[] cpuData)
        {
            if (cpuData.Length < DIMS.Size)
            {
                throw new ArgumentException($"Not enough size to copy buffer to CPU data");
            }

            var gcHandle = GCHandle.Alloc(cpuData, GCHandleType.Pinned);
            try
            {
                Accelerator device = stream.Accelerator;
                using var pageLockScope = device.CreatePageLockFromPinned<T>(gcHandle.AddrOfPinnedObject(), DIMS.Size);
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
            _cpuData = null;
        }
    }
}
