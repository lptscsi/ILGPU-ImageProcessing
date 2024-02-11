using ILGPU;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace GpuTest.Utils
{
    public static class MinMax
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T min<T>(T a, T b) where T : struct, INumber<T> => a < b ? a : b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T max<T>(T a, T b) where T : struct, INumber<T> => a > b ? a : b;

        public static T max9<T>(ArrayView<T> array)
           where T : unmanaged, INumber<T>
        {
            T res = array[0];
            res = max(res, array[1]);
            res = max(res, array[2]);
            res = max(res, array[3]);
            res = max(res, array[4]);
            res = max(res, array[5]);
            res = max(res, array[6]);
            res = max(res, array[7]);
            res = max(res, array[8]);
            return res;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T max25<T>(ArrayView<T> array)
            where T : unmanaged, INumber<T>
        {
            T res = array[0];
            res = max(res, array[1]);
            res = max(res, array[2]);
            res = max(res, array[3]);
            res = max(res, array[4]);
            res = max(res, array[5]);
            res = max(res, array[6]);
            res = max(res, array[7]);
            res = max(res, array[8]);
            res = max(res, array[9]);
            res = max(res, array[10]);
            res = max(res, array[11]);
            res = max(res, array[12]);
            res = max(res, array[13]);
            res = max(res, array[14]);
            res = max(res, array[15]);
            res = max(res, array[16]);
            res = max(res, array[17]);
            res = max(res, array[18]);
            res = max(res, array[19]);
            res = max(res, array[20]);
            res = max(res, array[21]);
            res = max(res, array[22]);
            res = max(res, array[23]);
            res = max(res, array[24]);
            return res;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T max49<T>(ArrayView<T> array)
         where T : unmanaged, INumber<T>
        {
            T res = array[0];
            res = max(res, array[1]);
            res = max(res, array[2]);
            res = max(res, array[3]);
            res = max(res, array[4]);
            res = max(res, array[5]);
            res = max(res, array[6]);
            res = max(res, array[7]);
            res = max(res, array[8]);
            res = max(res, array[9]);
            res = max(res, array[10]);
            res = max(res, array[11]);
            res = max(res, array[12]);
            res = max(res, array[13]);
            res = max(res, array[14]);
            res = max(res, array[15]);
            res = max(res, array[16]);
            res = max(res, array[17]);
            res = max(res, array[18]);
            res = max(res, array[19]);
            res = max(res, array[20]);
            res = max(res, array[21]);
            res = max(res, array[22]);
            res = max(res, array[23]);
            res = max(res, array[24]);
            res = max(res, array[25]);
            res = max(res, array[26]);
            res = max(res, array[27]);
            res = max(res, array[28]);
            res = max(res, array[29]);
            res = max(res, array[30]);
            res = max(res, array[31]);
            res = max(res, array[32]);
            res = max(res, array[33]);
            res = max(res, array[34]);
            res = max(res, array[35]);
            res = max(res, array[36]);
            res = max(res, array[37]);
            res = max(res, array[38]);
            res = max(res, array[39]);
            res = max(res, array[40]);
            res = max(res, array[41]);
            res = max(res, array[42]);
            res = max(res, array[43]);
            res = max(res, array[44]);
            res = max(res, array[45]);
            res = max(res, array[46]);
            res = max(res, array[47]);
            res = max(res, array[48]);
            return res;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T max81<T>(ArrayView<T> array)
         where T : unmanaged, INumber<T>
        {
            T res = array[0];
            res = max(res, array[1]);
            res = max(res, array[2]);
            res = max(res, array[3]);
            res = max(res, array[4]);
            res = max(res, array[5]);
            res = max(res, array[6]);
            res = max(res, array[7]);
            res = max(res, array[8]);
            res = max(res, array[9]);
            res = max(res, array[10]);
            res = max(res, array[11]);
            res = max(res, array[12]);
            res = max(res, array[13]);
            res = max(res, array[14]);
            res = max(res, array[15]);
            res = max(res, array[16]);
            res = max(res, array[17]);
            res = max(res, array[18]);
            res = max(res, array[19]);
            res = max(res, array[20]);
            res = max(res, array[21]);
            res = max(res, array[22]);
            res = max(res, array[23]);
            res = max(res, array[24]);
            res = max(res, array[25]);
            res = max(res, array[26]);
            res = max(res, array[27]);
            res = max(res, array[28]);
            res = max(res, array[29]);
            res = max(res, array[30]);
            res = max(res, array[31]);
            res = max(res, array[32]);
            res = max(res, array[33]);
            res = max(res, array[34]);
            res = max(res, array[35]);
            res = max(res, array[36]);
            res = max(res, array[37]);
            res = max(res, array[38]);
            res = max(res, array[39]);
            res = max(res, array[40]);
            res = max(res, array[41]);
            res = max(res, array[42]);
            res = max(res, array[43]);
            res = max(res, array[44]);
            res = max(res, array[45]);
            res = max(res, array[46]);
            res = max(res, array[47]);
            res = max(res, array[48]);
            res = max(res, array[49]);
            res = max(res, array[50]);
            res = max(res, array[51]);
            res = max(res, array[52]);
            res = max(res, array[53]);
            res = max(res, array[54]);
            res = max(res, array[55]);
            res = max(res, array[56]);
            res = max(res, array[57]);
            res = max(res, array[58]);
            res = max(res, array[59]);
            res = max(res, array[60]);
            res = max(res, array[61]);
            res = max(res, array[62]);
            res = max(res, array[63]);
            res = max(res, array[64]);
            res = max(res, array[65]);
            res = max(res, array[66]);
            res = max(res, array[67]);
            res = max(res, array[68]);
            res = max(res, array[69]);
            res = max(res, array[70]);
            res = max(res, array[71]);
            res = max(res, array[72]);
            res = max(res, array[73]);
            res = max(res, array[74]);
            res = max(res, array[75]);
            res = max(res, array[76]);
            res = max(res, array[77]);
            res = max(res, array[78]);
            res = max(res, array[79]);
            res = max(res, array[80]);
            return res;
        }

        public static T min9<T>(ArrayView<T> array)
           where T : unmanaged, INumber<T>
        {
            T res = array[0];
            res = min(res, array[1]);
            res = min(res, array[2]);
            res = min(res, array[3]);
            res = min(res, array[4]);
            res = min(res, array[5]);
            res = min(res, array[6]);
            res = min(res, array[7]);
            res = min(res, array[8]);
            return res;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T min25<T>(ArrayView<T> array)
            where T : unmanaged, INumber<T>
        {
            T res = array[0];
            res = min(res, array[1]);
            res = min(res, array[2]);
            res = min(res, array[3]);
            res = min(res, array[4]);
            res = min(res, array[5]);
            res = min(res, array[6]);
            res = min(res, array[7]);
            res = min(res, array[8]);
            res = min(res, array[9]);
            res = min(res, array[10]);
            res = min(res, array[11]);
            res = min(res, array[12]);
            res = min(res, array[13]);
            res = min(res, array[14]);
            res = min(res, array[15]);
            res = min(res, array[16]);
            res = min(res, array[17]);
            res = min(res, array[18]);
            res = min(res, array[19]);
            res = min(res, array[20]);
            res = min(res, array[21]);
            res = min(res, array[22]);
            res = min(res, array[23]);
            res = min(res, array[24]);
            return res;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T min49<T>(ArrayView<T> array)
         where T : unmanaged, INumber<T>
        {
            T res = array[0];
            res = min(res, array[1]);
            res = min(res, array[2]);
            res = min(res, array[3]);
            res = min(res, array[4]);
            res = min(res, array[5]);
            res = min(res, array[6]);
            res = min(res, array[7]);
            res = min(res, array[8]);
            res = min(res, array[9]);
            res = min(res, array[10]);
            res = min(res, array[11]);
            res = min(res, array[12]);
            res = min(res, array[13]);
            res = min(res, array[14]);
            res = min(res, array[15]);
            res = min(res, array[16]);
            res = min(res, array[17]);
            res = min(res, array[18]);
            res = min(res, array[19]);
            res = min(res, array[20]);
            res = min(res, array[21]);
            res = min(res, array[22]);
            res = min(res, array[23]);
            res = min(res, array[24]);
            res = min(res, array[25]);
            res = min(res, array[26]);
            res = min(res, array[27]);
            res = min(res, array[28]);
            res = min(res, array[29]);
            res = min(res, array[30]);
            res = min(res, array[31]);
            res = min(res, array[32]);
            res = min(res, array[33]);
            res = min(res, array[34]);
            res = min(res, array[35]);
            res = min(res, array[36]);
            res = min(res, array[37]);
            res = min(res, array[38]);
            res = min(res, array[39]);
            res = min(res, array[40]);
            res = min(res, array[41]);
            res = min(res, array[42]);
            res = min(res, array[43]);
            res = min(res, array[44]);
            res = min(res, array[45]);
            res = min(res, array[46]);
            res = min(res, array[47]);
            res = min(res, array[48]);
            return res;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T min81<T>(ArrayView<T> array)
            where T : unmanaged, INumber<T>
        {
            T res = array[0];
            res = min(res, array[1]);
            res = min(res, array[2]);
            res = min(res, array[3]);
            res = min(res, array[4]);
            res = min(res, array[5]);
            res = min(res, array[6]);
            res = min(res, array[7]);
            res = min(res, array[8]);
            res = min(res, array[9]);
            res = min(res, array[10]);
            res = min(res, array[11]);
            res = min(res, array[12]);
            res = min(res, array[13]);
            res = min(res, array[14]);
            res = min(res, array[15]);
            res = min(res, array[16]);
            res = min(res, array[17]);
            res = min(res, array[18]);
            res = min(res, array[19]);
            res = min(res, array[20]);
            res = min(res, array[21]);
            res = min(res, array[22]);
            res = min(res, array[23]);
            res = min(res, array[24]);
            res = min(res, array[25]);
            res = min(res, array[26]);
            res = min(res, array[27]);
            res = min(res, array[28]);
            res = min(res, array[29]);
            res = min(res, array[30]);
            res = min(res, array[31]);
            res = min(res, array[32]);
            res = min(res, array[33]);
            res = min(res, array[34]);
            res = min(res, array[35]);
            res = min(res, array[36]);
            res = min(res, array[37]);
            res = min(res, array[38]);
            res = min(res, array[39]);
            res = min(res, array[40]);
            res = min(res, array[41]);
            res = min(res, array[42]);
            res = min(res, array[43]);
            res = min(res, array[44]);
            res = min(res, array[45]);
            res = min(res, array[46]);
            res = min(res, array[47]);
            res = min(res, array[48]);
            res = min(res, array[49]);
            res = min(res, array[50]);
            res = min(res, array[51]);
            res = min(res, array[52]);
            res = min(res, array[53]);
            res = min(res, array[54]);
            res = min(res, array[55]);
            res = min(res, array[56]);
            res = min(res, array[57]);
            res = min(res, array[58]);
            res = min(res, array[59]);
            res = min(res, array[60]);
            res = min(res, array[61]);
            res = min(res, array[62]);
            res = min(res, array[63]);
            res = min(res, array[64]);
            res = min(res, array[65]);
            res = min(res, array[66]);
            res = min(res, array[67]);
            res = min(res, array[68]);
            res = min(res, array[69]);
            res = min(res, array[70]);
            res = min(res, array[71]);
            res = min(res, array[72]);
            res = min(res, array[73]);
            res = min(res, array[74]);
            res = min(res, array[75]);
            res = min(res, array[76]);
            res = min(res, array[77]);
            res = min(res, array[78]);
            res = min(res, array[79]);
            res = min(res, array[80]);
            return res;
        }
    }
}
