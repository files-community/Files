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
		private const ushort CfHDrop = 15;
		private const int ClipboardRetryCount = 10;
		private const int ClipboardRetryDelay = 100;
		internal const string ShellIdListArrayFormat = "Shell IDList Array";
		private const string PreferredDropEffectFormat = "Preferred DropEffect";
		private const string FileGroupDescriptorFormat = "FileGroupDescriptorW";
		private const string FileContentsFormat = "FileContents";

		public readonly record struct FileDescriptor(string Name, FILE_FLAGS_AND_ATTRIBUTES Attributes);

		/// <summary>Creates a Shell data object for the supplied items.</summary>
		public static IDataObject Create(IReadOnlyList<ShellItem> items)
		{
			ArgumentOutOfRangeException.ThrowIfZero(items.Count);
			using ShellItem? commonParent = GetCommonParent(items);
			ITEMIDLIST* parentPidl;
			if (commonParent is not null)
			{
				PInvoke.SHGetIDListFromObject(commonParent.IShellItem, out parentPidl).ThrowOnFailure();
			}
			else
			{
				PInvoke.SHGetDesktopFolder(out IShellFolder? desktop).ThrowOnFailure();
				PInvoke.SHGetIDListFromObject(desktop!, out parentPidl).ThrowOnFailure();
			}

			ITEMIDLIST** itemPidls = null;
			ITEMIDLIST** dataObjectPidls = null;
			try
			{
				itemPidls = (ITEMIDLIST**)NativeMemory.AllocZeroed((nuint)items.Count, (nuint)sizeof(ITEMIDLIST*));
				dataObjectPidls = (ITEMIDLIST**)NativeMemory.AllocZeroed((nuint)items.Count, (nuint)sizeof(ITEMIDLIST*));
				for (int index = 0; index < items.Count; index++)
				{
					PInvoke.SHGetIDListFromObject(items[index].IShellItem, out itemPidls[index]).ThrowOnFailure();
					// SHCreateDataObject expects single child PIDLs when the items share a parent.
					dataObjectPidls[index] = commonParent is null ? itemPidls[index] : PInvoke.ILFindLastID(itemPidls[index]);
				}

				Guid interfaceId = typeof(IDataObject).GUID;
				PInvoke.SHCreateDataObject(parentPidl, (uint)items.Count, dataObjectPidls, null!, &interfaceId, out object dataObject).ThrowOnFailure();
				return (IDataObject)dataObject;
			}
			finally
			{
				if (itemPidls is not null)
				{
					for (int index = 0; index < items.Count; index++)
						PInvoke.CoTaskMemFree(itemPidls[index]);
				}
				NativeMemory.Free(dataObjectPidls);
				NativeMemory.Free(itemPidls);
				PInvoke.CoTaskMemFree(parentPidl);
			}
		}

		/// <summary>Places file system paths on the clipboard with the specified preferred drop effect.</summary>
		public static void SetClipboard(IReadOnlyList<string> paths, uint preferredDropEffect)
		{
			var items = new List<ShellItem>(paths.Count);
			try
			{
				foreach (string path in paths)
					items.Add(new(path));

				IDataObject dataObject = Create(items);
				SetFileDropList(dataObject, paths);
				SetData(dataObject, PreferredDropEffectFormat, preferredDropEffect);
				SetClipboard(dataObject);
			}
			finally
			{
				foreach (ShellItem item in items)
					item.Dispose();
			}
		}

		private static ShellItem? GetCommonParent(IReadOnlyList<ShellItem> items)
		{
			ShellItem? commonParent = items[0].Parent;
			if (commonParent is null)
				return null;

			try
			{
				ShellPidl commonParentPidl = commonParent.PIDL;
				for (int index = 1; index < items.Count; index++)
				{
					using ShellItem? parent = items[index].Parent;
					if (parent is null || !parent.PIDL.Equals(commonParentPidl))
					{
						commonParent.Dispose();
						return null;
					}
				}

				return commonParent;
			}
			catch
			{
				commonParent.Dispose();
				throw;
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
				stream = UniqueComInterfaceMarshaller<IStream>.ConvertToManaged(medium.u.pstm);
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

		private static void SetData(IDataObject dataObject, string formatName, uint value)
		{
			HGLOBAL data = PInvoke.GlobalAlloc(GLOBAL_ALLOC_FLAGS.GMEM_MOVEABLE, sizeof(uint));
			if (data.IsNull)
				throw new OutOfMemoryException();

			bool transferred = false;
			try
			{
				uint* dataPointer = (uint*)PInvoke.GlobalLock(data);
				if (dataPointer is null)
					throw new OutOfMemoryException();
				try
				{
					*dataPointer = value;
				}
				finally
				{
					PInvoke.GlobalUnlock(data);
				}

				SetData(dataObject, checked((ushort)PInvoke.RegisterClipboardFormat(formatName)), data);
				transferred = true;
			}
			finally
			{
				if (!transferred)
					PInvoke.GlobalFree(data);
			}
		}

		private static void SetFileDropList(IDataObject dataObject, IReadOnlyList<string> paths)
		{
			int characterCount = 1;
			foreach (string path in paths)
				characterCount = checked(characterCount + path.Length + 1);

			nuint dataSize = checked((nuint)sizeof(DROPFILES) + (nuint)characterCount * sizeof(char));
			HGLOBAL data = PInvoke.GlobalAlloc(GLOBAL_ALLOC_FLAGS.GMEM_MOVEABLE, dataSize);
			if (data.IsNull)
				throw new OutOfMemoryException();

			bool transferred = false;
			try
			{
				DROPFILES* dropFiles = (DROPFILES*)PInvoke.GlobalLock(data);
				if (dropFiles is null)
					throw new OutOfMemoryException();
				try
				{
					*dropFiles = new()
					{
						pFiles = (uint)sizeof(DROPFILES),
						fWide = true,
					};
					char* destination = (char*)((byte*)dropFiles + dropFiles->pFiles);
					foreach (string path in paths)
					{
						path.AsSpan().CopyTo(new Span<char>(destination, path.Length));
						destination += path.Length;
						*destination++ = '\0';
					}
					*destination = '\0';
				}
				finally
				{
					PInvoke.GlobalUnlock(data);
				}

				SetData(dataObject, CfHDrop, data);
				transferred = true;
			}
			finally
			{
				if (!transferred)
					PInvoke.GlobalFree(data);
			}
		}

		private static void SetData(IDataObject dataObject, ushort formatId, HGLOBAL data)
		{
			FORMATETC format = new()
			{
				cfFormat = formatId,
				dwAspect = (uint)DVASPECT.DVASPECT_CONTENT,
				lindex = -1,
				tymed = (uint)TYMED.TYMED_HGLOBAL,
			};
			STGMEDIUM medium = new()
			{
				tymed = TYMED.TYMED_HGLOBAL,
				u = new() { hGlobal = data },
			};
			dataObject.SetData(format, medium, true).ThrowOnFailure();
		}

		private static void SetClipboard(IDataObject dataObject)
		{
			HRESULT result = default;
			for (int attempt = 0; attempt < ClipboardRetryCount; attempt++)
			{
				result = PInvoke.OleSetClipboard(dataObject);
				if (result.Succeeded)
				{
					result = PInvoke.OleFlushClipboard();
					if (result.Succeeded)
						return;
				}

				if (attempt + 1 < ClipboardRetryCount)
					Thread.Sleep(ClipboardRetryDelay);
			}

			result.ThrowOnFailure();
		}
	}
}
