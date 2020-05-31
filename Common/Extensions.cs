using System;
using System.Collections.Generic;

namespace Files.Common
{
    public static class Extensions
    {
        public static TOut Get<TOut, TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TOut defaultValue = default(TOut))
        {
            // If setting doesn't exist, create it.
            if (!dictionary.ContainsKey(key))
                dictionary[key] = (TValue)(object)defaultValue;

            return (TOut)(object)dictionary[key];
        }

        public static DateTime ToDateTime(this System.Runtime.InteropServices.ComTypes.FILETIME time)
        {
            ulong high = (ulong)time.dwHighDateTime;
            uint low = (uint)time.dwLowDateTime;
            long fileTime = (long)((high << 32) + low);
            try
            {
                return DateTime.FromFileTimeUtc(fileTime);
            }
            catch
            {
                return DateTime.FromFileTimeUtc(0xFFFFFFFF);
            }
        }
    }
}
