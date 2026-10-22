// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Server.Cloud;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.Win32.UI.Shell;

namespace Files.App.Server;

internal static class InitializationCache
{
	private static readonly ShellNewRegistryMonitor shellNewRegistry = new();
	private static readonly RegistryChangeMonitor wslRegistry = new(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Lxss");
	private static readonly TimeSpan lifetime = TimeSpan.FromMinutes(5);

	// Arrays of runtime classes have no WinRT runtime class name for cross-process marshaling.
	internal static readonly CachedTask<IReadOnlyList<ShellNewEntryInfo>> ShellNew = new(async () =>
		(await ShellNewMenuHelper.GetNewContextMenuEntries()).Select(entry => new ShellNewEntryInfo(entry)).ToList().AsReadOnly(),
		lifetime, shellNewRegistry.HasChanged);

	internal static readonly CachedTask<IReadOnlyList<string>> Wsl = new(() =>
	{
		using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Lxss");
		var names = new List<string>();
		foreach (var subKeyName in key?.GetSubKeyNames() ?? [])
		{
			using var distro = key!.OpenSubKey(subKeyName);
			if (distro?.GetValue("DistributionName") is string { Length: > 0 } name)
				names.Add(name);
		}
		return Task.FromResult<IReadOnlyList<string>>(names.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
	}, lifetime, wslRegistry.HasChanged);

	internal static readonly CachedTask<IReadOnlyList<LibraryInfo>> Libraries = new(() => STATask.Run<IReadOnlyList<LibraryInfo>>(() =>
	{
		var result = new List<ShellLibraryItem>();
		if (Directory.Exists(ShellLibraryItem.LibrariesPath))
		{
			foreach (var file in Directory.EnumerateFiles(ShellLibraryItem.LibrariesPath, "*.library-ms"))
			{
				using var item = ShellItem.Open(file);
				using var library = new ShellLibraryEx(item.IShellItem, true);
				var snapshot = new ShellLibraryItem
				{
					FullPath = file,
					AbsolutePath = library.GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEPARSING),
					RelativePath = library.GetDisplayName(SIGDN.SIGDN_PARENTRELATIVEPARSING),
					DisplayName = library.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY),
					IsPinned = library.PinnedToNavigationPane,
				};
				if (library.Folders.Count > 0)
				{
					using var defaultFolder = SafetyExtensions.IgnoreExceptions(() => library.DefaultSaveFolder);
					snapshot.DefaultSaveFolder = defaultFolder?.FileSystemPath ?? string.Empty;
					snapshot.Folders = library.Folders.Select(folder => folder.FileSystemPath).OfType<string>().ToArray();
				}
				result.Add(snapshot);
			}
		}
		return result.Select(library => new LibraryInfo(library)).ToList().AsReadOnly();
	}, Program.Logger), lifetime, new FileStampMonitor(() => Directory.Exists(ShellLibraryItem.LibrariesPath)
		? Directory.EnumerateFiles(ShellLibraryItem.LibrariesPath, "*.library-ms") : Array.Empty<string>()).HasChanged);

	internal static readonly CachedTask<IReadOnlyList<PinnedFolderInfo>> PinnedFolders = new(() => STATask.Run<IReadOnlyList<PinnedFolderInfo>>(() =>
	{
		var result = new List<PinnedFolderInfo>();
		using var folder = new ShellFolder("shell:::{3936e9e4-d92c-4eee-a85a-bc16d5ea0819}");
		foreach (var item in folder)
		{
			try
			{
				using (item)
				{
					var isFolder = item is Files.Storage.ShellLink link
						? link.StoredTargetIsFolder() ?? link.IsTargetFolder(Environment.ExpandEnvironmentVariables(link.TargetPath))
						: item.IsFolder && !item.IsStream;
					if (!isFolder)
						continue;

					var parsingPath = item.GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEPARSING) ?? item.FileSystemPath;
					if (string.IsNullOrEmpty(parsingPath) || !Path.IsPathRooted(parsingPath))
					{
						parsingPath = parsingPath switch
						{
							"::{645FF040-5081-101B-9F08-00AA002F954E}" => "Shell:RecycleBinFolder",
							"::{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}" or "::{208D2C60-3AEA-1069-A2D7-08002B30309D}" => "Shell:NetworkPlacesFolder",
							"::{20D04FE0-3AEA-1069-A2D8-08002B30309D}" => "Shell:MyComputerFolder",
							{ } path when path.StartsWith("::{031E4825-7B94-4DC3-B131-E946B44C8DD5}\\", StringComparison.OrdinalIgnoreCase)
								=> Path.Combine(ShellLibraryItem.LibrariesPath, Path.GetFileName(path)),
							_ => $@"\\SHELL\{string.Join("\\", item.PIDL.Select(segment => Convert.ToBase64String(segment.GetBytes()).Replace('/', '_')))}"
						};
					}
					var storable = new WindowsFolder(item.IShellItem);
					string? tooltip = null;
					SafetyExtensions.IgnoreExceptions(() => storable.TryGetShellTooltip(out tooltip), Program.Logger);
					result.Add(new PinnedFolderInfo(
						Path.IsPathRooted(item.Name) ? item.Name! : parsingPath,
						item.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY) ?? string.Empty,
						item.PIDL.GetBytes(),
						(bool?)SafetyExtensions.IgnoreExceptions(() => item.Properties["System.Home.IsPinned"], Program.Logger) ?? false,
						item.ParsingName ?? parsingPath, tooltip ?? string.Empty));
				}
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or COMException)
			{
				Program.Logger.LogWarning(ex, "Could not read a pinned folder.");
			}
		}
		return result.AsReadOnly();
	}, Program.Logger), lifetime, new FileStampMonitor(() => (string[])[Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Recent\AutomaticDestinations\f01b4d95cf55d32a.automaticDestinations-ms")]).HasChanged);

	private static readonly AbstractCloudDetector[] detectors =
	[
		new GoogleDriveCloudDetector(), new DropBoxCloudDetector(), new BoxCloudDetector(),
		new GenericCloudDetector(), new SynologyDriveCloudDetector(), new OXDriveCloudDetector()
	];

	internal static readonly CachedTask<IReadOnlyList<CloudProviderInfo>>[] CloudProviders = detectors.Select(detector => new CachedTask<IReadOnlyList<CloudProviderInfo>>(async () =>
		(await detector.DetectCloudProvidersAsync()).Select(provider => new CloudProviderInfo(provider)).ToList().AsReadOnly(),
		lifetime, CreateCloudMonitor(detector))).ToArray();

	private static Func<bool> CreateCloudMonitor(AbstractCloudDetector detector)
	{
		var local = UserDataPaths.GetDefault().LocalAppData;
		var files = new FileStampMonitor(() => detector switch
		{
			GoogleDriveCloudDetector => (string[])[Path.Combine(local, @"Google\DriveFS\root_preference_sqlite.db"), Path.Combine(local, @"Google\DriveFS\root_preference_sqlite.db-wal")],
			DropBoxCloudDetector => Directory.Exists(Path.Combine(local, "Packages"))
				? Directory.EnumerateDirectories(Path.Combine(local, "Packages"), "DropboxInc.Dropbox*")
					.Select(path => Path.Combine(path, @"LocalCache\Local\Dropbox\info.json")).Append(Path.Combine(local, @"Dropbox\info.json"))
				: (string[])[Path.Combine(local, @"Dropbox\info.json")],
			BoxCloudDetector => (string[])[Path.Combine(local, @"Box\Box\data\shell\sync_root_folder.txt"), Path.Combine(local, @"Box Sync\sync_root_folder.txt")],
			SynologyDriveCloudDetector => (string[])[Path.Combine(local, @"SynologyDrive\data\db\sys.sqlite"), Path.Combine(local, @"SynologyDrive\data\db\sys.sqlite-wal")],
			OXDriveCloudDetector => (string[])[Path.Combine(local, @"Open-Xchange\OXDrive\userConfig.json")],
			_ => Array.Empty<string>()
		});
		RegistryChangeMonitor[] registry = detector switch
		{
			GoogleDriveCloudDetector => [new(Registry.CurrentUser, @"Software\Google\DriveFS")],
			OXDriveCloudDetector => [new(Registry.LocalMachine, @"Software\WOW6432Node\Open-Xchange\OXDrive")],
			GenericCloudDetector =>
			[
				new(Registry.CurrentUser, @"Software\Yandex\Yandex.Disk.2"),
				new(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace"),
				new(Registry.CurrentUser, @"Software\Microsoft\OneDrive\Accounts"),
				new(Registry.CurrentUser, @"Software\pCloud"),
				new(Registry.CurrentUser, @"Software\Nutstore"),
				new(Registry.CurrentUser, @"Software\SeaDrive\Seafile Drive Client\Settings"),
				new(Registry.CurrentUser, @"Software\Classes\CLSID"),
				new(Registry.LocalMachine, @"Software\Classes\CLSID"),
				new(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Explorer\SyncRootManager"),
				new(Registry.LocalMachine, @"Software\Autodesk\Desktop Connector"),
			],
			_ => []
		};
		return () =>
		{
			var changed = files.HasChanged();
			foreach (var monitor in registry)
				changed |= monitor.HasChanged();
			return changed;
		};
	}

	internal static Task InitializeAsync() => Task.WhenAll(
		[ShellNew.GetAsync(), Libraries.GetAsync(), PinnedFolders.GetAsync()]);
}
