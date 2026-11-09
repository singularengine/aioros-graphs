namespace Aioros.Graphs.Arrays;

internal interface IOrder<in T>
{
    int Compare(T a, T b);
}

internal readonly struct AscendingDefined : IOrder<double>
{
    public int Compare(double a, double b) => Order(a, b);

    public static int Order(double a, double b)
    {
        var undefined = (double.IsNaN(a) ? 1 : 0) - (double.IsNaN(b) ? 1 : 0);
        return undefined != 0 ? undefined : a < b ? -1 : a > b ? 1 : 0;
    }
}

internal readonly struct AscendingByValue(double[] values) : IOrder<int>
{
    public int Compare(int a, int b) => AscendingDefined.Order(values[a], values[b]);
}

internal static class QuickSelect
{
    public static void Select<T, TOrder>(Span<T> array, int k, int left, int right, TOrder order)
        where TOrder : struct, IOrder<T>
    {
        left = Math.Max(0, left);
        right = Math.Min(array.Length - 1, right);
        if (!(left <= k && k <= right))
        {
            return;
        }

        while (right > left)
        {
            if (right - left > 600)
            {
                Narrow(array, k, left, right, order);
            }

            var j = Partition(array, k, left, right, order);
            if (j <= k)
            {
                left = j + 1;
            }

            if (k <= j)
            {
                right = j - 1;
            }
        }
    }

    private static int Partition<T, TOrder>(Span<T> array, int k, int left, int right, TOrder order)
        where TOrder : struct, IOrder<T>
    {
        var t = array[k];
        var i = left;
        var j = right;
        Swap(array, left, k);
        if (order.Compare(array[right], t) > 0)
        {
            Swap(array, left, right);
        }

        while (i < j)
        {
            Swap(array, i, j);
            ++i;
            --j;
            while (order.Compare(array[i], t) < 0)
            {
                ++i;
            }

            while (order.Compare(array[j], t) > 0)
            {
                --j;
            }
        }

        if (order.Compare(array[left], t) == 0)
        {
            Swap(array, left, j);
            return j;
        }

        Swap(array, j + 1, right);
        return j + 1;
    }

    private static void Narrow<T, TOrder>(Span<T> array, int k, int left, int right, TOrder order)
        where TOrder : struct, IOrder<T>
    {
        double n = right - left + 1;
        double m = k - left + 1;
        var z = Math.Log(n);
        var s = 0.5 * Math.Exp(2 * z / 3);
        var sd = 0.5 * Math.Sqrt(z * s * (n - s) / n) * (m - n / 2 < 0 ? -1 : 1);
        var newLeft = Math.Max(left, (int)Math.Floor(k - m * s / n + sd));
        var newRight = Math.Min(right, (int)Math.Floor(k + (n - m) * s / n + sd));
        Select(array, k, newLeft, newRight, order);
    }

    private static void Swap<T>(Span<T> array, int i, int j) => (array[i], array[j]) = (array[j], array[i]);
}
