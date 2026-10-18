// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace Files.App.Server;

internal static class RecycleBinCatalog
{
	private static readonly ConcurrentDictionary<string, CachedTask<RecycleBinInfo>> sizes = new(StringComparer.OrdinalIgnoreCase);
	private static readonly CachedTask<bool> hasItems = new(() => Task.Run(() =>
	{
		var sid = WindowsIdentity.GetCurrent().User?.Value;
		if (string.IsNullOrEmpty(sid))
			return false;
		foreach (var drive in System.IO.DriveInfo.GetDrives())
		{
			try
			{
				if (drive.DriveType == System.IO.DriveType.Network || !drive.IsReady)
					continue;
				var path = Path.Combine(drive.Name, "$RECYCLE.BIN", sid);
				if (Directory.Exists(path) && Directory.EnumerateFiles(path, "$I*").Any())
					return true;
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
			}
		}
		return false;
	}), TimeSpan.FromSeconds(5));

	internal static Task<bool> HasItemsAsync() => hasItems.GetAsync();

	internal static Task<RecycleBinInfo> QueryAsync(string path) => sizes.GetOrAdd(path,
		key => new CachedTask<RecycleBinInfo>(() => Task.Run(() =>
		{
			SHQUERYRBINFO info = default;
			info.cbSize = (uint)Marshal.SizeOf<SHQUERYRBINFO>();
			var available = PInvoke.SHQueryRecycleBin(key, ref info) == HRESULT.S_OK;
			return new RecycleBinInfo(available, info.i64NumItems, info.i64Size);
		}), TimeSpan.FromSeconds(5))).GetAsync();

	internal static void Invalidate()
	{
		hasItems.Invalidate();
		foreach (var cache in sizes.Values)
			cache.Invalidate();
	}
}

public sealed partial class RecycleBinInfo
{
	public bool Available { get; }
	public long Count { get; }
	public long Size { get; }

	internal RecycleBinInfo(bool available, long count, long size)
	{
		Available = available;
		Count = count;
		Size = size;
	}
}
