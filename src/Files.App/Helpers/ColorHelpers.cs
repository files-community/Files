using System;
using Windows.UI;

namespace Files.App.Helpers
{
	internal static class ColorHelpers
	{
		/// <summary>
		/// Converts Hex to Windows.UI.Color.
		/// </summary>
		public static Color FromHex(string colorHex)
		{
			colorHex = colorHex.Replace("#", string.Empty);
			var r = (byte)Convert.ToUInt32(colorHex.Substring(0, 2), 16);
			var g = (byte)Convert.ToUInt32(colorHex.Substring(2, 2), 16);
			var b = (byte)Convert.ToUInt32(colorHex.Substring(4, 2), 16);

			return Color.FromArgb(255, r, g, b);
		}

		/// <summary>
		/// Converts Uint to Windows.UI.Color.
		/// </summary>
		public static Color FromUint(this uint value)
		{
			return Windows.UI.Color.FromArgb((byte)((value >> 24) & 0xFF),
					   (byte)((value >> 16) & 0xFF),
					   (byte)((value >> 8) & 0xFF),
					   (byte)(value & 0xFF));
		}

		/// <summary>
		/// Converts Windows.UI.Color to Uint.
		/// </summary>
		public static uint ToUint(this Color c)
		{
			return (uint)(((c.A << 24) | (c.R << 16) | (c.G << 8) | c.B) & 0xffffffffL);
		}
	}
}
