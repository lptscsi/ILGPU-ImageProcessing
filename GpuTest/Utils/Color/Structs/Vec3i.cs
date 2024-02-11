using ILGPU.Algorithms;
using System.Runtime.InteropServices;

namespace GpuTest.Utils.Color
{

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Vec3i
    {
        public int X;
        public int Y;
        public int Z;

        public Vec3i(int X, int Y, int Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public Vec3i(float X, float Y, float Z)
        {
            this.X = (int)X;
            this.Y = (int)Y;
            this.Z = (int)Z;
        }

        public override string ToString()
        {
            return "{" + string.Format("{0:0.00}", X) + ", " + string.Format("{0:0.00}", Y) + ", " + string.Format("{0:0.00}", Z) + "}";
        }


        public static Vec3i operator -(Vec3i vec)
        {
            return new Vec3i(-vec.X, -vec.Y, -vec.Z);
        }


        public float length()
        {
            return XMath.Sqrt(X * X + Y * Y + Z * Z);
        }


        public float lengthSquared()
        {
            return X * X + Y * Y + Z * Z;
        }

        public float getAt(int a)
        {
            switch (a)
            {
                case 0:
                    return X;
                case 1:
                    return Y;
                case 2:
                    return Z;
                default:
                    return 0;
            }
        }

        public static Vec3i setX(Vec3i v, int X)
        {
            return new Vec3i(X, v.Y, v.Z);
        }

        public static Vec3i setY(Vec3i v, int Y)
        {
            return new Vec3i(v.X, Y, v.Z);
        }

        public static Vec3i setZ(Vec3i v, int Z)
        {
            return new Vec3i(v.X, v.Y, Z);
        }


        public static float dist(Vec3i v1, Vec3i v2)
        {
            float dx = v1.X - v2.X;
            float dy = v1.Y - v2.Y;
            float dz = v1.Z - v2.Z;
            return XMath.Sqrt(dx * dx + dy * dy + dz * dz);
        }


        public static Vec3i operator +(Vec3i v1, Vec3i v2)
        {
            return new Vec3i(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
        }


        public static Vec3i operator -(Vec3i v1, Vec3i v2)
        {
            return new Vec3i(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
        }


        public static Vec3i operator *(Vec3i v1, Vec3i v2)
        {
            return new Vec3i(v1.X * v2.X, v1.Y * v2.Y, v1.Z * v2.Z);
        }


        public static Vec3i operator /(Vec3i v1, Vec3i v2)
        {
            return new Vec3i(v1.X / v2.X, v1.Y / v2.Y, v1.Z / v2.Z);
        }


        public static Vec3i operator /(int v, Vec3i v1)
        {
            return new Vec3i(v / v1.X, v / v1.Y, v / v1.Z);
        }


        public static Vec3i operator *(Vec3i v1, int v)
        {
            return new Vec3i(v1.X * v, v1.Y * v, v1.Z * v);
        }


        public static Vec3i operator *(int v, Vec3i v1)
        {
            return new Vec3i(v1.X * v, v1.Y * v, v1.Z * v);
        }


        public static Vec3i operator +(Vec3i v1, int v)
        {
            return new Vec3i(v1.X + v, v1.Y + v, v1.Z + v);
        }


        public static Vec3i operator +(int v, Vec3i v1)
        {
            return new Vec3i(v1.X + v, v1.Y + v, v1.Z + v);
        }


        public static Vec3i operator /(Vec3i v1, int v)
        {
            return v1 * (1 / v);
        }


        public static float dot(Vec3i v1, Vec3i v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
        }


        public static Vec3i cross(Vec3i v1, Vec3i v2)
        {
            return new Vec3i(v1.Y * v2.Z - v1.Z * v2.Y,
                          -(v1.X * v2.Z - v1.Z * v2.X),
                            v1.X * v2.Y - v1.Y * v2.X);
        }


        public static bool Equals(Vec3i a, Vec3i b)
        {
            return a.X == b.X &&
                   a.Y == b.Y &&
                   a.Z == b.Z;
        }
    }
}
