using ILGPU.Algorithms;
using System.Numerics;
using System.Runtime.InteropServices;

namespace GpuTest.Utils.Color
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Vec3 : IVec3
    {
        const float USHORT_MAX = ushort.MaxValue;
        public const int SIZE = sizeof(float) * 3;

        public float X;
        public float Y;
        public float Z;

        public Vec3()
        {
            this.X = 0;
            this.Y = 0;
            this.Z = 0;
        }

        public Vec3(Vec3 toCopy)
        {
            this.X = toCopy.X;
            this.Y = toCopy.Y;
            this.Z = toCopy.Z;
        }

        public Vec3(RGB color)
        {
            X = color.R;
            Y = color.G;
            Z = color.B;
        }

        public Vec3(LinearRGB color)
        {
            X = color.R;
            Y = color.G;
            Z = color.B;
        }

        public Vec3(RGBA32 color)
        {
            X = color.R / 255f;
            Y = color.G / 255f;
            Z = color.B / 255f;
        }

        public Vec3(RGB24 color)
        {
            X = color.R / 255f;
            Y = color.G / 255f;
            Z = color.B / 255f;
        }

        public Vec3(RGB48 color)
        {
            X = color.R / USHORT_MAX;
            Y = color.G / USHORT_MAX;
            Z = color.B / USHORT_MAX;
        }

        public Vec3(RGBA64 color)
        {
            X = color.R / USHORT_MAX;
            Y = color.G / USHORT_MAX;
            Z = color.B / USHORT_MAX;
        }

        public Vec3(float v)
        {
            this.X = v;
            this.Y = v;
            this.Z = v;
        }

        public Vec3(float X, float Y, float Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public Vec3(double X, double Y, double Z)
        {
            this.X = (float)X;
            this.Y = (float)Y;
            this.Z = (float)Z;
        }

        public Vec3(Vec2 uv_in, float v)
        {
            this.X = uv_in.X;
            this.Y = uv_in.Y;
            this.Z = v;
        }

        /// <summary>
        /// Returns a new <see cref="Vec3"/> representing this instance.
        /// </summary>
        /// <returns>The <see cref="Vec3"/>.</returns>
        public Vec3 ToVec3() => this;

        public override string ToString()
        {
            return "{" + string.Format("{0:0.00}", X) + ", " + string.Format("{0:0.00}", Y) + ", " + string.Format("{0:0.00}", Z) + "}";
        }


        public static Vec3 operator -(Vec3 vec)
        {
            return new Vec3(-vec.X, -vec.Y, -vec.Z);
        }


        public float length()
        {
            return XMath.Sqrt(X * X + Y * Y + Z * Z);
        }

        public float average()
        {
            return (X + Y + Z) / 3.0f;
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

        public static Vec3 lerp(Vec3 a, Vec3 b, float t)
        {
            return (1f - t) * a + t * b;
        }

        public static Vec3 setX(Vec3 v, float X)
        {
            return new Vec3(X, v.Y, v.Z);
        }

        public static Vec3 setY(Vec3 v, float Y)
        {
            return new Vec3(v.X, Y, v.Z);
        }

        public static Vec3 setZ(Vec3 v, float Z)
        {
            return new Vec3(v.X, v.Y, Z);
        }


        public static float dist(Vec3 v1, Vec3 v2)
        {
            float dx = v1.X - v2.X;
            float dy = v1.Y - v2.Y;
            float dz = v1.Z - v2.Z;
            return XMath.Sqrt(dx * dx + dy * dy + dz * dz);
        }


        public static Vec3 operator +(Vec3 v1, Vec3 v2)
        {
            return new Vec3(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
        }


        public static Vec3 operator -(Vec3 v1, Vec3 v2)
        {
            return new Vec3(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
        }


        public static Vec3 operator *(Vec3 v1, Vec3 v2)
        {
            return new Vec3(v1.X * v2.X, v1.Y * v2.Y, v1.Z * v2.Z);
        }


        public static Vec3 operator /(Vec3 v1, Vec3 v2)
        {
            return new Vec3(v1.X / v2.X, v1.Y / v2.Y, v1.Z / v2.Z);
        }


        public static Vec3 operator /(float v, Vec3 v1)
        {
            return new Vec3(v / v1.X, v / v1.Y, v / v1.Z);
        }


        public static Vec3 operator *(Vec3 v1, float v)
        {
            return new Vec3(v1.X * v, v1.Y * v, v1.Z * v);
        }


        public static Vec3 operator *(float v, Vec3 v1)
        {
            return new Vec3(v1.X * v, v1.Y * v, v1.Z * v);
        }


        public static Vec3 operator +(Vec3 v1, float v)
        {
            return new Vec3(v1.X + v, v1.Y + v, v1.Z + v);
        }


        public static Vec3 operator +(float v, Vec3 v1)
        {
            return new Vec3(v1.X + v, v1.Y + v, v1.Z + v);
        }

        public static Vec3 operator -(Vec3 v1, float v)
        {
            return new Vec3(v1.X + v, v1.Y + v, v1.Z + v);
        }


        public static Vec3 operator -(float v, Vec3 v1)
        {
            return new Vec3(v1.X + v, v1.Y + v, v1.Z + v);
        }


        public static Vec3 operator /(Vec3 v1, float v)
        {
            return v1 * (1.0f / v);
        }


        public static float dot(Vec3 v1, Vec3 v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
        }


        public static Vec3 cross(Vec3 v1, Vec3 v2)
        {
            return new Vec3(v1.Y * v2.Z - v1.Z * v2.Y,
                          -(v1.X * v2.Z - v1.Z * v2.X),
                            v1.X * v2.Y - v1.Y * v2.X);
        }


        public static Vec3 unitVector(Vec3 v)
        {
            return v / XMath.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        }

        public Vec3 Normalize()
        {
            return unitVector(this);
        }


        public static Vec3 reflect(Vec3 incomming, Vec3 normal)
        {
            return incomming - normal * 2f * dot(incomming, normal);
        }

        public static Vec3 refract(Vec3 v, Vec3 n, float niOverNt)
        {
            Vec3 uv = unitVector(v);
            float dt = dot(uv, n);
            float discriminant = 1.0f - niOverNt * niOverNt * (1f - dt * dt);

            if (discriminant > 0)
            {
                return niOverNt * (uv - (n * dt)) - n * XMath.Sqrt(discriminant);
            }

            return v;
        }


        public static float NormalReflectance(Vec3 normal, Vec3 incomming, float iorFrom, float iorTo)
        {
            float iorRatio = iorFrom / iorTo;
            float cosThetaI = -dot(normal, incomming);
            float sinThetaTSquared = iorRatio * iorRatio * (1 - cosThetaI * cosThetaI);
            if (sinThetaTSquared > 1)
            {
                return 1f;
            }

            float cosThetaT = XMath.Sqrt(1 - sinThetaTSquared);
            float rPerpendicular = (iorFrom * cosThetaI - iorTo * cosThetaT) / (iorFrom * cosThetaI + iorTo * cosThetaT);
            float rParallel = (iorFrom * cosThetaI - iorTo * cosThetaT) / (iorFrom * cosThetaI + iorTo * cosThetaT);
            return (rPerpendicular * rPerpendicular + rParallel * rParallel) / 2f;
        }


        public static Vec3 aces_approx(Vec3 v)
        {
            v *= 0.6f;
            float a = 2.51f;
            float b = 0.03f;
            float c = 2.43f;
            float d = 0.59f;
            float e = 0.14f;
            Vec3 working = (v * (a * v + b)) / (v * (c * v + d) + e);
            return new Vec3(XMath.Clamp(working.X, 0, 1), XMath.Clamp(working.Y, 0, 1), XMath.Clamp(working.Z, 0, 1));
        }


        public static Vec3 reinhard(Vec3 v)
        {
            return v / (1.0f + v);
        }


        public static bool Equals(Vec3 a, Vec3 b)
        {
            return a.X == b.X &&
                   a.Y == b.Y &&
                   a.Z == b.Z;
        }

        public static int CompareTo(Vec3 a, Vec3 b)
        {
            return a.lengthSquared().CompareTo(b.lengthSquared());
        }

        public static Vec3 abs(Vec3 vec3)
        {
            return new Vec3(XMath.Abs(vec3.X), XMath.Abs(vec3.Y), XMath.Abs(vec3.Z));
        }

        public static Vec3 ceil(Vec3 vec3)
        {
            return new Vec3(XMath.Ceiling(vec3.X), XMath.Ceiling(vec3.Y), XMath.Ceiling(vec3.Z));
        }

        public static Vec3 mod(Vec3 vec3, float v)
        {
            return vec3 - v * floor(vec3 / v);
            //return new Vec3(vec3.X % v, vec3.Y % v, vec3.Z % v);
        }

        public static Vec3 clamp(Vec3 vec3, float v1, float v2)
        {
            return new Vec3(XMath.Clamp(vec3.X, v1, v2), XMath.Clamp(vec3.Y, v1, v2), XMath.Clamp(vec3.Z, v1, v2));
        }

        public static Vec3 floor(Vec3 vec3)
        {
            return new Vec3(XMath.Floor(vec3.X), XMath.Floor(vec3.Y), XMath.Floor(vec3.Z));
        }

        public static Vec3 vecif(Vec3 val, Vec3 cond, Vec3 newVal)
        {
            Vec3 toReturn = val;

            if ((int)val.X == (int)cond.X)
            {
                toReturn.X = newVal.X;
            }

            if ((int)val.Y == (int)cond.Y)
            {
                toReturn.Y = newVal.Y;
            }

            if ((int)val.Z == (int)cond.Z)
            {
                toReturn.Z = newVal.Z;
            }

            return toReturn;
        }

        public static Vec3 Clamp(Vec3 value, Vec3 min, Vec3 max)
        {
            float X = XMath.Clamp(value.X, min.X, max.X);
            float Y = XMath.Clamp(value.Y, min.Y, max.Y);
            float Z = XMath.Clamp(value.Z, min.Z, max.Z);
            return new Vec3(X, Y, Z);
        }

        public static Vec3 Pow(Vec3 color, float power)
        {
            return new Vec3(
                XMath.Pow(color.X, power),
                XMath.Pow(color.Y, power),
                XMath.Pow(color.Z, power)
            );
        }

        public static Vec3 Log(Vec3 color, float base_value)
        {
            return new Vec3(
                XMath.Log(color.X, base_value),
                XMath.Log(color.Y, base_value),
                XMath.Log(color.Z, base_value)
            );
        }

        public static Vec3 Sqrt(Vec3 vec3)
        {
            return new Vec3(
                XMath.Sqrt(vec3.X),
                XMath.Sqrt(vec3.Y),
                XMath.Sqrt(vec3.Z)
            );
        }

        public static Vec3 Exp(Vec3 vec3)
        {
            return new Vec3(
                XMath.Exp(vec3.X),
                XMath.Exp(vec3.Y),
                XMath.Exp(vec3.Z)
            );
        }

        public static Vec3 Min(Vec3 a, Vec3 b)
        {
            return new Vec3(
                XMath.Min(a.X, b.X),
                XMath.Min(a.Y, b.Y),
                XMath.Min(a.Z, b.Z)
            );
        }

        public static Vec3 Max(Vec3 a, Vec3 b)
        {
            return new Vec3(
                XMath.Max(a.X, b.X),
                XMath.Max(a.Y, b.Y),
                XMath.Max(a.Z, b.Z)
            );
        }

        public static Vec3 Min(Vec3 a)
        {
            return new Vec3(XMath.Min(a.X, XMath.Min(a.Y, a.Z)));
        }

        public static Vec3 Max(Vec3 a)
        {
            return new Vec3(XMath.Max(a.X, XMath.Max(a.Y, a.Z)));
        }

        public static Vec3 Abs(Vec3 viewLerp)
        {
            return new Vec3(XMath.Abs(viewLerp.X), XMath.Abs(viewLerp.Y), XMath.Abs(viewLerp.Z));
        }

        public static Vec3 Ceiling(Vec3 v)
        {
            return new Vec3(XMath.Ceiling(v.X), XMath.Ceiling(v.Y), XMath.Ceiling(v.Z));
        }

        public static Vec3 Clamp(Vec3 modViewLerp, float v1, float v2)
        {
            return new Vec3(
                XMath.Clamp(modViewLerp.X, v1, v2),
                XMath.Clamp(modViewLerp.Y, v1, v2),
                XMath.Clamp(modViewLerp.Z, v1, v2)
            );
        }

        public static float Mod(Vec3 vec3, float v)
        {
            return vec3.X % v + vec3.Y % v + vec3.Z % v;
        }

        public bool NearZero()
        {
            const float epsilon = 0.0001f; // Define a small threshold for comparison

            // Check if each component of the vector is close to zero
            return XMath.Abs(X) < epsilon && XMath.Abs(Y) < epsilon && XMath.Abs(Z) < epsilon;
        }

        public static implicit operator Vector3(Vec3 d)
        {
            return new Vector3((float)d.X, (float)d.Y, (float)d.Z);
        }

        public static implicit operator Vec3(Vector3 d)
        {
            return new Vec3(d.X, d.Y, d.Z);
        }

        public static implicit operator Vector4(Vec3 d)
        {
            return new Vector4((float)d.X, (float)d.Y, (float)d.Z, 0);
        }

        public static implicit operator Vec3(Vector4 d)
        {
            return new Vec3(d.X, d.Y, d.Z);
        }

        /// <summary>
        /// Allows the implicit conversion of an instance of <see cref="RGBA32"/> to a
        /// <see cref="Vec3"/>.
        /// </summary>
        public static implicit operator Vec3(in RGBA32 color) => new Vec3(color);


        /// <summary>
        /// Allows the implicit conversion of an instance of <see cref="RGB"/> to a
        /// <see cref="Vec3"/>.
        /// </summary>
        /// <param name="color">The instance of <see cref="RGB"/> to convert.</param>
        /// <returns>An instance of <see cref="Vec3"/>.</returns>
        public static implicit operator Vec3(in RGB color) => new Vec3(color);
    }
}
