// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.Helpers;
using System.Globalization;
using Windows.UI;
using SystemDrawing = System.Drawing;

namespace Files.App.Helpers
{
	internal static class ColorHelpers
	{
		private const int COLOR_LENGTH = 0x7;

		private const int COLOR_LENGTH_INCLUDING_ALPHA = 0x9;

		private static readonly Color unknownTagColor = Color.FromArgb(0xFF, 0x9E, 0xA3, 0xA1);

		/// <summary>
		/// Converts hexadecimal <see cref="string"/> to <see cref="Color"/>.
		/// </summary>
		public static Color FromHex(string? colorHex)
		{
			// If Hex string is invalid, return Unknown Tag's color
			if (
				string.IsNullOrWhiteSpace(colorHex) ||
				(colorHex.Length != COLOR_LENGTH && colorHex.Length != COLOR_LENGTH_INCLUDING_ALPHA)
				)
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
				Color.FromArgb((byte)(
					(value >> 24) & 0xFF),
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
		/// Generates a random <see cref="Color"/> and returns its hexadecimal <see cref="string"/>.
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

		private static string ToHex(this SystemDrawing.Color color)
		{
			return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
		}

		public static Color ToWindowsColor(this SystemDrawing.Color color)
		{
			string hex = color.ToHex();
			return FromHex(hex);
		}

		public static SystemDrawing.Color FromWindowsColor(this Color color)
		{
			string hex = color.ToHex();

			return SystemDrawing.Color.FromArgb(
				Convert.ToByte(hex.Substring(1, 2), 16),
				Convert.ToByte(hex.Substring(3, 2), 16),
				Convert.ToByte(hex.Substring(5, 2), 16),
				Convert.ToByte(hex.Substring(7, 2), 16));
		}
	}
}
