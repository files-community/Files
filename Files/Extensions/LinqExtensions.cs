using System;
using System.Collections.Generic;

namespace Files.Extensions
{
    internal static class LinqExtensions
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
    }
}