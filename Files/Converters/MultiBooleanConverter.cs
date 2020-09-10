using System;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Files.Converters
{
    internal class MultiBooleanConverter
    {
        public static Boolean Convert(bool a, bool b)
            => (a || b) ? true : false;
    }
}