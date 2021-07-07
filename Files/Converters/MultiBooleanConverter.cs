﻿using System;

namespace Files.Converters
{
    internal class MultiBooleanConverter
    {
        public static Boolean OrConvert(bool a, bool b)
            => (a || b);

        public static Boolean AndConvert(bool a, bool b)
            => (a && b);

        public static Boolean AndNotConvert(bool a, bool b)
            => (a && !b);

        public static Boolean OrAndConvert(bool a, bool b, bool c)
            => (a || b) && c;
    }
}