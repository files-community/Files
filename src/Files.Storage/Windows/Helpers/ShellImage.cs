// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Extensions;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.Shell;

namespace Files.Storage;

public static class ShellImage
{
	private static readonly ConcurrentDictionary<(string File, int Index, int Size), IconFileInfo> _iconCache = new();
	private static readonly Lock _iconLock = new();
	private static readonly Lock _iconOverlayLock = new();
	private static int GetImageListForSize(int size) => size switch
	{
		<= 16 => 1,
		<= 32 => 0,
		<= 48 => 2,
		_ => 4,
	};

	/// <summary>Extracts an icon resource. The caller owns the returned handle.</summary>
	public static SafeHandle ExtractIconHandle(string file, int index, uint size, out HRESULT result)
	{
		result = PInvoke.SHDefExtractIcon(file, -index, 0, out var large, out var small, size);
		using var smallIcon = small;
		return large;
	}

	public static IList<IconFileInfo> ExtractSelectedIconsFromDLL(string file, IList<int> indexes, int iconSize = 48)
	{
		var iconsList = new List<IconFileInfo>();

		foreach (int index in indexes)
		{
			if (_iconCache.TryGetValue((file, index, iconSize), out var iconInfo))
			{
				iconsList.Add(iconInfo);
			}
			else
			{
				using var icon = ExtractIconHandle(file, index, Convert.ToUInt32(iconSize), out var result);
				if (result == HRESULT.S_OK)
				{
					using var image = GetBitmapFromHIcon(icon);
					if (image is null)
						continue;
					byte[] bitmapData = (byte[])(new ImageConverter().ConvertTo(image, typeof(byte[])) ?? Array.Empty<byte>());
					iconInfo = new IconFileInfo(bitmapData, index);
					_iconCache[(file, index, iconSize)] = iconInfo;
					iconsList.Add(iconInfo);
				}
			}
		}

		return iconsList;
	}

	/// <summary>Returns the icon overlay for a file or folder.</summary>
	public static byte[]? GetIconOverlay(string? path, int size, bool isDirectory)
	{
		if (string.IsNullOrEmpty(path))
			return null;

		SHFILEINFOW shFileInfo = default;
		const SHGFI_FLAGS flags = SHGFI_FLAGS.SHGFI_OVERLAYINDEX | SHGFI_FLAGS.SHGFI_ICON | SHGFI_FLAGS.SHGFI_SYSICONINDEX | SHGFI_FLAGS.SHGFI_ICONLOCATION;
		byte[]? overlayData = null;

		try
		{
			nuint result = PInvoke.SHGetFileInfo(path, isDirectory ? FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_DIRECTORY : 0, ref shFileInfo, flags);
			if (result is 0)
				return null;

			PInvoke.DestroyIcon(shFileInfo.hIcon);

			lock (_iconOverlayLock)
			{
				if (PInvoke.SHGetImageList<IImageList>(GetImageListForSize(size), out var imageList).Failed)
					return null;

				var overlayIdx = shFileInfo.iIcon >> 24;
				if (overlayIdx != 0)
				{
					imageList.GetOverlayImage(overlayIdx, out int overlayImage).ThrowOnFailure();
					imageList.GetIcon(overlayImage, 1, out var overlayHandle).ThrowOnFailure();
					using var hOverlay = overlayHandle;

					if (!hOverlay.IsClosed && !hOverlay.IsInvalid)
					{
						using var image = GetBitmapFromHIcon(hOverlay);
						if (image is null)
							return null;
						overlayData = (byte[]?)new ImageConverter().ConvertTo(image, typeof(byte[]));
					}
				}
			}
		}
		catch (Exception)
		{
			return null;
		}

		return overlayData;
	}

	/// <summary>Returns an icon, or a thumbnail when available and permitted by the options.</summary>
	public static unsafe byte[]? GetIcon(
		string? path,
		int size,
		bool isFolder,
		IconOptions iconOptions)
	{
		if (string.IsNullOrWhiteSpace(path))
			return null;

		byte[]? iconData = null;

		// Attempt to get file icon/thumbnail using IShellItemImageFactory GetImage
		using var shellItem = SafetyExtensions.IgnoreExceptions(()
			=> ShellPath.Open(path));

		if (shellItem is not null && shellItem.IShellItem is IShellItemImageFactory shellFactory)
		{
			var flags = SIIGBF.SIIGBF_BIGGERSIZEOK;

			if (iconOptions.HasFlag(IconOptions.ReturnIconOnly))
				flags |= SIIGBF.SIIGBF_ICONONLY;

			if (iconOptions.HasFlag(IconOptions.ReturnThumbnailOnly))
				flags |= SIIGBF.SIIGBF_THUMBNAILONLY;

			if (iconOptions.HasFlag(IconOptions.ReturnOnlyIfCached))
				flags |= SIIGBF.SIIGBF_INCACHEONLY;

			HBITMAP hbitmap = default;
			var hres = shellFactory.GetImage(new(size, size), flags, &hbitmap);
			try
			{
				if (hres == HRESULT.S_OK)
				{
					using var image = GetBitmapFromHBitmap(hbitmap);
					if (image is not null)
						iconData = (byte[]?)new ImageConverter().ConvertTo(image, typeof(byte[]));
				}
			}
			finally
			{
				if (!hbitmap.IsNull)
					PInvoke.DeleteObject(hbitmap);
			}
		}

		if (iconData is not null || iconOptions.HasFlag(IconOptions.ReturnThumbnailOnly))
			return iconData;
		else
		{
			SHFILEINFOW shfi = default;
			const SHGFI_FLAGS flags = SHGFI_FLAGS.SHGFI_OVERLAYINDEX | SHGFI_FLAGS.SHGFI_ICON | SHGFI_FLAGS.SHGFI_SYSICONINDEX | SHGFI_FLAGS.SHGFI_ICONLOCATION | SHGFI_FLAGS.SHGFI_USEFILEATTRIBUTES;

			// Cannot access file, use file attributes
			var ret = PInvoke.SHGetFileInfo(path, isFolder ? FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_DIRECTORY : 0, ref shfi, flags);
			if (ret is 0)
				return iconData;

			PInvoke.DestroyIcon(shfi.hIcon);

			var imageListSize = GetImageListForSize(size);

			lock (_iconLock)
			{
				if (PInvoke.SHGetImageList<IImageList>(imageListSize, out var imageList).Failed)
					return iconData;

				if (iconData is null)
				{
					var iconIdx = shfi.iIcon & 0xFFFFFF;
					if (iconIdx != 0)
					{
						// Could not fetch thumbnail, load simple icon
						imageList.GetIcon(iconIdx, 1, out var iconHandle).ThrowOnFailure();
						using var hIcon = iconHandle;
						if (!hIcon.IsClosed && !hIcon.IsInvalid)
						{
							using (var image = GetBitmapFromHIcon(hIcon))
							{
								if (image is not null)
									iconData = (byte[]?)new ImageConverter().ConvertTo(image, typeof(byte[]));
							}
						}
					}
					else if (isFolder)
					{
						// Could not icon, load generic icon
						iconData = ExtractSelectedIconsFromDLL(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "imageres.dll"), [2], size).FirstOrDefault()?.IconData;
					}
					else
					{
						// Could not icon, load generic icon
						iconData = ExtractSelectedIconsFromDLL(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll"), [1], size).FirstOrDefault()?.IconData;
					}
				}
			}

			return iconData;
		}
	}

	public static Bitmap? GetBitmapFromHBitmap(HBITMAP hBitmap)
	{
		try
		{
			Bitmap bmp = Image.FromHbitmap((IntPtr)hBitmap);
			if (Image.GetPixelFormatSize(bmp.PixelFormat) < 32)
				return bmp;

			Rectangle bmBounds = new Rectangle(0, 0, bmp.Width, bmp.Height);
			var bmpData = bmp.LockBits(bmBounds, ImageLockMode.ReadOnly, bmp.PixelFormat);

			if (IsAlphaBitmap(bmpData))
			{
				var alpha = GetAlphaBitmapFromBitmapData(bmpData);

				bmp.UnlockBits(bmpData);
				bmp.Dispose();

				return alpha;
			}

			bmp.UnlockBits(bmpData);

			return bmp;
		}
		catch
		{
			return null;
		}
	}

	public static Bitmap? GetBitmapFromHIcon(SafeHandle iconHandle)
	{
		if (iconHandle.IsInvalid || iconHandle.IsClosed)
			return null;

		try
		{
			using var icon = (Icon)Icon.FromHandle(iconHandle.DangerousGetHandle()).Clone();
			return icon.ToBitmap();
		}
		catch
		{
			return null;
		}
	}

	private static Bitmap GetAlphaBitmapFromBitmapData(BitmapData bmpData)
	{
		using var tmp = new Bitmap(bmpData.Width, bmpData.Height, bmpData.Stride, PixelFormat.Format32bppArgb, bmpData.Scan0);
		Bitmap clone = new Bitmap(tmp.Width, tmp.Height, tmp.PixelFormat);

		using (Graphics gr = Graphics.FromImage(clone))
		{
			gr.DrawImage(tmp, new Rectangle(0, 0, clone.Width, clone.Height));
		}

		return clone;
	}

	private static bool IsAlphaBitmap(BitmapData bmpData)
	{
		for (int y = 0; y <= bmpData.Height - 1; y++)
		{
			for (int x = 0; x <= bmpData.Width - 1; x++)
			{
				Color pixelColor = Color.FromArgb(
					Marshal.ReadInt32(bmpData.Scan0, (bmpData.Stride * y) + (4 * x)));

				if (pixelColor.A < 255)
					return true;
			}
		}

		return false;
	}
}
