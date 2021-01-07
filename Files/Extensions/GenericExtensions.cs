using System;
using System.Collections.Generic;

namespace Files.Extensions
{
    public static class GenericExtensions
    {
        /// <summary>
        /// Converts given <paramref name="value"/> to provided <typeparamref name="TOut"/>
        /// </summary>
        /// <typeparam name="TOut">The generic type to convert to</typeparam>
        /// <typeparam name="TIn">The type to convert from</typeparam>
        /// <param name="value">The value</param>
        /// <remarks>
        /// The <typeparamref name="TOut"/> must implement <see cref="IConvertible"/>
        /// </remarks>
        /// <returns>Converted <paramref name="value"/></returns>
        public static TOut ConvertValue<TSource, TOut>(this TSource value) =>
            (TOut)Convert.ChangeType(value, typeof(TOut));

        /// <summary>
        /// Compares two generic types
        /// </summary>
        /// <typeparam name="T">The type to compare</typeparam>
        /// <param name="value1">The first value</param>
        /// <param name="value2">The second value</param>
        /// <remarks>
        /// The <typeparamref name="T"/> must implement <see cref="IComparable{T}"/>
        /// </remarks>
        /// <returns><see cref="bool"/> true if both <typeparamref name="T"/> <paramref name="value1"/> and <typeparamref name="T"/> <paramref name="value2"/> are equal</returns>
        public static bool Equals<T>(this T value1, T value2) =>
            EqualityComparer<T>.Default.Equals(value1, value2);
    }
}
