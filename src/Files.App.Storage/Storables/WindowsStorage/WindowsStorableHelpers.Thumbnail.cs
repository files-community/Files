// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.Graphics.GdiPlus;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace Files.App.Storage.Storables
{
	public static partial class WindowsStorableHelpers
	{
		private static (Guid Format, Guid Encorder)[]? GdiEncoders;

		/// <inheritdoc cref="GetThumbnail"/>
		public static async Task<byte[]> GetThumbnailAsync(this IWindowsStorable storable, int size, SIIGBF options)
		{
			return await STATask.Run(() => storable.GetThumbnail(size, options));
		}

		/// <summary>
		/// Retrieves a thumbnail image data for the specified <paramref name="storable"/> using <see cref="IShellItemImageFactory"/>.
		/// </summary>
		/// <param name="storable">An object that implements <see cref="IWindowsStorable"/> and represents a shell item on Windows.</param>
		/// <param name="size">The desired size (in pixels) of the thumbnail (width and height are equal).</param>
		/// <param name="options">A combination of <see cref="SIIGBF"/> flags that specify how the thumbnail should be retrieved.</param>
		/// <returns>A byte array containing the thumbnail image in its native format (e.g., PNG, JPEG).</returns>
		/// <remarks>If the thumbnail is JPEG, this tries to decoded as a PNG instead because JPEG loses data.</remarks>
		public unsafe static byte[] GetThumbnail(this IWindowsStorable storable, int size, SIIGBF options)
		{
			using ComPtr<IShellItemImageFactory> pShellItemImageFactory = storable.ThisPtr.As<IShellItemImageFactory>();
			if (pShellItemImageFactory.IsNull)
				return [];

			// Get HBITMAP
			HBITMAP hBitmap = default;
			HRESULT hr = pShellItemImageFactory.Get()->GetImage(new(size, size), options, &hBitmap);
			if (hr.ThrowIfFailedOnDebug().Failed)
			{
				if (!hBitmap.IsNull) PInvoke.DeleteObject(hBitmap);
				return [];
			}

			// Convert to GpBitmap of GDI+
			GpBitmap* gpBitmap = default;
			PInvoke.GdipCreateBitmapFromHBITMAP(hBitmap, HPALETTE.Null, &gpBitmap);

			// Get an encoder for PNG
			Guid format = Guid.Empty;
			if (PInvoke.GdipGetImageRawFormat((GpImage*)gpBitmap, &format) is not Status.Ok)
			{
				if (gpBitmap is not null) PInvoke.GdipDisposeImage((GpImage*)gpBitmap);
				if (!hBitmap.IsNull) PInvoke.DeleteObject(hBitmap);
				return [];
			}

			Guid encoder = GetEncoderClsid(format);
			if (format == PInvoke.ImageFormatJPEG || encoder == Guid.Empty)
			{
				format = PInvoke.ImageFormatPNG;
				encoder = GetEncoderClsid(format);
			}

			using ComPtr<IStream> pStream = default;
			hr = PInvoke.CreateStreamOnHGlobal(HGLOBAL.Null, true, pStream.GetAddressOf());
			if (hr.ThrowIfFailedOnDebug().Failed)
			{
				if (gpBitmap is not null) PInvoke.GdipDisposeImage((GpImage*)gpBitmap);
				if (!hBitmap.IsNull) PInvoke.DeleteObject(hBitmap);
				return [];
			}

			if (PInvoke.GdipSaveImageToStream((GpImage*)gpBitmap, pStream.Get(), &encoder, (EncoderParameters*)null) is not Status.Ok)
			{
				if (gpBitmap is not null) PInvoke.GdipDisposeImage((GpImage*)gpBitmap);
				if (!hBitmap.IsNull) PInvoke.DeleteObject(hBitmap);
				return [];
			}

			STATSTG stat = default;
			hr = pStream.Get()->Stat(&stat, (uint)STATFLAG.STATFLAG_NONAME);
			if (hr.ThrowIfFailedOnDebug().Failed)
			{
				if (gpBitmap is not null) PInvoke.GdipDisposeImage((GpImage*)gpBitmap);
				if (!hBitmap.IsNull) PInvoke.DeleteObject(hBitmap);
				return [];
			}

			ulong statSize = stat.cbSize & 0xFFFFFFFF;
			byte* RawThumbnailData = (byte*)NativeMemory.Alloc((nuint)statSize);

			pStream.Get()->Seek(0L, (SystemIO.SeekOrigin)STREAM_SEEK.STREAM_SEEK_SET, null);
			hr = pStream.Get()->Read(RawThumbnailData, (uint)statSize);
			if (hr.ThrowIfFailedOnDebug().Failed)
			{
				if (gpBitmap is not null) PInvoke.GdipDisposeImage((GpImage*)gpBitmap);
				if (!hBitmap.IsNull) PInvoke.DeleteObject(hBitmap);
				if (RawThumbnailData is not null) NativeMemory.Free(RawThumbnailData);
				return [];
			}

			byte[] thumbnailData = new ReadOnlySpan<byte>(RawThumbnailData, (int)statSize / sizeof(byte)).ToArray();
			NativeMemory.Free(RawThumbnailData);

			return thumbnailData;

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
	}
}
