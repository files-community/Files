// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Text;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Files.App.Utils.Storage
{
	public static class FontFileHelper
	{
		private const float FontSizeRatio = 0.35f;
		private const string PreviewText = "Abg";

		public static async Task<byte[]?> GetWinRTThumbnailAsync(string fontPath, uint size)
		{
			StorageFile? file = null;
			StorageItemThumbnail? thumbnail = null;
			try
			{
				file = await StorageFile.GetFileFromPathAsync(fontPath);
				thumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem, size);

				if (thumbnail is null || thumbnail.Size == 0)
				{
					return null;
				}

				using (var stream = thumbnail.AsStream())
				{
					using var memoryStream = new MemoryStream((int)thumbnail.Size);
					await stream.CopyToAsync(memoryStream);

					return memoryStream.ToArray();
				}
			}
			catch (Exception ex) 
			{			
				App.Logger.LogError(ex, $"Exception while getting WinRT thumbnail for {fontPath}.");
				return null;
			}
			finally
			{
				thumbnail?.Dispose();
			}
		}

		public static byte[]? GenerateFontThumbnail(string fontPath, int size)
		{
			try
			{
				if (!File.Exists(fontPath))
					return null;

				using var fontCollection = new PrivateFontCollection();
				fontCollection.AddFontFile(fontPath);

				if (fontCollection.Families.Length == 0)
					return null;

				var fontFamily = fontCollection.Families[0];
				var style = GetAvailableFontStyle(fontFamily);

				using var bitmap = new Bitmap(size, size);
				using var graphics = Graphics.FromImage(bitmap);

				graphics.Clear(Color.White);
				graphics.SmoothingMode = SmoothingMode.AntiAlias;
				graphics.TextRenderingHint = TextRenderingHint.AntiAlias;

				var fontSize = size * FontSizeRatio;
				using var font = new Font(fontFamily, fontSize, style, GraphicsUnit.Pixel);

				var textSize = graphics.MeasureString(PreviewText, font);

				var x = (size - textSize.Width) / 2;
				var y = (size - textSize.Height) / 2;

				using var brush = new SolidBrush(Color.Black);
				graphics.DrawString(PreviewText, font, brush, x, y);

				using var ms = new MemoryStream();
				bitmap.Save(ms, ImageFormat.Png);
				return ms.ToArray();
			}
			catch (Exception ex)
			{
				App.Logger.LogError(ex, $"Exception while generating font thumbnail for {fontPath}.");
				return null;
			}
		}

		public static string? GetFontName(string fontPath)
		{
			try
			{
				if (!File.Exists(fontPath))
					return null;

				var fullName = ExtractFontNameFromTable(fontPath);
				if (!string.IsNullOrEmpty(fullName))
					return fullName;

				using var fontCollection = new PrivateFontCollection();
				fontCollection.AddFontFile(fontPath);

				if (fontCollection.Families.Length == 0)
					return null;

				return fontCollection.Families[0].Name;
			}
			catch
			{
				App.Logger.LogError($"Failed to get font name for file: {fontPath}");
				return null;
			}
		}

		private static string? ExtractFontNameFromTable(string fontPath)
		{
			try
			{
				using var fileStream = File.OpenRead(fontPath);
				using var reader = new BinaryReader(fileStream);

				// Read TTF header to find table directory
				var sfntVersion = ReadUInt32BigEndian(reader);

				// Check if it's a TrueType Collection (.ttc)
				if (sfntVersion == 0x74746366) // 'ttcf'
				{
					// For TTC files, read the first font in the collection
					reader.ReadUInt32(); // version
					var numFonts = ReadUInt32BigEndian(reader);
					if (numFonts == 0)
						return null;

					// Read offset to first font
					var firstFontOffset = ReadUInt32BigEndian(reader);
					fileStream.Seek(firstFontOffset, SeekOrigin.Begin);
					reader.ReadUInt32(); // Skip sfntVersion of inner font
				}
				else if (sfntVersion != 0x00010000 && sfntVersion != 0x4F54544F) // Not TTF or OTF
				{
					return null;
				}

				var numTables = ReadUInt16BigEndian(reader);
				reader.ReadUInt16(); // searchRange
				reader.ReadUInt16(); // entrySelector
				reader.ReadUInt16(); // rangeShift

				// Find the 'name' table
				uint nameTableOffset = 0;
				uint nameTableLength = 0;

				for (int i = 0; i < numTables; i++)
				{
					var tag = Encoding.ASCII.GetString(reader.ReadBytes(4));
					reader.ReadUInt32(); // checksum
					var offset = ReadUInt32BigEndian(reader);
					var length = ReadUInt32BigEndian(reader);

					if (tag == "name")
					{
						nameTableOffset = offset;
						nameTableLength = length;
						break;
					}
				}

				if (nameTableOffset == 0)
					return null;

				fileStream.Seek(nameTableOffset, SeekOrigin.Begin);

				var version = ReadUInt16BigEndian(reader);
				var count = ReadUInt16BigEndian(reader);
				var storageOffset = ReadUInt16BigEndian(reader);

				string? familyName = null;
				string? subfamilyName = null;
				string? fullName = null;

				for (int i = 0; i < count; i++)
				{
					var platformID = ReadUInt16BigEndian(reader);
					var encodingID = ReadUInt16BigEndian(reader);
					var languageID = ReadUInt16BigEndian(reader);
					var nameID = ReadUInt16BigEndian(reader);
					var length = ReadUInt16BigEndian(reader);
					var stringOffset = ReadUInt16BigEndian(reader);

					var isWindows = platformID == 3 && languageID == 0x0409;
					var isUnicode = platformID == 0 && (languageID == 0 || languageID == 0x0409);

					if (!isWindows && !isUnicode)
						continue;

					var currentPos = fileStream.Position;

					fileStream.Seek(nameTableOffset + storageOffset + stringOffset, SeekOrigin.Begin);
					var stringBytes = reader.ReadBytes(length);

					string stringValue;
					if (platformID == 3 || platformID == 0) // Windows or Unicode - UTF-16BE
					{
						stringValue = Encoding.BigEndianUnicode.GetString(stringBytes);
					}
					else // Macintosh - ASCII/Latin1
					{
						stringValue = Encoding.ASCII.GetString(stringBytes);
					}

					if (nameID == 4)
						fullName = stringValue;
					else if (nameID == 1)
						familyName = stringValue;
					else if (nameID == 2)
						subfamilyName = stringValue;

					fileStream.Seek(currentPos, SeekOrigin.Begin);
				}

				if (!string.IsNullOrEmpty(fullName))
					return fullName;

				if (!string.IsNullOrEmpty(familyName) && !string.IsNullOrEmpty(subfamilyName))
				{
					if (subfamilyName.Equals("Regular", StringComparison.OrdinalIgnoreCase))
						return familyName;

					return $"{familyName} {subfamilyName}";
				}

				return familyName;
			}
			catch
			{
				return null;
			}
		}

		private static uint ReadUInt32BigEndian(BinaryReader reader)
		{
			var bytes = reader.ReadBytes(4);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			return BitConverter.ToUInt32(bytes, 0);
		}

		private static ushort ReadUInt16BigEndian(BinaryReader reader)
		{
			var bytes = reader.ReadBytes(2);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			return BitConverter.ToUInt16(bytes, 0);
		}

		private static FontStyle GetAvailableFontStyle(FontFamily fontFamily)
		{
			if (fontFamily.IsStyleAvailable(FontStyle.Regular))
				return FontStyle.Regular;
			if (fontFamily.IsStyleAvailable(FontStyle.Bold))
				return FontStyle.Bold;
			if (fontFamily.IsStyleAvailable(FontStyle.Italic))
				return FontStyle.Italic;
			if (fontFamily.IsStyleAvailable(FontStyle.Bold | FontStyle.Italic))
				return FontStyle.Bold | FontStyle.Italic;

			return FontStyle.Regular;
		}
	}
}
