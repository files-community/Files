// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.Graphics.GdiPlus;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Storage
{
	public static partial class WindowsStorableHelpers
	{
		// Fields

		private static (Guid Format, Guid Encorder)[]? GdiEncoders;
		private static ConcurrentDictionary<(string, int, int), byte[]>? DllIconCache;

		// Methods

		/// <inheritdoc cref="TryGetThumbnail"/>
		public static async Task<byte[]?> GetThumbnailAsync(this IWindowsStorable storable, int size, SIIGBF options)
		{
			return await STATask.Run(() =>
			{
				HRESULT hr = storable.TryGetThumbnail(size, options, out var thumbnailData).ThrowIfFailedOnDebug();
				return thumbnailData;
			});
		}

		/// <summary>
		/// Retrieves a thumbnail image data for the specified <paramref name="storable"/> using <see cref="IShellItemImageFactory"/>.
		/// </summary>
		/// <param name="storable">An object that implements <see cref="IWindowsStorable"/> and represents a shell item on Windows.</param>
		/// <param name="size">The desired size (in pixels) of the thumbnail (width and height are equal).</param>
		/// <param name="options">A combination of <see cref="SIIGBF"/> flags that specify how the thumbnail should be retrieved.</param>
		/// <returns>A byte array containing the thumbnail image in its native format (e.g., PNG, JPEG).</returns>
		/// <remarks>If the thumbnail is JPEG, this tries to decoded as a PNG instead because JPEG loses data.</remarks>
		public unsafe static HRESULT TryGetThumbnail(this IWindowsStorable storable, int size, SIIGBF options, out byte[]? thumbnailData)
		{
			thumbnailData = null;

			using ComPtr<IShellItemImageFactory> pShellItemImageFactory = storable.ThisPtr.As<IShellItemImageFactory>();
			if (pShellItemImageFactory.IsNull)
				return HRESULT.E_NOINTERFACE;

			// Get HBITMAP
			HBITMAP hBitmap = default;
			HRESULT hr = pShellItemImageFactory.Get()->GetImage(new(size, size), options, &hBitmap);
			if (hr.ThrowIfFailedOnDebug().Failed)
			{
				if (!hBitmap.IsNull) PInvoke.DeleteObject(hBitmap);
				return hr;
			}

			// Convert to GpBitmap of GDI+
			GpBitmap* gpBitmap = default;
			if (PInvoke.GdipCreateBitmapFromHBITMAP(hBitmap, HPALETTE.Null, &gpBitmap) is not Status.Ok)
			{
				if (gpBitmap is not null) PInvoke.GdipDisposeImage((GpImage*)gpBitmap);
				if (!hBitmap.IsNull) PInvoke.DeleteObject(hBitmap);
				return HRESULT.E_FAIL;
			}

			if (TryConvertGpBitmapToByteArray(gpBitmap, out thumbnailData))
			{
				if (!hBitmap.IsNull) PInvoke.DeleteObject(hBitmap);
				return HRESULT.E_FAIL;
			}

			return HRESULT.S_OK;
		}

		public unsafe static HRESULT TryExtractImageFromDll(this IWindowsStorable storable, int size, int index, out byte[]? imageData)
		{
			DllIconCache ??= [];
			imageData = null;

			if (storable.ToString() is not { } path)
				return HRESULT.E_INVALIDARG;

			if (DllIconCache.TryGetValue((path, index, size), out var cachedImageData))
			{
				imageData = cachedImageData;
				return HRESULT.S_OK;
			}
			else
			{
				HICON hIcon = default;
				HRESULT hr = default;

				fixed (char* pszPath = path)
					hr = PInvoke.SHDefExtractIcon(pszPath, -1 * index, 0, &hIcon, null, (uint)size);

				if (hr.ThrowIfFailedOnDebug().Failed)
				{
					if (!hIcon.IsNull) PInvoke.DestroyIcon(hIcon);
					return hr;
				}

				// Convert to GpBitmap of GDI+
				GpBitmap* gpBitmap = default;
				if (PInvoke.GdipCreateBitmapFromHICON(hIcon, &gpBitmap) is not Status.Ok)
				{
					if (!hIcon.IsNull) PInvoke.DestroyIcon(hIcon);
					return HRESULT.E_FAIL;
				}

				if (!TryConvertGpBitmapToByteArray(gpBitmap, out imageData))
				{
					if (!hIcon.IsNull) PInvoke.DestroyIcon(hIcon);
					return HRESULT.E_FAIL;
				}

				DllIconCache[(path, index, size)] = imageData;
				PInvoke.DestroyIcon(hIcon);

				return HRESULT.S_OK;
			}
		}

		public unsafe static bool TryConvertGpBitmapToByteArray(GpBitmap* gpBitmap, out byte[]? imageData)
		{
			imageData = null;

			// Get an encoder for PNG
			Guid format = Guid.Empty;
			if (PInvoke.GdipGetImageRawFormat((GpImage*)gpBitmap, &format) is not Status.Ok)
			{
				if (gpBitmap is not null) PInvoke.GdipDisposeImage((GpImage*)gpBitmap);
				return false;
			}

			Guid encoder = GetEncoderClsid(format);
			if (format == PInvoke.ImageFormatJPEG || encoder == Guid.Empty)
			{
				format = PInvoke.ImageFormatPNG;
				encoder = GetEncoderClsid(format);
			}

			using ComPtr<IStream> pStream = default;
			HRESULT hr = PInvoke.CreateStreamOnHGlobal(HGLOBAL.Null, true, pStream.GetAddressOf());
			if (hr.ThrowIfFailedOnDebug().Failed)
			{
				if (gpBitmap is not null) PInvoke.GdipDisposeImage((GpImage*)gpBitmap);
				return false;
			}

			if (PInvoke.GdipSaveImageToStream((GpImage*)gpBitmap, pStream.Get(), &encoder, (EncoderParameters*)null) is not Status.Ok)
			{
				if (gpBitmap is not null) PInvoke.GdipDisposeImage((GpImage*)gpBitmap);
				return false;
			}

			STATSTG stat = default;
			hr = pStream.Get()->Stat(&stat, (uint)STATFLAG.STATFLAG_NONAME);
			if (hr.ThrowIfFailedOnDebug().Failed)
			{
				if (gpBitmap is not null) PInvoke.GdipDisposeImage((GpImage*)gpBitmap);
				return false;
			}

			ulong statSize = stat.cbSize & 0xFFFFFFFF;
			byte* RawThumbnailData = (byte*)NativeMemory.Alloc((nuint)statSize);

			pStream.Get()->Seek(0L, (SystemIO.SeekOrigin)STREAM_SEEK.STREAM_SEEK_SET, null);
			hr = pStream.Get()->Read(RawThumbnailData, (uint)statSize);
			if (hr.ThrowIfFailedOnDebug().Failed)
			{
				if (gpBitmap is not null) PInvoke.GdipDisposeImage((GpImage*)gpBitmap);
				if (RawThumbnailData is not null) NativeMemory.Free(RawThumbnailData);
				return false;
			}

			imageData = new ReadOnlySpan<byte>(RawThumbnailData, (int)statSize / sizeof(byte)).ToArray();
			NativeMemory.Free(RawThumbnailData);

			return true;

			Guid GetEncoderClsid(Guid format)
			{
				foreach ((Guid Format, Guid Encoder) in GetGdiEncoders())
					if (Format == format)
						return Encoder;

				return Guid.Empty;
			}

			(Guid Format, Guid Encorder)[] GetGdiEncoders()
			{
				if (GdiEncoders is not null)
					return GdiEncoders;

				if (PInvoke.GdipGetImageEncodersSize(out var numEncoders, out var size) is not Status.Ok)
					return [];

				ImageCodecInfo* pImageCodecInfo = (ImageCodecInfo*)NativeMemory.Alloc(size);

				if (PInvoke.GdipGetImageEncoders(numEncoders, size, pImageCodecInfo) is not Status.Ok)
					return [];

				ReadOnlySpan<ImageCodecInfo> codecs = new(pImageCodecInfo, (int)numEncoders);
				GdiEncoders = new (Guid Format, Guid Encoder)[codecs.Length];
				for (int index = 0; index < codecs.Length; index++)
					GdiEncoders[index] = (codecs[index].FormatID, codecs[index].Clsid);

				return GdiEncoders;
			}
		}

		public unsafe static HRESULT TrySetFolderIcon(this IWindowsStorable storable, IWindowsStorable iconFile, int index)
		{
			if (storable.GetDisplayName() is not { } folderPath ||
				iconFile.GetDisplayName() is not { } filePath)
				return HRESULT.E_INVALIDARG;

			fixed (char* pszFolderPath = folderPath, pszIconFile = filePath)
			{
				SHFOLDERCUSTOMSETTINGS settings = default;
				settings.dwSize = (uint)sizeof(SHFOLDERCUSTOMSETTINGS);
				settings.dwMask = PInvoke.FCSM_ICONFILE;
				settings.pszIconFile = pszIconFile;
				settings.cchIconFile = 0;
				settings.iIconIndex = index;

				HRESULT hr = PInvoke.SHGetSetFolderCustomSettings(&settings, pszFolderPath, PInvoke.FCS_FORCEWRITE);
				if (hr.ThrowIfFailedOnDebug().Failed)
					return hr;
			}

			return HRESULT.S_OK;
		}

		public unsafe static HRESULT TrySetShortcutIcon(this IWindowsStorable storable, IWindowsStorable iconFile, int index)
		{
			if (iconFile.ToString() is not { } iconFilePath)
				return HRESULT.E_INVALIDARG;

			using ComPtr<IShellLinkW> pShellLink = default;
			Guid IID_IShellLink = IShellLinkW.IID_Guid;
			Guid BHID_SFUIObject = PInvoke.BHID_SFUIObject;

			HRESULT hr = storable.ThisPtr.Get()->BindToHandler(null, &BHID_SFUIObject, &IID_IShellLink, (void**)pShellLink.GetAddressOf());
			if (hr.ThrowIfFailedOnDebug().Failed)
				return hr;

			fixed (char* pszIconFilePath = iconFilePath)
				hr = pShellLink.Get()->SetIconLocation(iconFilePath, index);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return hr;

			return HRESULT.S_OK;
		}
	}
}
