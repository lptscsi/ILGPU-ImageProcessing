using System.Runtime.InteropServices;

namespace GpuTest.Utils.Color
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public readonly struct Matrix3x3
    {
        public const int SIZE = sizeof(float) * 9;

        public readonly float M00, M01, M02;
        public readonly float M10, M11, M12;
        public readonly float M20, M21, M22;

        public Matrix3x3()
        {
            M00 = 0;
            M01 = 0;
            M02 = 0;
            M10 = 0;
            M11 = 0;
            M12 = 0;
            M20 = 0;
            M21 = 0;
            M22 = 0;
        }

        public Matrix3x3(Matrix3x3 toCopy)
        {
            M00 = toCopy.M00;
            M01 = toCopy.M01;
            M02 = toCopy.M02;
            M10 = toCopy.M10;
            M11 = toCopy.M11;
            M12 = toCopy.M12;
            M20 = toCopy.M20;
            M21 = toCopy.M21;
            M22 = toCopy.M12;
        }

        public Matrix3x3(float xr, float xg, float xb, float yr, float yg, float yb, float zr, float zg, float zb)
        {
            M00 = xr;
            M01 = xg;
            M02 = xb;
            M10 = yr;
            M11 = yg;
            M12 = yb;
            M20 = zr;
            M21 = zg;
            M22 = zb;
        }
    }
}
