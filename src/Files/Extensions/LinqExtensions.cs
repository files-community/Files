using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Files.Extensions
{
    internal static class LinqExtensions
    {
        /// <summary>
        /// Determines whether <paramref name="enumerable"/> is empty or not.
        /// <br/><br/>
        /// Remarks:
        /// <br/>
        /// This function is faster than enumerable.Count == 0 since it'll only iterate one element instead of all elements.
        /// <br/>
        /// This function is null-safe.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static bool IsEmpty<T>(this IEnumerable<T> enumerable) => enumerable == null || !enumerable.Any();

        /// <summary>
        /// Enumerates through <see cref="IEnumerable{T}"/> of elements and executes <paramref name="action"/>
        /// </summary>
        /// <typeparam name="T">Element of <paramref name="collection"/></typeparam>
        /// <param name="collection">The collection to enumerate through</param>
        /// <param name="action">The action to take every element</param>
        internal static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (T value in collection)
                action(value);
        }

        internal static IList<T> AddSorted<T>(this IList<T> list, T item) where T : IComparable<T>
        {
            if (list.Count == 0)
            {
                list.Add(item);
                return list;
            }
            if (list[list.Count - 1].CompareTo(item) <= 0)
            {
                list.Add(item);
                return list;
            }
            if (list[0].CompareTo(item) >= 0)
            {
                list.Insert(0, item);
                return list;
            }
            int index = list.ToList().BinarySearch(item);
            if (index < 0)
            {
                index = ~index;
            }
            list.Insert(index, item);
            return list;
        }

        /// <summary>
        /// Removes all elements from the specified index to the end of the list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="index"></param>

        public static List<T> RemoveFrom<T>(this List<T> list, int index)
        {
            if (list.Count == 0)
            {
                return list;
            }

            var res = new List<T>(list);
            index = Math.Min(index, list.Count);
            var end = res.Count - index;
            res.RemoveRange(index, end);
            return res;
        }
    }
}