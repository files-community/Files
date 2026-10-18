// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Data.Enums;
using Files.App.Utils.Storage;
using System.Collections.Concurrent;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;

namespace Files.App.Server;

internal static class IconCache
{
	private static readonly ConcurrentDictionary<(string Extension, bool Folder, int Size), CachedTask<byte[]>> generic = new();

	internal static async Task<IBuffer> GetGenericAsync(string extension, bool folder, int size)
	{
		ValidateSize(size);
		if (generic.Count > 512)
			generic.Clear();
		var key = (extension.ToLowerInvariant(), folder, size);
		var cache = generic.GetOrAdd(key, _ => new CachedTask<byte[]>(async () =>
		{
			var path = folder ? Environment.SystemDirectory : Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "x46696c6573") + extension;
			return await STATask.RunPooled(() => ShellImage.GetIcon(path, size, folder, IconOptions.ReturnIconOnly), Program.Logger) ?? [];
		}, TimeSpan.FromMinutes(5)));
		return (await cache.GetAsync()).AsBuffer();
	}

	internal static async Task<IBuffer> GetAsync(string path, int size, bool folder, uint options, bool overlay)
	{
		ValidateSize(size);
		var data = await STATask.RunPooled(() =>
		{
			var resolved = path.StartsWith(@"\\?\", StringComparison.Ordinal) ? MtpHelpers.ResolveMtpShellPath(path) ?? path : path;
			return overlay ? ShellImage.GetIconOverlay(resolved, size, folder)
				: ShellImage.GetIcon(resolved, size, folder, (IconOptions)options);
		}, Program.Logger);
		return (data ?? []).AsBuffer();
	}

	private static void ValidateSize(int size)
	{
		if (size is < 1 or > 1024)
			throw new ArgumentOutOfRangeException(nameof(size));
	}
}
