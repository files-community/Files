using CommunityToolkit.WinUI.Helpers;
using System;
using Windows.UI;

namespace Files.App.Helpers
{
	internal static class ColorHelpers
	{
		/// <summary>
		/// Converts Hex to Windows.UI.Color.
		/// </summary>
		public static Color FromHex(string? colorHex)
		{
			if (string.IsNullOrWhiteSpace(colorHex))
				return Color.FromArgb(255, 255, 255, 255);

			colorHex = colorHex.Replace("#", string.Empty);

			var alphaOffset = colorHex.Length == 8 ? 2 : 0;

			var a = alphaOffset == 2 ? (byte)Convert.ToUInt32(colorHex.Substring(0, 2), 16) : (byte)255;
			var r = (byte)Convert.ToUInt32(colorHex.Substring(alphaOffset, 2), 16);
			var g = (byte)Convert.ToUInt32(colorHex.Substring(alphaOffset + 2, 2), 16);
			var b = (byte)Convert.ToUInt32(colorHex.Substring(alphaOffset + 4, 2), 16);

			return Color.FromArgb(a, r, g, b);
		}

		/// <summary>
		/// Converts Uint to Windows.UI.Color.
		/// </summary>
		public static Color FromUint(this uint value)
		{
			return Color.FromArgb((byte)((value >> 24) & 0xFF),
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

		/// <summary>
		/// Generates a random color and returns its Hex
		/// </summary>
		/// <returns></returns>
		public static string RandomColor()
		{
			var color = Color.FromArgb(
				255,
				(byte)Random.Shared.Next(0, 256),
				(byte)Random.Shared.Next(0, 256),
				(byte)Random.Shared.Next(0, 256));

			return color.ToHex();
		}
	}
}
