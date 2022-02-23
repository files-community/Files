using Microsoft.Toolkit.Uwp;
using System;

namespace Files.Enums
{
    public class LocalizedEnum<T> where T : Enum
    {
        public string Name
        {
            get
            {
                var localized = $"{typeof(T).Name}_{Enum.GetName(typeof(T), Value)}".GetLocalized();
                if (string.IsNullOrEmpty(localized))
                {
                    localized = $"{Enum.GetName(typeof(T), Value)}".GetLocalized();
                }
                return localized;
            }
        }

        public T Value { get; set; }

        public LocalizedEnum(T value)
        {
            Value = value;
        }
    }
}
