﻿using ILGPU;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace GpuTest.Utils
{
    public static class Median25
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static T min<T>(T a, T b) where T : struct, INumber<T> => a < b ? a : b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static T max<T>(T a, T b) where T : struct, INumber<T> => a > b ? a : b;


        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T median<T>(ArrayView<T> array)
            where T : unmanaged, INumber<T>
        {
            T tmp = min(array[0], array[1]);
            array[1] = max(array[0], array[1]);
            array[0] = tmp;
            tmp = min(array[3], array[4]);
            array[4] = max(array[3], array[4]);
            array[3] = tmp;
            tmp = min(array[2], array[4]);
            array[4] = max(array[2], array[4]);
            array[2] = min(tmp, array[3]);
            array[3] = max(tmp, array[3]);
            tmp = min(array[6], array[7]);
            array[7] = max(array[6], array[7]);
            array[6] = tmp;
            tmp = min(array[5], array[7]);
            array[7] = max(array[5], array[7]);
            array[5] = min(tmp, array[6]);
            array[6] = max(tmp, array[6]);
            tmp = min(array[9], array[10]);
            array[10] = max(array[9], array[10]);
            array[9] = tmp;
            tmp = min(array[8], array[10]);
            array[10] = max(array[8], array[10]);
            array[8] = min(tmp, array[9]);
            array[9] = max(tmp, array[9]);
            tmp = min(array[12], array[13]);
            array[13] = max(array[12], array[13]);
            array[12] = tmp;
            tmp = min(array[11], array[13]);
            array[13] = max(array[11], array[13]);
            array[11] = min(tmp, array[12]);
            array[12] = max(tmp, array[12]);
            tmp = min(array[15], array[16]);
            array[16] = max(array[15], array[16]);
            array[15] = tmp;
            tmp = min(array[14], array[16]);
            array[16] = max(array[14], array[16]);
            array[14] = min(tmp, array[15]);
            array[15] = max(tmp, array[15]);
            tmp = min(array[18], array[19]);
            array[19] = max(array[18], array[19]);
            array[18] = tmp;
            tmp = min(array[17], array[19]);
            array[19] = max(array[17], array[19]);
            array[17] = min(tmp, array[18]);
            array[18] = max(tmp, array[18]);
            tmp = min(array[21], array[22]);
            array[22] = max(array[21], array[22]);
            array[21] = tmp;
            tmp = min(array[20], array[22]);
            array[22] = max(array[20], array[22]);
            array[20] = min(tmp, array[21]);
            array[21] = max(tmp, array[21]);
            tmp = min(array[23], array[24]);
            array[24] = max(array[23], array[24]);
            array[23] = tmp;
            tmp = min(array[2], array[5]);
            array[5] = max(array[2], array[5]);
            array[2] = tmp;
            tmp = min(array[3], array[6]);
            array[6] = max(array[3], array[6]);
            array[3] = tmp;
            tmp = min(array[0], array[6]);
            array[6] = max(array[0], array[6]);
            array[0] = min(tmp, array[3]);
            array[3] = max(tmp, array[3]);
            tmp = min(array[4], array[7]);
            array[7] = max(array[4], array[7]);
            array[4] = tmp;
            tmp = min(array[1], array[7]);
            array[7] = max(array[1], array[7]);
            array[1] = min(tmp, array[4]);
            array[4] = max(tmp, array[4]);
            tmp = min(array[11], array[14]);
            array[14] = max(array[11], array[14]);
            array[11] = tmp;
            tmp = min(array[8], array[14]);
            array[14] = max(array[8], array[14]);
            array[8] = min(tmp, array[11]);
            array[11] = max(tmp, array[11]);
            tmp = min(array[12], array[15]);
            array[15] = max(array[12], array[15]);
            array[12] = tmp;
            tmp = min(array[9], array[15]);
            array[15] = max(array[9], array[15]);
            array[9] = min(tmp, array[12]);
            array[12] = max(tmp, array[12]);
            tmp = min(array[13], array[16]);
            array[16] = max(array[13], array[16]);
            array[13] = tmp;
            tmp = min(array[10], array[16]);
            array[16] = max(array[10], array[16]);
            array[10] = min(tmp, array[13]);
            array[13] = max(tmp, array[13]);
            tmp = min(array[20], array[23]);
            array[23] = max(array[20], array[23]);
            array[20] = tmp;
            tmp = min(array[17], array[23]);
            array[23] = max(array[17], array[23]);
            array[17] = min(tmp, array[20]);
            array[20] = max(tmp, array[20]);
            tmp = min(array[21], array[24]);
            array[24] = max(array[21], array[24]);
            array[21] = tmp;
            tmp = min(array[18], array[24]);
            array[24] = max(array[18], array[24]);
            array[18] = min(tmp, array[21]);
            array[21] = max(tmp, array[21]);
            tmp = min(array[19], array[22]);
            array[22] = max(array[19], array[22]);
            array[19] = tmp;
            array[17] = max(array[8], array[17]);
            tmp = min(array[9], array[18]);
            array[18] = max(array[9], array[18]);
            array[9] = tmp;
            tmp = min(array[0], array[18]);
            array[18] = max(array[0], array[18]);
            array[9] = max(tmp, array[9]);
            tmp = min(array[10], array[19]);
            array[19] = max(array[10], array[19]);
            array[10] = tmp;
            tmp = min(array[1], array[19]);
            array[19] = max(array[1], array[19]);
            array[1] = min(tmp, array[10]);
            array[10] = max(tmp, array[10]);
            tmp = min(array[11], array[20]);
            array[20] = max(array[11], array[20]);
            array[11] = tmp;
            tmp = min(array[2], array[20]);
            array[20] = max(array[2], array[20]);
            array[11] = max(tmp, array[11]);
            tmp = min(array[12], array[21]);
            array[21] = max(array[12], array[21]);
            array[12] = tmp;
            tmp = min(array[3], array[21]);
            array[21] = max(array[3], array[21]);
            array[3] = min(tmp, array[12]);
            array[12] = max(tmp, array[12]);
            tmp = min(array[13], array[22]);
            array[22] = max(array[13], array[22]);
            array[4] = min(array[4], array[22]);
            array[13] = max(array[4], tmp);
            tmp = min(array[4], tmp);
            array[4] = tmp;
            tmp = min(array[14], array[23]);
            array[23] = max(array[14], array[23]);
            array[14] = tmp;
            tmp = min(array[5], array[23]);
            array[23] = max(array[5], array[23]);
            array[5] = min(tmp, array[14]);
            array[14] = max(tmp, array[14]);
            tmp = min(array[15], array[24]);
            array[24] = max(array[15], array[24]);
            array[15] = tmp;
            array[6] = min(array[6], array[24]);
            tmp = min(array[6], array[15]);
            array[15] = max(array[6], array[15]);
            array[6] = tmp;
            tmp = min(array[7], array[16]);
            array[7] = min(tmp, array[19]);
            tmp = min(array[13], array[21]);
            array[15] = min(array[15], array[23]);
            tmp = min(array[7], tmp);
            array[7] = min(tmp, array[15]);
            array[9] = max(array[1], array[9]);
            array[11] = max(array[3], array[11]);
            array[17] = max(array[5], array[17]);
            array[17] = max(array[11], array[17]);
            array[17] = max(array[9], array[17]);
            tmp = min(array[4], array[10]);
            array[10] = max(array[4], array[10]);
            array[4] = tmp;
            tmp = min(array[6], array[12]);
            array[12] = max(array[6], array[12]);
            array[6] = tmp;
            tmp = min(array[7], array[14]);
            array[14] = max(array[7], array[14]);
            array[7] = tmp;
            tmp = min(array[4], array[6]);
            array[6] = max(array[4], array[6]);
            array[7] = max(tmp, array[7]);
            tmp = min(array[12], array[14]);
            array[14] = max(array[12], array[14]);
            array[12] = tmp;
            array[10] = min(array[10], array[14]);
            tmp = min(array[6], array[7]);
            array[7] = max(array[6], array[7]);
            array[6] = tmp;
            tmp = min(array[10], array[12]);
            array[12] = max(array[10], array[12]);
            array[10] = max(array[6], tmp);
            tmp = min(array[6], tmp);
            array[17] = max(tmp, array[17]);
            tmp = min(array[12], array[17]);
            array[17] = max(array[12], array[17]);
            array[12] = tmp;
            array[7] = min(array[7], array[17]);
            tmp = min(array[7], array[10]);
            array[10] = max(array[7], array[10]);
            array[7] = tmp;
            tmp = min(array[12], array[18]);
            array[18] = max(array[12], array[18]);
            array[12] = max(array[7], tmp);
            array[10] = min(array[10], array[18]);
            tmp = min(array[12], array[20]);
            array[20] = max(array[12], array[20]);
            array[12] = tmp;
            tmp = min(array[10], array[20]);
            return max(tmp, array[12]);
        }
    }
}