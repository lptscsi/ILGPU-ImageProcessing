using System.Runtime.InteropServices;

namespace GpuTest.Utils.Color
{

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct Offset2D 
    {
        public short X;

        public short Y;

        public Offset2D(short x, short y)
        {
            this.X = x;
            this.Y = y;
        }
    }
}
