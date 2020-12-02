namespace System.Collections.Generic
{
    public static class ListEx
    {
        public static int BinarySearch<T>(this IList<T> list, int start, int size, System.Func<T, int> compare)
        {
            UnityEngine.Assertions.Assert.IsTrue(start >= 0);
            UnityEngine.Assertions.Assert.IsTrue(size >= 1);
            int end = start + size - 1;
            while (start <= end)
            {
                int mid = (start + end) / 2;
                int c = compare(list[mid]);
                if (c == 0)
                {
                    return mid;
                }
                else if (c < 0)
                {
                    end = mid - 1;
                }
                else
                {
                    start = mid + 1;
                }
            }
            return -start;
        }
    }
}
