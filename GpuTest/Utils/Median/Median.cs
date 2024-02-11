using ILGPU;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace GpuTest.Utils
{
    public static class Median
    {
        // general but much slower sorting (because it uses global memory, instead of registers)
        public static void Sort(ArrayView<float> arr)
        {
            bool swapped = true;
            int t = 0;
            float tmp;
            int cnt = arr.IntLength;

            while (swapped)
            {
                swapped = false;
                t++;
                for (int i = 0; i < cnt - t; i++)
                {
                    if (arr[i] > arr[i + 1])
                    {
                        tmp = arr[i];
                        arr[i] = arr[i + 1];
                        arr[i + 1] = tmp;
                        swapped = true;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static T min<T>(T a, T b) where T : struct, INumber<T> => a < b ? a : b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static T max<T>(T a, T b) where T : struct, INumber<T> => a > b ? a : b;

        public static T median3<T>(ArrayView<T> array)
           where T : unmanaged, INumber<T>
        {
            return max(min(array[0], array[1]), min(array[2], max(array[0], array[1])));
        }

        public static float median4(ArrayView<float> array)
        {
            float val1 = max(min(array[0], array[1]), min(array[2], array[3]));
            float val2 = min(max(array[0], array[1]), max(array[2], array[3]));

            return (val1 + val2) / 2f;
        }

        public static T median5<T>(ArrayView<T> array)
            where T : unmanaged, INumber<T>
        {
            T tmp = min(array[0], array[1]);
            array[1] = max(array[0], array[1]);
            array[0] = tmp;
            tmp = min(array[3], array[4]);
            array[4] = max(array[3], array[4]);
            array[3] = max(array[0], tmp);
            array[1] = min(array[1], array[4]);
            tmp = min(array[1], array[2]);
            array[2] = max(array[1], array[2]);
            array[1] = tmp;
            tmp = min(array[2], array[3]);
            return max(array[1], tmp);
        }

        public static T median7<T>(ArrayView<T> array)
         where T : unmanaged, INumber<T>
        {
            T tmp = min(array[0], array[5]);
            array[5] = max(array[0], array[5]);
            array[0] = tmp;
            tmp = min(array[0], array[3]);
            array[3] = max(array[0], array[3]);
            array[0] = tmp;
            tmp = min(array[1], array[6]);
            array[6] = max(array[1], array[6]);
            array[1] = tmp;
            tmp = min(array[2], array[4]);
            array[4] = max(array[2], array[4]);
            array[2] = tmp;
            array[1] = max(array[0], array[1]);
            tmp = min(array[3], array[5]);
            array[5] = max(array[3], array[5]);
            array[3] = tmp;
            tmp = min(array[2], array[6]);
            array[6] = max(array[2], array[6]);
            array[3] = max(tmp, array[3]);
            array[3] = min(array[3], array[6]);
            tmp = min(array[4], array[5]);
            array[4] = max(array[1], tmp);
            tmp = min(array[1], tmp);
            array[3] = max(tmp, array[3]);
            return min(array[3], array[4]);
        }

        public static T median9<T>(ArrayView<T> array)
         where T : unmanaged, INumber<T>
        {
            T tmp = min(array[1], array[2]);
            array[2] = max(array[1], array[2]);
            array[1] = tmp;
            tmp = min(array[4], array[5]);
            array[5] = max(array[4], array[5]);
            array[4] = tmp;
            tmp = min(array[7], array[8]);
            array[8] = max(array[7], array[8]);
            array[7] = tmp;
            tmp = min(array[0], array[1]);
            array[1] = max(array[0], array[1]);
            array[0] = tmp;
            tmp = min(array[3], array[4]);
            array[4] = max(array[3], array[4]);
            array[3] = tmp;
            tmp = min(array[6], array[7]);
            array[7] = max(array[6], array[7]);
            array[6] = tmp;
            tmp = min(array[1], array[2]);
            array[2] = max(array[1], array[2]);
            array[1] = tmp;
            tmp = min(array[4], array[5]);
            array[5] = max(array[4], array[5]);
            array[4] = tmp;
            tmp = min(array[7], array[8]);
            array[8] = max(array[7], array[8]);
            array[3] = max(array[0], array[3]);
            array[5] = min(array[5], array[8]);
            array[7] = max(array[4], tmp);
            tmp = min(array[4], tmp);
            array[6] = max(array[3], array[6]);
            array[4] = max(array[1], tmp);
            array[2] = min(array[2], array[5]);
            array[4] = min(array[4], array[7]);
            tmp = min(array[4], array[2]);
            array[2] = max(array[4], array[2]);
            array[4] = max(array[6], tmp);
            return min(array[4], array[2]);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T median13<T>(ArrayView<T> array)
           where T : unmanaged, INumber<T>
        {
            T tmp = min(array[1], array[7]);
            array[7] = max(array[1], array[7]);
            array[1] = tmp;
            tmp = min(array[9], array[11]);
            array[11] = max(array[9], array[11]);
            array[9] = tmp;
            tmp = min(array[3], array[4]);
            array[4] = max(array[3], array[4]);
            array[3] = tmp;
            tmp = min(array[5], array[8]);
            array[8] = max(array[5], array[8]);
            array[5] = tmp;
            tmp = min(array[0], array[12]);
            array[12] = max(array[0], array[12]);
            array[0] = tmp;
            tmp = min(array[2], array[6]);
            array[6] = max(array[2], array[6]);
            array[2] = tmp;
            tmp = min(array[0], array[1]);
            array[1] = max(array[0], array[1]);
            array[0] = tmp;
            tmp = min(array[2], array[3]);
            array[3] = max(array[2], array[3]);
            array[2] = tmp;
            tmp = min(array[4], array[6]);
            array[6] = max(array[4], array[6]);
            array[4] = tmp;
            tmp = min(array[8], array[11]);
            array[11] = max(array[8], array[11]);
            array[8] = tmp;
            tmp = min(array[7], array[12]);
            array[12] = max(array[7], array[12]);
            array[7] = tmp;
            tmp = min(array[5], array[9]);
            array[9] = max(array[5], array[9]);
            array[5] = tmp;
            tmp = min(array[0], array[2]);
            array[2] = max(array[0], array[2]);
            array[0] = tmp;
            tmp = min(array[3], array[7]);
            array[7] = max(array[3], array[7]);
            array[3] = tmp;
            tmp = min(array[10], array[11]);
            array[11] = max(array[10], array[11]);
            array[10] = tmp;
            tmp = min(array[1], array[4]);
            array[4] = max(array[1], array[4]);
            array[1] = tmp;
            tmp = min(array[6], array[12]);
            array[12] = max(array[6], array[12]);
            array[6] = tmp;
            tmp = min(array[7], array[8]);
            array[8] = max(array[7], array[8]);
            array[7] = tmp;
            array[11] = min(array[11], array[12]);
            tmp = min(array[4], array[9]);
            array[9] = max(array[4], array[9]);
            array[4] = tmp;
            tmp = min(array[6], array[10]);
            array[10] = max(array[6], array[10]);
            array[6] = tmp;
            tmp = min(array[3], array[4]);
            array[4] = max(array[3], array[4]);
            array[3] = tmp;
            tmp = min(array[5], array[6]);
            array[6] = max(array[5], array[6]);
            array[5] = tmp;
            array[8] = min(array[8], array[9]);
            array[10] = min(array[10], array[11]);
            tmp = min(array[1], array[7]);
            array[7] = max(array[1], array[7]);
            array[1] = tmp;
            tmp = min(array[2], array[6]);
            array[6] = max(array[2], array[6]);
            array[2] = tmp;
            array[3] = max(array[1], array[3]);
            tmp = min(array[4], array[7]);
            array[7] = max(array[4], array[7]);
            array[4] = tmp;
            array[8] = min(array[8], array[10]);
            array[5] = max(array[0], array[5]);
            array[5] = max(array[2], array[5]);
            tmp = min(array[6], array[8]);
            array[8] = max(array[6], array[8]);
            array[5] = max(array[3], array[5]);
            array[7] = min(array[7], array[8]);
            array[6] = max(array[4], tmp);
            tmp = min(array[4], tmp);
            array[5] = max(tmp, array[5]);
            array[6] = min(array[6], array[7]);
            return max(array[5], array[6]);
        }
    }
}
