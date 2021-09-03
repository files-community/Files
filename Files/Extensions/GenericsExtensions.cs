using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Extensions
{
    public static class GenericsExtensions
    {
        /// <summary>
        /// Determines whether <paramref name="value"/> is null
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns><see cref="bool"/> if <paramref name="value"/> is null, otherwise false</returns>
        public static bool IsNull<T>(this T value) => GenericEquals<T>(value, default);

        /// <summary>
        /// Compares two generic types
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value1">The first value</param>
        /// <param name="value2">The second value</param>
        /// <returns><see cref="bool"/> true if both <typeparamref name="T"/> <paramref name="value1"/> and <typeparamref name="T"/> <paramref name="value2"/> are equal, otherwise false</returns>
        public static bool GenericEquals<T>(this T value1, T value2) =>
            EqualityComparer<T>.Default.Equals(value1, value2);
    }
}
