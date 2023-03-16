using System;
using System.Collections.Generic;
using System.Linq;

namespace ChatRouletteServer
{
    internal static class RandomExtension
    {
        public static (T, T) RandomPair<T>(this IEnumerable<T> list)
        {
            Random r = new Random();

            if (list != null && list.Count() > 1)
            {
                Int32 r1 = r.Next(list.Count());
                Int32 r2 = r.Next(list.Count());
                while (r1 == r2)
                    r2 = r.Next(list.Count());
                T t1 = list.ElementAt(r1);
                T t2 = list.ElementAt(r2);
                return (t1, t2);
            }
            return default;
        }
    }
}
