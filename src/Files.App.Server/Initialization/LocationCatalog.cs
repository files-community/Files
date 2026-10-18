// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Windows.Win32;
using Windows.Win32.UI.Shell;

namespace Files.App.Server;

internal static class LocationCatalog
{
	internal static readonly CachedTask<IReadOnlyList<ShellLocationInfo>> RecentFiles = new(() => ReadAsync("shell:::{679F85CB-0220-4080-B29B-5540CC05AAB6}", false, 20), TimeSpan.FromMinutes(5));
	internal static readonly CachedTask<IReadOnlyList<ShellLocationInfo>> RecentFolders = new(() => ReadAsync("shell:::{22877A6D-37A1-461A-91B0-DBDA5AAEBC99}", false, 20), TimeSpan.FromMinutes(5));
	internal static readonly CachedTask<IReadOnlyList<ShellLocationInfo>> NetworkComputers = new(() => ReadAsync("shell:::{f02c1a0d-be21-4350-88b0-7367fc96ef3c}", true, int.MaxValue), TimeSpan.FromSeconds(30));
	internal static readonly CachedTask<IReadOnlyList<ShellLocationInfo>> NetworkShortcuts = new(() => ReadAsync("shell:::{c5abbf53-e17f-4121-8900-86626fc2c973}", true, int.MaxValue, true), TimeSpan.FromMinutes(5));

	private static Task<IReadOnlyList<ShellLocationInfo>> ReadAsync(string path, bool foldersOnly, int limit, bool links = false)
		=> STATask.Run<IReadOnlyList<ShellLocationInfo>>(() =>
		{
			var result = new List<ShellLocationInfo>();
			using var folder = new ShellFolder(path);
			foreach (var item in folder)
			{
				using (item)
				{
					try
					{
						if (!links && (foldersOnly ? !item.IsFolder : item.IsFolder && !item.IsStream))
							continue;
						var identity = item.ParsingName ?? item.FileSystemPath ?? string.Empty;
						var target = links
							? item is Files.App.Utils.Shell.ShellLink link ? link.TargetPath : item.Properties["System.Link.TargetParsingPath"] as string
							: item.GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEEDITING);
						if (string.IsNullOrEmpty(target))
							continue;
						var date = SafetyExtensions.IgnoreExceptions(() => item.Properties["System.DateModified"], Program.Logger);
						var modified = date switch
						{
							DateTimeOffset value => value,
							DateTime value => new DateTimeOffset(value),
							_ => DateTimeOffset.MinValue,
						};
						result.Add(new ShellLocationInfo(target, identity, item.Name ?? string.Empty, item.PIDL.GetBytes(), modified));
						if (result.Count >= limit)
							break;
					}
					catch (Exception ex)
					{
						Program.Logger.LogDebug(ex, "Could not read a shell location.");
					}
				}
			}
			return result.AsReadOnly();
		}, Program.Logger);
}

public sealed partial class ShellLocationInfo
{
	public string Path { get; }
	public string Identity { get; }
	public string Name { get; }
	public byte[] Pidl { get; }
	public DateTimeOffset Modified { get; }

	internal ShellLocationInfo(string path, string identity, string name, byte[] pidl, DateTimeOffset modified)
	{
		Path = path;
		Identity = identity;
		Name = name;
		Pidl = pidl;
		Modified = modified;
	}
}
