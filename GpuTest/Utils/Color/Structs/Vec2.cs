using ILGPU.Algorithms;
using System.Runtime.InteropServices;

namespace GpuTest.Utils.Color
{

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Vec2
    {
        public float X;
        public float Y;

        public Vec2(float X, float Y)
        {
            this.X = X;
            this.Y = Y;
        }

        public static Vec2 operator +(Vec2 v1, Vec2 v2)
        {
            return new Vec2(v1.X + v2.X, v1.Y + v2.Y);
        }


        public static Vec2 operator -(Vec2 v1, Vec2 v2)
        {
            return new Vec2(v1.X - v2.X, v1.Y - v2.Y);
        }


        public static Vec2 operator *(Vec2 v1, Vec2 v2)
        {
            return new Vec2(v1.X * v2.X, v1.Y * v2.Y);
        }


        public static Vec2 operator /(Vec2 v1, Vec2 v2)
        {
            return new Vec2(v1.X / v2.X, v1.Y / v2.Y);
        }


        public static Vec2 operator /(float v, Vec2 v1)
        {
            return new Vec2(v / v1.X, v / v1.Y);
        }


        public static Vec2 operator *(Vec2 v1, float v)
        {
            return new Vec2(v1.X * v, v1.Y * v);
        }


        public static Vec2 operator *(float v, Vec2 v1)
        {
            return new Vec2(v1.X * v, v1.Y * v);
        }


        public static Vec2 operator +(Vec2 v1, float v)
        {
            return new Vec2(v1.X + v, v1.Y + v);
        }

        public static Vec2 operator -(Vec2 v1, float v)
        {
            return new Vec2(v1.X - v, v1.Y - v);
        }

        public static Vec2 operator +(float v, Vec2 v1)
        {
            return new Vec2(v1.X + v, v1.Y + v);
        }

        public static Vec2 operator -(float v, Vec2 v1)
        {
            return new Vec2(v1.X - v, v1.Y - v);
        }


        public static Vec2 operator /(Vec2 v1, float v)
        {
            return v1 * (1.0f / v);
        }

        public float Dot(Vec2 other)
        {
            return (X * other.X) + (Y * other.Y);
        }

        public static float Dot(Vec2 a, Vec2 b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        public static float Distance(Vec2 point, Vec2 center)
        {
            float dx = point.X - center.X;
            float dy = point.Y - center.Y;
            return XMath.Sqrt(dx * dx + dy * dy);
        }

        public float length()
        {
            return XMath.Sqrt(X * X + Y * Y);
        }
    }

}
