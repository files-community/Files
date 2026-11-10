// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.Storage;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.IO;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;

namespace Files.App.Utils.Shell
{
	/// <summary>
	/// Represents a column a shell folder offers, as in the Details view of File Explorer.
	/// </summary>
	/// <param name="Id">The canonical name of the shell property, such as "System.Size".</param>
	/// <param name="IsPrimary">Whether File Explorer offers the column directly, instead of only under "More".</param>
	public sealed record ShellFolderColumn(string Id, string DisplayName, bool CanGroup, bool IsPrimary);

	/// <summary>
	/// Reads the columns a location offers from its shell folder, so each location shows its own columns.
	/// </summary>
	public static unsafe class ShellFolderColumns
	{
		private const uint MaxColumnCount = 1024;

		private static readonly ConcurrentDictionary<string, Task<IReadOnlyList<ShellFolderColumn>>> _cache = new(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Gets the columns of the specified location, or an empty list when the shell doesn't provide them.
		/// </summary>
		public static Task<IReadOnlyList<ShellFolderColumn>> GetAsync(string? path)
		{
			if (string.IsNullOrEmpty(path))
				return Task.FromResult<IReadOnlyList<ShellFolderColumn>>([]);

			var isFileSystemPath = IsFileSystemPath(path);
			if (!isFileSystemPath && !path.EndsWith(".library-ms", StringComparison.OrdinalIgnoreCase) && !path.StartsWith("shell:", StringComparison.OrdinalIgnoreCase) && !path.StartsWith("::", StringComparison.Ordinal))
				return Task.FromResult<IReadOnlyList<ShellFolderColumn>>([]);

			// Regular folders are read at their drive root
			var source = isFileSystemPath ? Path.GetPathRoot(path) ?? path : path;
			return _cache.GetOrAdd(source, ReadAsync);
		}

		/// <summary>
		/// Gets whether the specified location is a regular folder.
		/// </summary>
		public static bool IsFileSystemPath(string path)
			=> !path.EndsWith(".library-ms", StringComparison.OrdinalIgnoreCase) && Path.IsPathFullyQualified(path);

		private static Task<IReadOnlyList<ShellFolderColumn>> ReadAsync(string path)
		{
			return STATask.RunPooled(() => Read(path), App.Logger).ContinueWith(task =>
			{
				// Read again next time when the location wasn't available
				if (task.Result.Count == 0)
					_cache.TryRemove(path, out _);

				return task.Result;
			}, TaskScheduler.Default);
		}

		private static IReadOnlyList<ShellFolderColumn> Read(string path)
		{
			var columns = new List<ShellFolderColumn>();
			try
			{
				using var shellItem = new ShellItem(path);
				if (shellItem.IShellItem.BindToHandler(null, PInvoke.BHID_SFObject, out IShellFolder? shellFolder).Failed ||
					shellFolder is not IShellFolder2 folder)
					return columns;

				Span<char> header = stackalloc char[256];
				for (uint index = 0; index < MaxColumnCount; index++)
				{
					var details = default(SHELLDETAILS);
					if (folder.GetDetailsOf(null, index, &details).Failed)
						break;

					if (folder.MapColumnToSCID(index, out var propertyKey).Failed ||
						folder.GetDefaultColumnState(index, out var state).Failed ||
						state.HasFlag(SHCOLSTATE.SHCOLSTATE_HIDDEN) ||
						GetPropertyName(propertyKey) is not { } id)
						continue;

					header.Clear();
					var displayName = PInvoke.StrRetToBuf(ref details.str, null, header).Succeeded ? header.ToString().TrimEnd('\0') : string.Empty;
					columns.Add(new(
						id,
						string.IsNullOrWhiteSpace(displayName) ? id : displayName,
						!state.HasFlag(SHCOLSTATE.SHCOLSTATE_NO_GROUPBY),
						!state.HasFlag(SHCOLSTATE.SHCOLSTATE_SECONDARYUI)));
				}
			}
			catch (Exception ex)
			{
				App.Logger.LogDebug(ex, "Failed to read the columns of a shell folder.");
			}

			return columns;
		}

		private static string? GetPropertyName(PROPERTYKEY propertyKey)
		{
			if (PInvoke.PSGetNameFromPropertyKey(propertyKey, out var name).Failed)
				return null;

			try
			{
				return name.ToString() is { Length: > 0 } value ? value : null;
			}
			finally
			{
				PInvoke.CoTaskMemFree(name.Value);
			}
		}
	}
}
