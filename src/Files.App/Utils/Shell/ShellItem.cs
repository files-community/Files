// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.System.Com;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.System.SystemServices;
using Windows.Win32.System.Variant;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
using Windows.Win32.UI.Shell.PropertiesSystem;
using Windows.Win32.UI.WindowsAndMessaging;
using Win32ShellLink = Windows.Win32.UI.Shell.ShellLink;

namespace Files.App.Utils.Shell
{
	public readonly record struct IconLocation(string Path, int Index)
	{
		public override string ToString()
			=> $"{Path},{Index}";
	}

	[WinRT.GeneratedWinRTExposedType]
	public sealed partial class ShellPidl : IEnumerable<ShellPidl>
	{
		private readonly byte[] bytes;

		public ShellPidl(byte[] bytes)
		{
			ArgumentNullException.ThrowIfNull(bytes);
			this.bytes = EnsureTerminated(bytes);
		}

		public byte[] GetBytes()
			=> bytes.ToArray();

		public unsafe bool IsParentOf(ShellPidl child, bool immediate)
		{
			fixed (byte* parentPidl = bytes)
			fixed (byte* childPidl = child.bytes)
				return PInvoke.ILIsParent((ITEMIDLIST*)parentPidl, (ITEMIDLIST*)childPidl, immediate);
		}

		public static ShellPidl Combine(ShellPidl parent, ShellPidl child)
		{
			int parentLength = Math.Max(0, parent.bytes.Length - sizeof(ushort));
			byte[] combined = new byte[parentLength + child.bytes.Length];
			parent.bytes.AsSpan(0, parentLength).CopyTo(combined);
			child.bytes.CopyTo(combined, parentLength);
			return new(combined);
		}

		public static ShellPidl FromSegments(IEnumerable<byte[]> segments)
		{
			using var enumerator = segments.GetEnumerator();
			if (!enumerator.MoveNext())
				throw new ArgumentException("The PIDL has no segments.", nameof(segments));

			ShellPidl result = new(enumerator.Current);
			while (enumerator.MoveNext())
				result = Combine(result, new(enumerator.Current));

			return result;
		}

		public IEnumerator<ShellPidl> GetEnumerator()
		{
			int offset = 0;
			while (offset + sizeof(ushort) <= bytes.Length)
			{
				ushort segmentLength = BitConverter.ToUInt16(bytes, offset);
				if (segmentLength is 0 || offset + segmentLength > bytes.Length)
					yield break;

				byte[] segment = new byte[segmentLength + sizeof(ushort)];
				bytes.AsSpan(offset, segmentLength).CopyTo(segment);
				yield return new(segment);
				offset += segmentLength;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();

		public override bool Equals(object? obj)
			=> obj is ShellPidl other && bytes.AsSpan().SequenceEqual(other.bytes);

		public override int GetHashCode()
		{
			HashCode hash = new();
			foreach (byte value in bytes)
				hash.Add(value);
			return hash.ToHashCode();
		}

		private static byte[] EnsureTerminated(byte[] value)
		{
			if (value.Length >= sizeof(ushort) && value[^1] is 0 && value[^2] is 0)
				return value.ToArray();

			byte[] terminated = new byte[value.Length + sizeof(ushort)];
			value.CopyTo(terminated, 0);
			return terminated;
		}
	}

	public unsafe partial class ShellItem : IDisposable
	{
		private IShellItem? shellItem;
		private ShellItemPropertyStore? properties;
		private ShellPidl? pidl;

		public ShellItem(string path)
		{
			HRESULT result = PInvoke.SHCreateItemFromParsingName(path, null, out IShellItem item);
			result.ThrowOnFailure();
			shellItem = item;
		}

		public ShellItem(Guid knownFolderId)
		{
			HRESULT result = PInvoke.SHGetKnownFolderItem(knownFolderId, KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT, null, out IShellItem item);
			result.ThrowOnFailure();
			shellItem = item;
		}

		protected ShellItem(IShellItem shellItem)
		{
			Initialize(shellItem);
		}

		protected ShellItem()
		{
		}

		protected void Initialize(IShellItem item)
		{
			shellItem = item;
			properties = null;
			pidl = null;
		}

		public IShellItem IShellItem
			=> shellItem ?? throw new ObjectDisposedException(nameof(ShellItem));

		public bool IsFolder
			=> HasAttribute(SFGAO_FLAGS.SFGAO_FOLDER);

		public bool IsFileSystem
			=> HasAttribute(SFGAO_FLAGS.SFGAO_FILESYSTEM);

		public bool IsStream
			=> HasAttribute(SFGAO_FLAGS.SFGAO_STREAM);

		public string? FileSystemPath
			=> GetDisplayName(SIGDN.SIGDN_FILESYSPATH);

		public string? ParsingName
			=> GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEPARSING);

		public virtual string? Name
			=> GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY);

		public ShellItem? Parent
		{
			get
			{
				HRESULT result = IShellItem.GetParent(out IShellItem parent);
				return result.Succeeded ? Open(parent) : null;
			}
		}

		public ShellPidl PIDL
			=> pidl ??= GetPidl();

		public ShellItemPropertyStore Properties
			=> properties ??= new(IShellItem);

		public FileSystemInfo? FileInfo
		{
			get
			{
				string? path = FileSystemPath;
				return string.IsNullOrEmpty(path) ? null : IsFolder ? new DirectoryInfo(path) : new FileInfo(path);
			}
		}

		public string? GetDisplayName(SIGDN displayString)
		{
			HRESULT result = IShellItem.GetDisplayName(displayString, out PWSTR value);
			if (result.Failed)
				return null;

			try
			{
				return value.ToString();
			}
			finally
			{
				PInvoke.CoTaskMemFree(value);
			}
		}

		public static ShellItem Open(string path)
		{
			var item = new ShellItem(path);
			return CreateSpecializedItem(item);
		}

		public static ShellItem Open(IShellItem item)
		{
			var shellItem = new ShellItem(item);
			return CreateSpecializedItem(shellItem);
		}

		public unsafe static ShellItem Open(ShellPidl pidl)
		{
			byte[] bytes = pidl.GetBytes();
			fixed (byte* value = bytes)
			{
				HRESULT result = PInvoke.SHCreateItemFromIDList<IShellItem>(in *(ITEMIDLIST*)value, out IShellItem item);
				result.ThrowOnFailure();
				return Open(item);
			}
		}

		public virtual void Dispose()
		{
			properties = null;
			pidl = null;
			shellItem = null;
			GC.SuppressFinalize(this);
		}

		private bool HasAttribute(SFGAO_FLAGS attribute)
			=> IShellItem.GetAttributes(attribute, out SFGAO_FLAGS returned).Succeeded && (returned & attribute) is not 0;

		private unsafe ShellPidl GetPidl()
		{
			HRESULT result = PInvoke.SHGetIDListFromObject(IShellItem, out ITEMIDLIST* value);
			result.ThrowOnFailure();

			try
			{
				uint size = PInvoke.ILGetSize(value);
				byte[] bytes = new byte[size];
				Marshal.Copy((nint)value, bytes, 0, bytes.Length);
				return new(bytes);
			}
			finally
			{
				PInvoke.CoTaskMemFree(value);
			}
		}

		private static ShellLink? TryOpenLink(ShellItem item)
		{
			string? path = item.FileSystemPath;
			if (!string.Equals(Path.GetExtension(path), ".lnk", StringComparison.OrdinalIgnoreCase))
				return null;

			try
			{
				var link = new ShellLink(path!);
				item.Dispose();
				return link;
			}
			catch
			{
				return null;
			}
		}

		private static ShellItem CreateSpecializedItem(ShellItem item)
		{
			if (TryOpenLink(item) is { } link)
				return link;

			if (!item.IsFolder)
				return item;

			var folder = new ShellFolder(item);
			item.Dispose();
			return folder;
		}
	}

	[WinRT.GeneratedWinRTExposedType]
	public partial class ShellFolder : ShellItem, IEnumerable<ShellItem>
	{
		public ShellFolder(string path) : base(path)
		{
		}

		public ShellFolder(Guid knownFolderId) : base(knownFolderId)
		{
		}

		public ShellFolder(ShellItem item) : base(item.IShellItem)
		{
		}

		internal ShellFolder(IShellItem item) : base(item)
		{
		}

		public IEnumerator<ShellItem> GetEnumerator()
		{
			HRESULT result = IShellItem.BindToHandler(null!, PInvoke.BHID_EnumItems, out IEnumShellItems? enumerator);
			if (result.Failed || enumerator is null)
				yield break;

			try
			{
				IShellItem[] items = new IShellItem[1];
				while (enumerator.Next(items) == HRESULT.S_OK)
					yield return Open(items[0]);
			}
			finally
			{
				if ((object)enumerator is ComObject comObject)
					comObject.FinalRelease();
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();
	}

	public sealed partial class ShellLink : ShellItem
	{
		private IShellLinkW? link;
		private IPersistFile? persistFile;
		private string? persistedPath;

		public ShellLink(string linkPath, SLR_FLAGS resolution = SLR_FLAGS.SLR_NO_UI, HWND window = default, TimeSpan timeout = default, bool resolve = true) : base(linkPath)
		{
			link = Win32ShellLink.CreateInstance<IShellLinkW>();
			persistFile = (IPersistFile)link;
			persistFile.Load(linkPath, STGM.STGM_READ).ThrowOnFailure();
			persistedPath = linkPath;

			if (!resolve)
				return;

			uint resolveFlags = (uint)resolution;
			if ((resolveFlags & (uint)SLR_FLAGS.SLR_NO_UI) is not 0 && timeout != default)
			{
				ushort timeoutMilliseconds = checked((ushort)timeout.TotalMilliseconds);
				resolveFlags = (resolveFlags & ushort.MaxValue) | ((uint)timeoutMilliseconds << 16);
			}
			link.Resolve(window, resolveFlags).ThrowOnFailure();
		}

		public ShellLink(string targetPath, string? arguments, string? workingDirectory)
		{
			link = Win32ShellLink.CreateInstance<IShellLinkW>();
			persistFile = (IPersistFile)link;
			link.SetPath(targetPath).ThrowOnFailure();
			link.SetArguments(arguments ?? string.Empty).ThrowOnFailure();
			link.SetWorkingDirectory(workingDirectory ?? string.Empty).ThrowOnFailure();
		}

		private IShellLinkW Link
			=> link ?? throw new ObjectDisposedException(nameof(ShellLink));

		private IPersistFile PersistFile
			=> persistFile ?? throw new ObjectDisposedException(nameof(ShellLink));

		public string TargetPath
		{
			get
			{
				Span<char> path = stackalloc char[32768];
				path.Clear();
				WIN32_FIND_DATAW data = default;
				if (Link.GetPath(path, ref data, (uint)SLGP_FLAGS.SLGP_RAWPATH).Succeeded)
				{
					string result = GetNullTerminatedString(path);
					if (!string.IsNullOrEmpty(result))
						return result;
				}

				using ShellItem? targetItem = TryOpenTarget();
				if (targetItem is not null)
					return targetItem.ParsingName ?? string.Empty;

				return string.Empty;
			}
		}

		/// <summary>
		/// Reads the target's directory attribute from the find data persisted inside the link file,
		/// without opening the target. Returns <see langword="null"/> when the link stores no attributes.
		/// </summary>
		internal bool? StoredTargetIsFolder()
		{
			Span<char> path = stackalloc char[32768];
			path.Clear();
			WIN32_FIND_DATAW data = default;
			if (Link.GetPath(path, ref data, (uint)SLGP_FLAGS.SLGP_RAWPATH).Succeeded && data.dwFileAttributes is not 0)
				return (data.dwFileAttributes & (uint)FileAttributes.Directory) is not 0;

			return null;
		}

		internal bool IsTargetFolder(string expandedTargetPath)
		{
			using ShellItem? targetItem = TryOpenTarget();
			if (targetItem is not null)
				return targetItem.IsFolder;

			try
			{
				using var fallbackTarget = Open(expandedTargetPath);
				return fallbackTarget.IsFolder;
			}
			catch
			{
				return false;
			}
		}

		public string Arguments
		{
			get => GetString(Link.GetArguments);
			set
			{
				Link.SetArguments(value).ThrowOnFailure();
				SaveCurrent();
			}
		}

		public string WorkingDirectory
		{
			get => GetString(Link.GetWorkingDirectory);
			set
			{
				Link.SetWorkingDirectory(value).ThrowOnFailure();
				SaveCurrent();
			}
		}

		public SHOW_WINDOW_CMD ShowState
		{
			get
			{
				Link.GetShowCmd(out SHOW_WINDOW_CMD value).ThrowOnFailure();
				return value;
			}
			set
			{
				Link.SetShowCmd(value).ThrowOnFailure();
				SaveCurrent();
			}
		}

		public bool RunAsAdministrator
		{
			get
			{
				IShellLinkDataList dataList = (IShellLinkDataList)Link;
				dataList.GetFlags(out uint value).ThrowOnFailure();
				return (value & (uint)SHELL_LINK_DATA_FLAGS.SLDF_RUNAS_USER) is not 0;
			}
			set
			{
				IShellLinkDataList dataList = (IShellLinkDataList)Link;
				dataList.GetFlags(out uint flags).ThrowOnFailure();
				flags = value
					? flags | (uint)SHELL_LINK_DATA_FLAGS.SLDF_RUNAS_USER
					: flags & ~(uint)SHELL_LINK_DATA_FLAGS.SLDF_RUNAS_USER;
				dataList.SetFlags(flags).ThrowOnFailure();
				SaveCurrent();
			}
		}

		public IconLocation IconLocation
		{
			get
			{
				Span<char> path = stackalloc char[32768];
				path.Clear();
				Link.GetIconLocation(path, out int index).ThrowOnFailure();
				return new(GetNullTerminatedString(path), index);
			}
			set
			{
				Link.SetIconLocation(value.Path, value.Index).ThrowOnFailure();
				SaveCurrent();
			}
		}

		public void SaveAs(string path)
		{
			PersistFile.Save(path, true).ThrowOnFailure();
			PInvoke.SHCreateItemFromParsingName(path, null, out IShellItem item).ThrowOnFailure();
			Initialize(item);
			persistedPath = path;
		}

		public override void Dispose()
		{
			link = null;
			persistFile = null;
			base.Dispose();
		}

		private delegate HRESULT GetStringDelegate(Span<char> value);

		private static string GetString(GetStringDelegate getter)
		{
			Span<char> value = stackalloc char[32768];
			value.Clear();
			getter(value).ThrowOnFailure();
			return GetNullTerminatedString(value);
		}

		private static string GetNullTerminatedString(Span<char> value)
		{
			int length = value.IndexOf('\0');
			return value[..(length < 0 ? value.Length : length)].ToString();
		}

		private unsafe ShellItem? TryOpenTarget()
		{
			if (Link.GetIDList(out ITEMIDLIST* targetPidl).Failed || targetPidl is null)
				return null;

			try
			{
				return PInvoke.SHCreateItemFromIDList<IShellItem>(in *targetPidl, out IShellItem target).Succeeded ? Open(target) : null;
			}
			finally
			{
				PInvoke.CoTaskMemFree(targetPidl);
			}
		}

		private void SaveCurrent()
		{
			if (persistedPath is not null)
				PersistFile.Save(persistedPath, true).ThrowOnFailure();
		}
	}

	public sealed unsafe class ShellItemPropertyStore
	{
		private readonly IShellItem2? shellItem;

		public ShellItemPropertyStore(IShellItem shellItem)
		{
			this.shellItem = shellItem as IShellItem2;
		}

		public object? this[string propertyName]
		{
			get
			{
				if (shellItem is null || PInvoke.PSGetPropertyKeyFromName(propertyName, out PROPERTYKEY key).Failed)
					return null;

				return GetValue(key);
			}
		}

		public bool TryGetValue<T>(PROPERTYKEY key, out T? value)
		{
			value = default;
			if (shellItem is null)
				return false;

			object? result;
			if (typeof(T) == typeof(string))
			{
				if (shellItem.GetString(key, out PWSTR text).Failed)
					return false;
				try
				{
					result = text.ToString();
				}
				finally
				{
					PInvoke.CoTaskMemFree(text);
				}
			}
			else if (typeof(T) == typeof(bool))
			{
				if (shellItem.GetBool(key, out BOOL boolean).Failed)
					return false;
				result = (bool)boolean;
			}
			else if (typeof(T) == typeof(uint))
			{
				if (shellItem.GetUInt32(key, out uint number).Failed)
					return false;
				result = number;
			}
			else if (typeof(T) == typeof(ulong) || typeof(T) == typeof(ulong?))
			{
				if (shellItem.GetUInt64(key, out ulong number).Failed)
					return false;
				result = number;
			}
			else if (typeof(T) == typeof(System.Runtime.InteropServices.ComTypes.FILETIME) || typeof(T) == typeof(System.Runtime.InteropServices.ComTypes.FILETIME?))
			{
				if (shellItem.GetFileTime(key, out System.Runtime.InteropServices.ComTypes.FILETIME fileTime).Failed)
					return false;
				result = fileTime;
			}
			else
			{
				result = GetValue(key);
				if (result is not T)
					return false;
			}

			value = (T?)result;
			return true;
		}

		public bool TryGetValue<T>(string propertyName, out T? value)
		{
			if (PInvoke.PSGetPropertyKeyFromName(propertyName, out PROPERTYKEY key).Failed)
			{
				value = default;
				return false;
			}

			return TryGetValue(key, out value);
		}

		public string? GetPropertyString(PROPERTYKEY key)
		{
			if (shellItem is null || shellItem.GetProperty(key, out PROPVARIANT value).Failed)
				return null;

			try
			{
				if (PInvoke.PSFormatForDisplayAlloc(key, value, PROPDESC_FORMAT_FLAGS.PDFF_DEFAULT, out PWSTR display).Failed)
					return null;
				try
				{
					return display.ToString();
				}
				finally
				{
					PInvoke.CoTaskMemFree(display);
				}
			}
			finally
			{
				PInvoke.PropVariantClear(ref value);
			}
		}

		public string? GetPropertyString(string propertyName)
			=> PInvoke.PSGetPropertyKeyFromName(propertyName, out PROPERTYKEY key).Succeeded ? GetPropertyString(key) : null;

		private object? GetValue(PROPERTYKEY key)
		{
			if (shellItem is null || shellItem.GetProperty(key, out PROPVARIANT value).Failed)
				return null;

			try
			{
				return value.vt switch
				{
					VARENUM.VT_LPWSTR => value.pwszVal.ToString(),
					VARENUM.VT_BSTR => value.bstrVal.ToString(),
					VARENUM.VT_BOOL => value.boolVal.Value is not 0,
					VARENUM.VT_UI4 => value.ulVal,
					VARENUM.VT_UI8 => value.uhVal,
					VARENUM.VT_I4 => value.lVal,
					VARENUM.VT_FILETIME => value.filetime,
					_ => null,
				};
			}
			finally
			{
				PInvoke.PropVariantClear(ref value);
			}
		}
	}

	public static unsafe class InternetShortcut
	{
		public static string Load(string path)
		{
			IUniformResourceLocatorW shortcut = Create();
			((IPersistFile)shortcut).Load(path, STGM.STGM_READ).ThrowOnFailure();
			shortcut.GetURL(out PWSTR value).ThrowOnFailure();
			try
			{
				return value.ToString();
			}
			finally
			{
				PInvoke.CoTaskMemFree(value);
			}
		}

		public static void Save(string path, string target)
		{
			IUniformResourceLocatorW shortcut = Create();
			shortcut.SetURL(target, (uint)IURL_SETURL_FLAGS.IURL_SETURL_FL_GUESS_PROTOCOL).ThrowOnFailure();
			((IPersistFile)shortcut).Save(path, false).ThrowOnFailure();
		}

		private static IUniformResourceLocatorW Create()
		{
			PInvoke.CoCreateInstance(PInvoke.CLSID_InternetShortcut, null, CLSCTX.CLSCTX_INPROC_SERVER, out IUniformResourceLocatorW? shortcut).ThrowOnFailure();
			return shortcut!;
		}
	}
}
