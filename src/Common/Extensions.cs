using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Files.Common
{
    public static class Extensions
    {
        public static IEnumerable<TSource> ExceptBy<TSource, TKey>(
            this IEnumerable<TSource> source,
            IEnumerable<TSource> other,
            Func<TSource, TKey> keySelector)
        {
            var set = new HashSet<TKey>(other.Select(keySelector));
            foreach (var item in source)
            {
                if (set.Add(keySelector(item)))
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> items, Func<T, TKey> property)
        {
            return items.GroupBy(property).Select(x => x.First());
        }

        public static async Task<IEnumerable<T>> WhereAsync<T>(this IEnumerable<T> source, Func<T, Task<bool>> predicate)
        {
            var results = await Task.WhenAll(source.Select(async x => (x, await predicate(x))));
            return results.Where(x => x.Item2).Select(x => x.Item1);
        }

        public static IEnumerable<TSource> IntersectBy<TSource, TKey>(
            this IEnumerable<TSource> source,
            IEnumerable<TSource> other,
            Func<TSource, TKey> keySelector)
        {
            return source.Join(other.Select(keySelector), keySelector, id => id, (o, id) => o);
        }

        public static TOut Get<TOut, TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TOut defaultValue = default)
        {
            // If dictionary is null or key is invalid, return default.
            if (dictionary == null || key == null)
            {
                return defaultValue;
            }

            // If setting doesn't exist, create it.
            if (!dictionary.ContainsKey(key))
            {
                dictionary[key] = (TValue)(object)defaultValue;
            }

            return (TOut)(object)dictionary[key];
        }

        public static async Task<TOut> Get<TOut, TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Task<TOut> defaultValue = default)
        {
            // If dictionary is null or key is invalid, return default.
            if (dictionary == null || key == null)
            {
                return await defaultValue;
            }

            // If setting doesn't exist, create it.
            if (!dictionary.ContainsKey(key))
            {
                dictionary[key] = (TValue)(object)(await defaultValue);
            }

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

        public static async Task WithTimeoutAsync(this Task task,
            TimeSpan timeout)
        {
            if (task == await Task.WhenAny(task, Task.Delay(timeout)))
            {
                await task;
            }
        }

        public static async Task<T> WithTimeoutAsync<T>(this Task<T> task,
            TimeSpan timeout)
        {
            if (task == await Task.WhenAny(task, Task.Delay(timeout)))
            {
                return await task;
            }
            return default;
        }

        public static bool IgnoreExceptions(Action action, Logger logger = null)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                logger?.Info(ex, ex.Message);
                return false;
            }
        }

        public static async Task<bool> IgnoreExceptions(Func<Task> action, Logger logger = null)
        {
            try
            {
                await action();
                return true;
            }
            catch (Exception ex)
            {
                logger?.Info(ex, ex.Message);
                return false;
            }
        }

        public static T IgnoreExceptions<T>(Func<T> action, Logger logger = null)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                logger?.Info(ex, ex.Message);
                return default;
            }
        }

        public static async Task<T> IgnoreExceptions<T>(Func<Task<T>> action, Logger logger = null)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                logger?.Info(ex, ex.Message);
                return default;
            }
        }

        public static string GetDescription<T>(this T enumValue) where T : Enum
        {
            var description = enumValue.ToString();
            var fieldInfo = enumValue.GetType().GetField(enumValue.ToString());

            if (fieldInfo != null)
            {
                var attrs = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), true);
                if (attrs != null && attrs.Length > 0)
                {
                    description = ((DescriptionAttribute)attrs[0]).Description;
                }
            }

            return description;
        }

        public static T GetValueFromDescription<T>(string description) where T : Enum
        {
            foreach (var field in typeof(T).GetFields())
            {
                if (Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
                {
                    if (attribute.Description == description)
                    {
                        return (T)field.GetValue(null);
                    }
                }
                else
                {
                    if (field.Name == description)
                    {
                        return (T)field.GetValue(null);
                    }
                }
            }

            return default(T);
        }
    }
}