using ILGPU.Algorithms;

namespace GpuTest.GPU.Core
{
    /// <summary>
    /// helper to calculate indexes upto 4 dimensions
    /// </summary>
    public struct DimsHelper
    {
        // precalculated values
        private readonly int D3, D4;
        // Dimensions lengths
        public readonly int X = 0, Y = 0, Z = 0, W = 0;
        // Total flatten size
        public readonly int Size;

        /// <summary>
        /// Initializes dimensions
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="w"></param>
        public DimsHelper(int x = 0, int y = 0, int z = 0, int w = 0)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;

            // D1 = 1;
            // D2 = x; 
            D3 = x * y;
            D4 = x * y * z;
            Size = XMath.Max(1, x) * XMath.Max(1, y) * XMath.Max(1, z) * XMath.Max(1, w);
        }

        public int Get2DIndex(int x, int y)
        {
            return x + y * X;
        }

        public (int x, int y) Split2DIndex(int index)
        {
            int x = index % X;
            int y = (index - x) / X % Y;

            return (x, y);
        }

        public int Get3DIndex(int x, int y, int z)
        {
            return x + y * X + z * D3;
        }

        public (int x, int y, int z) Split3DIndex(int index)
        {
            int x = index % X;
            int y = (index - x) / X % Y;
            int z = (index - y * X - x) / D3 % Z;

            return (x, y, z);
        }

        public int Get4DIndex(int x, int y, int z, int w)
        {
            return x + y * X + z * D3 + w * D4;
        }

        public (int x, int y, int z, int w) Split4DIndex(int index)
        {
            int x = index % X;
            int y = (index - x) / X % Y;
            int z = (index - y * X - x) / D3 % Z;
            int w = (index - z * D3 - y * X - x) / D4 % W;

            return (x, y, z, w);
        }
    }
}
