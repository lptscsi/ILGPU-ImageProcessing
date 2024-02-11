using System;
using System.Collections.Generic;
using System.Numerics;
using static ILGPU.Algorithms.XMath;
using Numerics = MathNet.Numerics;

namespace GpuTest
{
    /// <summary>
    /// Утилита методов для математических вычисленний
    /// </summary>
    public static class MathUtils
    {
        private const float TwoPI = 2 * PI;

        /// <summary>
        /// Compute x^2.
        /// </summary>
        /// <param name="x">Base.</param>
        /// <returns>Result of the exponentiation.</returns>
        public static T Pow2<T> (in T x) 
            where T : unmanaged, INumber<T>
            => x * x;

        /// <summary>
        /// Compute x^3.
        /// </summary>
        /// <param name="x">Base.</param>
        /// <returns>Result of the exponentiation.</returns>
        public static T Pow3<T>(in T x)
            where T : unmanaged, INumber<T>
            => x * x * x;

        /// <summary>
        /// Compute x^4.
        /// </summary>
        /// <param name="x">Base.</param>
        /// <returns>Result of the exponentiation.</returns>
        public static T Pow4<T>(in T x)
            where T : unmanaged, INumber<T>
            => x * x * (x * x);

        /// <summary>
        /// Compute x^7.
        /// </summary>
        /// <param name="x">Base.</param>
        /// <returns>Result of the exponentiation.</returns>
        public static T Pow7<T>(in T x)
            where T : unmanaged, INumber<T>
            => x * x * x * (x * x * x) * x;

        /// <summary>
        /// Compute sine of angle in degrees.
        /// </summary>
        /// <param name="x">Given angle.</param>
        public static float SinDeg(in float x)
        {
            var x_rad = DegreeToRadian(in x);
            var y = Sin(x_rad);
            return y;
        }

        /// <summary>
        /// Compute sine of angle in degrees.
        /// </summary>
        /// <param name="x">Given angle.</param>
        public static double SinDeg(in double x)
        {
            var x_rad = DegreeToRadian(in x);
            var y = Sin(x_rad);
            return y;
        }

        /// <summary>
        /// Compute cosine of angle in degrees.
        /// </summary>
        /// <param name="x">Given angle.</param>
        public static float CosDeg(in float x)
        {
            var x_rad = DegreeToRadian(in x);
            var y = Cos(x_rad);
            return y;
        }

        /// <summary>
        /// Compute cosine of angle in degrees.
        /// </summary>
        /// <param name="x">Given angle.</param>
        public static double CosDeg(in double x)
        {
            var x_rad = DegreeToRadian(in x);
            var y = Cos(x_rad);
            return y;
        }

        public static float RadianToDegree(in float rad)
        {
            var deg = 360f * (rad / TwoPI);
            return deg;
        }

        public static double RadianToDegree(in double rad)
        {
            var deg = 360d * (rad / TwoPI);
            return deg;
        }

        public static float DegreeToRadian(in float deg)
        {
            var rad = TwoPI * (deg / 360f);
            return rad;
        }

        public static double DegreeToRadian(in double deg)
        {
            var rad = TwoPI * (deg / 360d);
            return rad;
        }

        public static float NormalizeDegree(in float deg)
        {
            var d = deg % 360f;
            return d >= 0 ? d : d + 360f;
        }

        public static double NormalizeDegree(in double deg)
        {
            var d = deg % 360d;
            return d >= 0 ? d : d + 360d;
        }

        public static float Blerp(float c00, float c10, float c01, float c11, float tx, float ty)
        {
            static float Lerp(float s, float e, float t)
            {
                return s + (e - s) * t;
            }

            return Lerp(Lerp(c00, c10, tx), Lerp(c01, c11, tx), ty);
        }

        /// <summary>
        /// Interpolate one value. Based on https://stackoverflow.com/a/12838328/6818663
        /// </summary>
        /// <param name="x">Target value for interpolate (x value)</param>
        /// <param name="x0">Known point 0 (x value)</param>
        /// <param name="x1">Known point 1 (x value)</param>
        /// <param name="y0">Known point 0 (y value)</param>
        /// <param name="y1">Known point 1 (y value)</param>
        /// <returns>Interpolated value (y value)</returns>
        public static double Interpolate(double x, double x0, double x1, double y0, double y1)
        {
            if ((x1 - x0) == 0)
            {
                return (y0 + y1) / 2;
            }
            return y0 + (x - x0) * (y1 - y0) / (x1 - x0);
        }

        /// <summary>
        /// Получить значение функции методом кусочно-линейной интерполяции
        /// </summary>
        /// <param name="listValues">Набор точек функции для интерполяции</param>
        /// <returns>Функция для вычисления Y по входящему X</returns>
        public static Func<double, double> InterpolatePiecewiseLinear(IReadOnlyList<(double x, double y)> listValues)
        {
            if (listValues.Count < 2)
            {
                throw new Exception("Can not create interpolation function with less than 2 points");
            }

            List<double> points = new List<double>();
            List<double> values = new List<double>();
            foreach (var kvp in listValues)
            {
                points.Add(kvp.x);
                values.Add(kvp.y);
            }
            var interpolator = Numerics.Interpolate.Linear(points, values);

            return (x) => interpolator.Interpolate(x);
        }

        /// <summary>
        /// Получить значение функции методом CubicSpline интерполяции
        /// </summary>
        /// <param name="listValues">Набор точек функции для интерполяции</param>
        /// <returns>Функция для вычисления Y по входящему X</returns>
        public static Func<double, double> CubicSpline(IReadOnlyList<(double x, double y)> listValues)
        {
            if (listValues.Count < 2)
            {
                throw new Exception("Can not create interpolation function with less than 2 points");
            }

            List<double> points = new List<double>();
            List<double> values = new List<double>();
            foreach (var kvp in listValues)
            {
                points.Add(kvp.x);
                values.Add(kvp.y);
            }
            var interpolator = Numerics.Interpolate.CubicSpline(points, values);

            return (x) => interpolator.Interpolate(x);
        }

    }
}
