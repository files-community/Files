// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.Helpers;
using System.Globalization;
using Windows.UI;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides static helper for color value.
	/// </summary>
	internal static class ColorHelper
	{
		private const int COLOR_LENGTH = 7;
		private const int COLOR_LENGTH_INCLUDING_ALPHA = 9;

		private static readonly Color unknownTagColor = Color.FromArgb(255, 0x9E, 0xA3, 0xA1);

		/// <summary>
		/// Converts Hex to Windows.UI.Color.
		/// </summary>
		public static Color FromHex(string? colorHex)
		{
			// If Hex string is invalid, return Unknown Tag's color
			if (string.IsNullOrWhiteSpace(colorHex) ||
				(colorHex.Length != COLOR_LENGTH && colorHex.Length != COLOR_LENGTH_INCLUDING_ALPHA))
				return unknownTagColor;

			colorHex = colorHex.Replace("#", string.Empty);

			var alphaOffset = colorHex.Length == 8 ? 2 : 0;

			var a = (byte)255;
			var alphaValid = alphaOffset == 0 || byte.TryParse(colorHex.AsSpan(0, 2), NumberStyles.HexNumber, null, out a);

			if (alphaValid &&
				byte.TryParse(colorHex.AsSpan(alphaOffset, 2), NumberStyles.HexNumber, null, out byte r) &&
				byte.TryParse(colorHex.AsSpan(alphaOffset + 2, 2), NumberStyles.HexNumber, null, out byte g) &&
				byte.TryParse(colorHex.AsSpan(alphaOffset + 4, 2), NumberStyles.HexNumber, null, out byte b))
				return Color.FromArgb(a, r, g, b);

			return unknownTagColor;
		}

		/// <summary>
		/// Converts <see cref="uint"/> to <see cref="Color"/>.
		/// </summary>
		public static Color FromUint(this uint value)
		{
			return
				Color.FromArgb((byte)((value >> 24) & 0xFF),
					(byte)((value >> 16) & 0xFF),
					(byte)((value >> 8) & 0xFF),
					(byte)(value & 0xFF));
		}

		/// <summary>
		/// Converts <see cref="Color"/> to <see cref="uint"/>.
		/// </summary>
		public static uint ToUint(this Color c)
		{
			return (uint)(((c.A << 24) | (c.R << 16) | (c.G << 8) | c.B) & 0xffffffffL);
		}

		/// <summary>
		/// Converts <see cref="System.Drawing.Color"/> to hex <see cref="string"/>.
		/// </summary>
		private static string ToHex(this System.Drawing.Color color)
		{
			return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
		}

		/// <summary>
		/// Converts <see cref="System.Drawing.Color"/> to <see cref="Color"/>.
		/// </summary>
		public static Color ToWindowsColor(this System.Drawing.Color color)
		{
			string hex = color.ToHex();
			return FromHex(hex);
		}

		/// <summary>
		/// Converts <see cref="Color"/> to <see cref="System.Drawing.Color"/>.
		/// </summary>
		public static System.Drawing.Color FromWindowsColor(this Color color)
		{
			string hex = color.ToHex();

			return System.Drawing.Color.FromArgb(
				Convert.ToByte(hex.Substring(1, 2), 16),
				Convert.ToByte(hex.Substring(3, 2), 16),
				Convert.ToByte(hex.Substring(5, 2), 16),
				Convert.ToByte(hex.Substring(7, 2), 16));
		}

		/// <summary>
		/// Generates a random color and returns its hex <see cref="string"/>.
		/// </summary>
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
