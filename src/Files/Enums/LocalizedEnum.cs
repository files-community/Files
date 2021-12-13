using Microsoft.Toolkit.Uwp;
using System;

namespace Files.Enums
{
    public class LocalizedEnum<T> where T : Enum
    {
        public string Name => $"{typeof(T).Name}_{Enum.GetName(typeof(T), Value)}".GetLocalized();
        public T Value { get; set; }

        public LocalizedEnum(T value)
        {
            Value = value;
        }
    }
}
