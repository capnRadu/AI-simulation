using System;
using System.Collections.Generic;
using System.Linq;

public static class ListExtensions
{
    static Random rng;

    public static IList<T> Shuffle<T>(this IList<T> list)
    {
        rng ??= new Random();

        int count = list.Count;

        while (count > 1)
        {
            --count;
            int index = rng.Next(count + 1);
            (list[index], list[count]) = (list[count], list[index]);
        }

        return list;
    }
}