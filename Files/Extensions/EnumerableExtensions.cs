using System.Collections.Generic;

namespace Files.Extensions
{
    internal static class EnumerableExtensions
    {
        /// <summary>
        /// Creates <see cref="List{T}"/> and returns <see cref="IEnumerable{T}"/> with provided <paramref name="item"/>
        /// </summary>
        /// <typeparam name="T">The item type</typeparam>
        /// <param name="item">The item</param>
        /// <returns><see cref="IEnumerable{T}"/> with <paramref name="item"/></returns>
        internal static IEnumerable<T> CreateEnumerable<T>(this T item) =>
            new List<T>() { item };
    }
}