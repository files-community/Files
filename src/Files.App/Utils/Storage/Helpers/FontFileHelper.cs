// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
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
