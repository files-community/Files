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
    }
}
