using System;

namespace GpuTest
{
    using int32_t = Int32;
    using uint8_t = Byte;


    /// <summary>
    /// Constants for color manipulation calculations
    /// </summary>
    internal static class ELConstants
    {
        /// <summary>
        /// Number of bits in one byte
        /// </summary>
        internal const uint8_t BitsPerByte = 8;

        /// <summary>
        /// Number of fractional bits in Q16.12 format
        /// </summary>
        internal const int32_t FractionBits = 12;

    }
}
