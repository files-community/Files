using System;

namespace Files.Converters
{
    internal class MultiBooleanConverter
    {
        public static Boolean Convert(bool a, bool b)
            => (a || b) ? true : false;
    }
}