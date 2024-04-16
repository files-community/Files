using Microsoft.Toolkit.Uwp;
using System;

namespace Files.Uwp.Helpers
{
    public sealed class LocalizedEnumHelper<T> where T : Enum
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

        public LocalizedEnumHelper(T value)
        {
            Value = value;
        }
    }
}
