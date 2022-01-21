using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Files.Shared
{
    public static class Extensions
    {
        

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