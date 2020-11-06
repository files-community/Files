using System;
using System.Collections.Generic;

namespace Files.Extensions
{
    internal static class Linq
    {
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

        /// <summary>
        /// Enumerates through <paramref name="collection"/> of elements and executes <paramref name="func"/> and returns <typeparamref name="T2"/>
        /// </summary>
        /// <typeparam name="T1">Element of <paramref name="collection"/></typeparam>
        /// <typeparam name="T2">The result of <paramref name="func"/></typeparam>
        /// <param name="collection">The collection to enumerate through</param>
        /// <param name="func">The func to take every element</param>
        /// <returns>Result of <paramref name="func"/> of every enumerated item</returns>
        internal static IEnumerable<T2> ForEach<T1, T2>(this IEnumerable<T1> collection, Func<T1, T2> func)
        {
            foreach (T1 value in collection)
                yield return func(value);
        }
    }
}
