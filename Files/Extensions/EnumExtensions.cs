using System;

namespace Files.Extensions
{
    internal class EnumExtensions
    {
        /// <summary>
        /// Gets <see cref="Enum"/> from <paramref name="value"/> within <typeparamref name="TEnum"/>
        /// </summary>
        /// <typeparam name="TEnum">The enum</typeparam>
        /// <param name="value">The enum value</param>
        /// <returns>Value of <typeparamref name="TEnum"/> from <paramref name="value"/></returns>
        internal static TEnum GetEnum<TEnum>(string value) where TEnum : Enum =>
            (TEnum)Enum.Parse(typeof(TEnum), value);

        /// <summary>
        /// Gets <see cref="Enum"/> from <paramref name="value"/> within <typeparamref name="TEnum"/>
        /// </summary>
        /// <typeparam name="TEnum">The enum</typeparam>
        /// <param name="value">The enum value</param>
        /// <returns>Value of <typeparamref name="TEnum"/> from <paramref name="value"/></returns>
        internal static TEnum GetEnum<TEnum>(int value) where TEnum : Enum =>
            (TEnum)Enum.ToObject(typeof(TEnum), value);
    }
}
