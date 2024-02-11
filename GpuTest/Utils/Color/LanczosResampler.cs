using ILGPU.Algorithms;
using System;

namespace GpuTest.Utils.Color
{

    /// <summary>
    /// The function implements the Lanczos kernel algorithm as described on
    /// <see href="https://en.wikipedia.org/wiki/Lanczos_resampling#Algorithm">Wikipedia</see>.
    /// </summary>
    public readonly struct LanczosResampler
    {
        /// <summary>
        /// The epsilon value for comparing floating point numbers.
        /// </summary>
        public static readonly float Epsilon = 0.001F;

        /// <summary>
        /// Implements the Lanczos kernel algorithm with a radius of 2.
        /// </summary>
        public static readonly LanczosResampler Lanczos2 = new(2);

        /// <summary>
        /// Implements the Lanczos kernel algorithm with a radius of 3.
        /// </summary>
        public static readonly LanczosResampler Lanczos3 = new(3);

        /// <summary>
        /// Implements the Lanczos kernel algorithm with a radius of 5.
        /// </summary>
        public static readonly LanczosResampler Lanczos5 = new(5);

        /// <summary>
        /// Implements the Lanczos kernel algorithm with a radius of 8.
        /// </summary>
        public static readonly LanczosResampler Lanczos8 = new(8);

        /// <summary>
        /// Initializes a new instance of the <see cref="LanczosResampler"/> struct.
        /// </summary>
        /// <param name="radius">The sampling radius.</param>
        public LanczosResampler(float radius) => this.Radius = radius;

        /// <inheritdoc/>
        public float Radius { get; }

        private float SinC(float f)
        {
            if (XMath.Abs(f) > Epsilon)
            {
                f *= XMath.PI;
                float result = XMath.Sin(f) / f;
                return MathF.Abs(result) < Epsilon ? 0F : result;
            }

            return 1F;
        }

        /// <inheritdoc/>
        public float GetValue(float x)
        {
            if (x < 0F)
            {
                x = -x;
            }

            float radius = this.Radius;
            if (x < radius)
            {
                return SinC(x) * SinC(x / radius);
            }

            return 0F;
        }
    }
}
