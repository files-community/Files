using System;
using Windows.UI;

namespace Files.Uwp.Helpers
{
    internal static class ColorHelpers
    {
        public static Color FromHex(string colorHex)
        {
            colorHex = colorHex.Replace("#", string.Empty);
            var r = (byte)Convert.ToUInt32(colorHex.Substring(0, 2), 16);
            var g = (byte)Convert.ToUInt32(colorHex.Substring(2, 2), 16);
            var b = (byte)Convert.ToUInt32(colorHex.Substring(4, 2), 16);

            return Color.FromArgb(255, r, g, b);
        }
    }
}
