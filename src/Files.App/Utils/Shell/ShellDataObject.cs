// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.System.Com;
using Windows.Win32.System.Memory;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;

namespace Files.App.Utils.Shell
{
	/// <summary>
	/// Creates and reads Shell data objects used by clipboard and drag-and-drop operations.
	/// </summary>
	public static unsafe class ShellDataObject
	{
		private const string ShellIdListArrayFormat = "Shell IDList Array";
		private const string FileGroupDescriptorFormat = "FileGroupDescriptorW";
		private const string FileContentsFormat = "FileContents";

		public readonly record struct FileDescriptor(string Name, FILE_FLAGS_AND_ATTRIBUTES Attributes);

		/// <summary>Creates a Shell data object for the supplied items.</summary>
		public static IDataObject Create(IReadOnlyList<ShellItem> items)
		{
			ArgumentOutOfRangeException.ThrowIfZero(items.Count);
			PInvoke.SHGetDesktopFolder(out IShellFolder? desktop).ThrowOnFailure();
			PInvoke.SHGetIDListFromObject(desktop!, out ITEMIDLIST* desktopPidl).ThrowOnFailure();

			ITEMIDLIST** itemPidls = (ITEMIDLIST**)NativeMemory.AllocZeroed((nuint)items.Count, (nuint)sizeof(ITEMIDLIST*));
			try
			{
				for (int index = 0; index < items.Count; index++)
					PInvoke.SHGetIDListFromObject(items[index].IShellItem, out itemPidls[index]).ThrowOnFailure();

				Guid interfaceId = typeof(IDataObject).GUID;
				PInvoke.SHCreateDataObject(desktopPidl, (uint)items.Count, itemPidls, null!, &interfaceId, out object dataObject).ThrowOnFailure();
				return (IDataObject)dataObject;
			}
			finally
			{
				for (int index = 0; index < items.Count; index++)
					PInvoke.CoTaskMemFree(itemPidls[index]);
				NativeMemory.Free(itemPidls);
				PInvoke.CoTaskMemFree(desktopPidl);
			}
		}

		/// <summary>Reads the Shell IDList Array clipboard payload from a data object.</summary>
		public static byte[]? GetShellIdListArray(IDataObject dataObject)
		{
			if (!TryGetData(dataObject, ShellIdListArrayFormat, -1, TYMED.TYMED_HGLOBAL, out STGMEDIUM medium))
				return null;

			void* data = null;
			try
			{
				data = PInvoke.GlobalLock(medium.u.hGlobal);
				nuint size = PInvoke.GlobalSize(medium.u.hGlobal);
				if (data is null || size is 0 || size > int.MaxValue)
					return null;

				byte[] result = new byte[(int)size];
				Marshal.Copy((nint)data, result, 0, result.Length);
				return result;
			}
			finally
			{
				if (data is not null)
					PInvoke.GlobalUnlock(medium.u.hGlobal);
				PInvoke.ReleaseStgMedium(ref medium);
			}
		}

		/// <summary>Gets the current OLE clipboard data object.</summary>
		public static IDataObject? GetClipboard()
			=> PInvoke.OleGetClipboard(out IDataObject dataObject).Succeeded ? dataObject : null;

		/// <summary>Reads virtual-file descriptors from a clipboard data object.</summary>
		public static IReadOnlyList<FileDescriptor> GetFileDescriptors(IDataObject dataObject)
		{
			if (!TryGetData(dataObject, FileGroupDescriptorFormat, -1, TYMED.TYMED_HGLOBAL, out STGMEDIUM medium))
				return [];

			void* data = null;
			try
			{
				data = PInvoke.GlobalLock(medium.u.hGlobal);
				if (data is null)
					return [];

				FILEGROUPDESCRIPTORW* group = (FILEGROUPDESCRIPTORW*)data;
				nuint dataSize = PInvoke.GlobalSize(medium.u.hGlobal);
				if (group->cItems > int.MaxValue || (nuint)FILEGROUPDESCRIPTORW.SizeOf((int)group->cItems) > dataSize)
					return [];

				var descriptors = new List<FileDescriptor>((int)group->cItems);
				for (int index = 0; index < group->cItems; index++)
				{
					ref FILEDESCRIPTORW descriptor = ref group->fgd[index];
					descriptors.Add(new(descriptor.cFileName.ToString(), (FILE_FLAGS_AND_ATTRIBUTES)descriptor.dwFileAttributes));
				}
				return descriptors;
			}
			finally
			{
				if (data is not null)
					PInvoke.GlobalUnlock(medium.u.hGlobal);
				PInvoke.ReleaseStgMedium(ref medium);
			}
		}

		/// <summary>Gets a virtual file-content stream and transfers ownership of its storage medium to the caller.</summary>
		public static bool TryGetFileContents(IDataObject dataObject, int index, out IStream? stream, out STGMEDIUM medium)
		{
			stream = null;
			if (!TryGetData(dataObject, FileContentsFormat, index, TYMED.TYMED_ISTREAM, out medium) || medium.u.pstm is null)
				return false;

			try
			{
				stream = ComInterfaceMarshaller<IStream>.ConvertToManaged(medium.u.pstm);
			}
			catch
			{
				PInvoke.ReleaseStgMedium(ref medium);
				medium = default;
				throw;
			}
			if (stream is not null)
				return true;

			PInvoke.ReleaseStgMedium(ref medium);
			medium = default;
			return false;
		}

		private static bool TryGetData(IDataObject dataObject, string formatName, int index, TYMED mediumType, out STGMEDIUM medium)
		{
			uint format = PInvoke.RegisterClipboardFormat(formatName);
			FORMATETC formatEtc = new()
			{
				cfFormat = checked((ushort)format),
				dwAspect = (uint)DVASPECT.DVASPECT_CONTENT,
				lindex = index,
				tymed = (uint)mediumType,
			};
			if (dataObject.GetData(formatEtc, out medium).Failed)
				return false;

			if (medium.tymed == mediumType)
				return true;

			PInvoke.ReleaseStgMedium(ref medium);
			medium = default;
			return false;
		}
	}
}
