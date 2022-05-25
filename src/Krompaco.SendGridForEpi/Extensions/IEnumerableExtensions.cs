namespace Krompaco.SendGridForEpi.Extensions;

public static class IEnumerableExtensions
{
    public static IEnumerable<IEnumerable<T>> SplitToBatches<T>(this IEnumerable<T> enumerable, int size = 1000)
    {
        T[]? temp = null;
        var i = 0;

        foreach (var item in enumerable)
        {
            temp ??= new T[size];

            temp[i++] = item;

            if (i != size)
            {
                continue;
            }

            yield return temp.Select(x => x);

            temp = null;
            i = 0;
        }

        if (temp != null && i > 0)
        {
            yield return temp.Take(i);
        }
    }
}
