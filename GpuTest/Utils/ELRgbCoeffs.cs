using System;

namespace GpuTest
{
    using int32_t = Int32;
    using int64_t = Int64;
    using uint8_t = Byte;

    /// <summary>
    /// RGB gain coefficient type
    /// </summary>
    public struct RgbCoeffs
    {
        public const int FractionBits = ELConstants.FractionBits;

        /// <summary>
        /// New object, with initialized all colors to ones in Q16.12
        /// </summary>
        public static RgbCoeffs One => new RgbCoeffs(1 << FractionBits, 1 << FractionBits, 1 << FractionBits);

        /// <summary>
        /// New object, with initialized all colors to zeroes
        /// </summary>
        public static RgbCoeffs Zero => new RgbCoeffs(0, 0, 0);

        /// <summary>
        /// Build object from fractional part on number:
        /// 0 will be equal to double 0.0., 255 (max value) - to double 1.0
        /// </summary>
        /// <param name="red">Red channel value</param>
        /// <param name="green">Green channel value</param>
        /// <param name="blue">Blue channel value</param>
        public RgbCoeffs(uint8_t red, uint8_t green, uint8_t blue)
        {
            double tempRed = (double)red * (double)(1 << FractionBits) / (double)uint8_t.MaxValue;
            double tempGreen = (double)green * (double)(1 << FractionBits) / (double)uint8_t.MaxValue;
            double tempBlue = (double)blue * (double)(1 << FractionBits) / (double)uint8_t.MaxValue;

            RedCoeff = (int32_t)tempRed;
            GreenCoeff = (int32_t)tempGreen;
            BlueCoeff = (int32_t)tempBlue;
        }

        /// <summary>
        /// Build object from raw internal values
        /// </summary>
        /// <param name="red">Raw internal red value</param>
        /// <param name="green">Raw internal green value</param>
        /// <param name="blue">Raw internal blue value</param>
        public RgbCoeffs(int32_t red, int32_t green, int32_t blue)
        {
            RedCoeff = red;
            GreenCoeff = green;
            BlueCoeff = blue;
        }

        /// <summary>
        /// Build object from floating values (double)
        /// </summary>
        /// <param name="red">Red channel value</param>
        /// <param name="green">Green channel value</param>
        /// <param name="blue">Blue channel value</param>
        public RgbCoeffs(double red, double green, double blue)
        {
            double tempRed = (double)red * (double)(1 << FractionBits);
            double tempGreen = (double)green * (double)(1 << FractionBits);
            double tempBlue = (double)blue * (double)(1 << FractionBits);

            RedCoeff = (int32_t)tempRed;
            GreenCoeff = (int32_t)tempGreen;
            BlueCoeff = (int32_t)tempBlue;
        }

        /// <summary>
        /// Red coefficient value
        /// </summary>
        public int32_t RedCoeff;

        /// <summary>
        /// Green coefficient value
        /// </summary>
        public int32_t GreenCoeff;

        /// <summary>
        /// Blue coefficient value
        /// </summary>
        public int32_t BlueCoeff;

        /// <summary>
        /// Red coefficient value as floating value
        /// </summary>
        public double RedCoeffDouble => ((double)RedCoeff) / (1 << FractionBits);

        /// <summary>
        /// Green coefficient value as floating value
        /// </summary>
        public double GreenCoeffDouble => ((double)GreenCoeff) / (1 << FractionBits);

        /// <summary>
        /// Blue coefficient value as floating value
        /// </summary>
        public double BlueCoeffDouble => ((double)BlueCoeff) / (1 << FractionBits);

        /// <summary>
        /// Multiply 2 fixed point numbers
        /// </summary>
        /// <param name="a">First fixed point number</param>
        /// <param name="b">Second fixed point number</param>
        /// <returns>Newly multiplied number</returns>
        public static int32_t FractionMul(int32_t a, int32_t b)
        {
            return (int32_t)((((int64_t)a) * b) >> FractionBits);
        }

        /// <summary>
        /// In-place Multiply current coefficients to another
        /// </summary>
        /// <param name="another">Another coefficients for multiplying with</param>
        /// <returns>This object for chained multiplication</returns>
        public RgbCoeffs Mul(RgbCoeffs another)
        {
            RedCoeff = FractionMul(RedCoeff, another.RedCoeff);
            GreenCoeff = FractionMul(GreenCoeff, another.GreenCoeff);
            BlueCoeff = FractionMul(BlueCoeff, another.BlueCoeff);

            return this;
        }

        /// <summary>
        /// Clone current object
        /// </summary>
        /// <returns>Newly created object</returns>
        public RgbCoeffs Clone()
        {
            return new RgbCoeffs(RedCoeff, GreenCoeff, BlueCoeff);
        }
    }
}
