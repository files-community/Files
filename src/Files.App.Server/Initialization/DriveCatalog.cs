// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Concurrent;
using Files.Core.Storage.EventArguments;
using Windows.Devices.Enumeration;
using Windows.Devices.Portable;
using Windows.Storage;
using Files.App.Data.Enums;
using Files.App.Utils.Storage;

namespace Files.App.Server;

internal static class DriveCatalog
{
	private static readonly ConcurrentDictionary<string, byte> portable = new(StringComparer.OrdinalIgnoreCase);
	private static readonly ConcurrentDictionary<string, (string Id, StorageFolder Root)> portableRoots = new(StringComparer.OrdinalIgnoreCase);
	private static readonly ConcurrentDictionary<string, CachedTask<DriveItemInfo?>> drives = new(StringComparer.OrdinalIgnoreCase);
	private static readonly ConcurrentDictionary<(string Path, bool Cloud), CachedTask<DrivePropertiesInfo>> properties = new();
	private static readonly Lazy<Task> starting = new(() => ServerHost.InvokeAsync(Start));
	private static DeviceWatcher? watcher;

	internal static async Task<IReadOnlyList<string>> GetIdsAsync()
	{
		await starting.Value;
		return System.IO.DriveInfo.GetDrives().Select(drive => drive.Name).Concat(portable.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
	}

	private static void Start()
	{
		watcher = DeviceInformation.CreateWatcher(StorageDevice.GetDeviceSelector());
		watcher.Added += (_, item) =>
		{
			portable[item.Id] = 0;
			Changed(item.Id);
		};
		watcher.Removed += (sender, item) =>
		{
			portable.TryRemove(item.Id, out _);
			Changed(item.Id);
		};
		watcher.Updated += (_, item) => Changed(item.Id);
		watcher.EnumerationCompleted += (_, _) => ServerEvents.Notify("Drives");
		watcher.Start();
		WindowsDriveManager.Default.DeviceAdded += OnDriveChanged;
		WindowsDriveManager.Default.DeviceRemoved += OnDriveChanged;
		WindowsDriveManager.Default.DeviceInserted += OnDriveChanged;
		WindowsDriveManager.Default.DeviceEjected += OnDriveChanged;
		WindowsDriveManager.Default.Start();
	}

	private static void OnDriveChanged(object? sender, DeviceEventArgs e) => Changed(e.DeviceId.TrimEnd('\\') + "\\");

	private static void Changed(string id)
	{
		MtpHelpers.ClearCache();
		drives.TryRemove(id, out _);
		foreach (var pair in portableRoots.Where(pair => pair.Value.Id == id))
			portableRoots.TryRemove(pair.Key, out _);
		properties.Clear();
		ServerEvents.Notify("Drives");
	}

	internal static Task<DriveItemInfo?> GetAsync(string id) => drives.GetOrAdd(id,
		key => new CachedTask<DriveItemInfo?>(() => LoadAsync(key), TimeSpan.FromMinutes(1), allowNull: true)).GetAsync();

	private static async Task<DriveItemInfo?> LoadAsync(string id)
	{
		return await SafetyExtensions.IgnoreExceptions(async () =>
		{
			var isPortable = portable.ContainsKey(id);
			StorageFolder root;
			string label = string.Empty;
			var kind = DriveType.Removable;
			if (isPortable)
			{
				root = StorageDevice.FromId(id);
				if (!string.IsNullOrEmpty(root.Path) && !root.Path.StartsWith(@"\\?\", StringComparison.Ordinal))
					return await GetAsync(root.Path);
				var path = string.IsNullOrEmpty(root.Path) ? $"\\\\?\\{root.Name}\\" : root.Path;
				portableRoots[path] = (id, root);
			}
			else
			{
				var info = await Task.Factory.StartNew(() =>
				{
					var drive = new System.IO.DriveInfo(id);
					return (Ready: drive.IsReady, Label: DriveLabel.GetExtendedDriveLabel(drive), Type: drive.DriveType);
				}, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
				if (!info.Ready)
					return null;
				label = info.Label;
				kind = info.Type switch
				{
					System.IO.DriveType.Fixed => DriveType.Fixed,
					System.IO.DriveType.Removable => DriveType.Removable,
					System.IO.DriveType.Network => DriveType.Network,
					System.IO.DriveType.Ram => DriveType.Ram,
					System.IO.DriveType.CDRom => DriveType.CDRom,
					System.IO.DriveType.NoRootDirectory => DriveType.NoRootDirectory,
					_ => id.TrimEnd('\\') is "A:" or "B:" ? DriveType.FloppyDisk : DriveType.Unknown,
				};
				root = await StorageFolder.GetFolderFromPathAsync(id);
			}
			var name = kind == DriveType.CDRom && label.Length > 0 ? root.DisplayName.Replace(label[..Math.Min(32, label.Length)], label) : root.DisplayName;
			var icon = await STATask.RunPooled(() =>
			{
				var path = string.IsNullOrEmpty(root.Path) ? $"\\\\?\\{root.Name}\\" : root.Path;
				if (path.StartsWith(@"\\?\", StringComparison.Ordinal))
					path = MtpHelpers.ResolveMtpShellPath(path) ?? path;
				return ShellImage.GetIcon(path, 40, true, IconOptions.ReturnIconOnly) ?? [];
			}, Program.Logger) ?? [];

			return new DriveItemInfo(id, root, name, label, kind, icon);
		}, Program.Logger);
	}

	internal static Task<DrivePropertiesInfo> GetPropertiesAsync(string path, bool cloud) => properties.GetOrAdd((path, cloud),
		key => new CachedTask<DrivePropertiesInfo>(() => LoadPropertiesAsync(key.Path, key.Cloud), TimeSpan.FromSeconds(5))).GetAsync();

	private static async Task<DrivePropertiesInfo> LoadPropertiesAsync(string path, bool cloud)
	{
		if (cloud)
		{
			var quota = await SafetyExtensions.IgnoreExceptions(() => SyncRootHelpers.GetSyncRootQuotaAsync(path), Program.Logger);
			if (quota.Success)
				return new DrivePropertiesInfo(true, quota.Capacity, quota.Capacity >= quota.Used ? quota.Capacity - quota.Used : 0, string.Empty);
		}
		var root = portableRoots.TryGetValue(path, out var device) ? device.Root : await StorageFolder.GetFolderFromPathAsync(path);
		var values = await root.Properties.RetrievePropertiesAsync(new[] { "System.FreeSpace", "System.Capacity", "System.Volume.FileSystem" }).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
		return new DrivePropertiesInfo(values["System.Capacity"] is ulong && values["System.FreeSpace"] is ulong,
			values["System.Capacity"] is ulong capacity ? capacity : 0,
			values["System.FreeSpace"] is ulong free ? free : 0,
			values["System.Volume.FileSystem"] as string ?? string.Empty);
	}

	internal static void Stop()
	{
		if (watcher is null)
			return;
		if (watcher.Status is DeviceWatcherStatus.Started or DeviceWatcherStatus.EnumerationCompleted)
			watcher.Stop();
		WindowsDriveManager.Default.DeviceAdded -= OnDriveChanged;
		WindowsDriveManager.Default.DeviceRemoved -= OnDriveChanged;
		WindowsDriveManager.Default.DeviceInserted -= OnDriveChanged;
		WindowsDriveManager.Default.DeviceEjected -= OnDriveChanged;
		WindowsDriveManager.Default.Dispose();
	}
}

public sealed partial class DriveItemInfo
{
	public string Id { get; }
	public StorageFolder Root { get; }
	public string Name { get; }
	public string Label { get; }
	public DriveType Kind { get; }
	public byte[] Icon { get; }

	internal DriveItemInfo(string id, StorageFolder root, string name, string label, DriveType kind, byte[] icon)
	{
		Id = id;
		Root = root;
		Name = name;
		Label = label;
		Kind = kind;
		Icon = icon;
	}
}

public sealed partial class DrivePropertiesInfo
{
	public bool Available { get; }
	public ulong Capacity { get; }
	public ulong FreeSpace { get; }
	public string FileSystem { get; }

	internal DrivePropertiesInfo(bool available, ulong capacity, ulong freeSpace, string fileSystem)
	{
		Available = available;
		Capacity = capacity;
		FreeSpace = freeSpace;
		FileSystem = fileSystem;
	}
}
