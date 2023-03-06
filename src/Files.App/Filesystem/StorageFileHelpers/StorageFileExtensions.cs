using Files.App.Extensions;
using Files.App.Filesystem.StorageItems;
using Files.App.Helpers;
using Files.App.Views;
using Files.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;

namespace Files.App.Filesystem
{
	public static class StorageFileExtensions
	{
		public static readonly ImmutableHashSet<string> _ftpPaths =
			new HashSet<string>() { "ftp:/", "ftps:/", "ftpes:/" }.ToImmutableHashSet();

		public static BaseStorageFile? AsBaseStorageFile(this IStorageItem item)
		{
			if (item is null || !item.IsOfType(StorageItemTypes.File))
				return null;

			return item is StorageFile file ? (BaseStorageFile)file : item as BaseStorageFile;
		}

		public static async Task<List<IStorageItem>> ToStandardStorageItemsAsync(this IEnumerable<IStorageItem> items)
		{
			var newItems = new List<IStorageItem>();
			foreach (var item in items)
			{
				try
				{
					if (item is null)
					{
					}
					else if (item.IsOfType(StorageItemTypes.File))
					{
						newItems.Add(await item.AsBaseStorageFile().ToStorageFileAsync());
					}
					else if (item.IsOfType(StorageItemTypes.Folder))
					{
						newItems.Add(await item.AsBaseStorageFolder().ToStorageFolderAsync());
					}
				}
				catch (NotSupportedException)
				{
					// Ignore items that can't be converted
				}
			}
			return newItems;
		}

		public static bool AreItemsInSameDrive(this IEnumerable<string> itemsPath, string destinationPath)
		{
			try
			{
				var destinationRoot = Path.GetPathRoot(destinationPath);
				return itemsPath.Any(itemPath => Path.GetPathRoot(itemPath).Equals(destinationRoot, StringComparison.OrdinalIgnoreCase));
			}
			catch
			{
				return false;
			}
		}
		public static bool AreItemsInSameDrive(this IEnumerable<IStorageItem> storageItems, string destinationPath)
			=> storageItems.Select(x => x.Path).AreItemsInSameDrive(destinationPath);
		public static bool AreItemsInSameDrive(this IEnumerable<IStorageItemWithPath> storageItems, string destinationPath)
			=> storageItems.Select(x => x.Path).AreItemsInSameDrive(destinationPath);

		public static bool AreItemsAlreadyInFolder(this IEnumerable<string> itemsPath, string destinationPath)
		{
			try
			{
				var trimmedDestination = destinationPath.TrimPath();
				return itemsPath.All(itemPath => Path.GetDirectoryName(itemPath).Equals(trimmedDestination, StringComparison.OrdinalIgnoreCase));
			}
			catch
			{
				return false;
			}
		}
		public static bool AreItemsAlreadyInFolder(this IEnumerable<IStorageItem> storageItems, string destinationPath)
			=> storageItems.Select(x => x.Path).AreItemsAlreadyInFolder(destinationPath);
		public static bool AreItemsAlreadyInFolder(this IEnumerable<IStorageItemWithPath> storageItems, string destinationPath)
			=> storageItems.Select(x => x.Path).AreItemsAlreadyInFolder(destinationPath);

		public static BaseStorageFolder? AsBaseStorageFolder(this IStorageItem item)
		{
			if (item is not null && item.IsOfType(StorageItemTypes.Folder))
			{
				return item is StorageFolder folder
					? (BaseStorageFolder)folder
					: item as BaseStorageFolder;
			}

			return null;
		}

		public static List<PathBoxItem> GetDirectoryPathComponents(string value)
		{
			List<PathBoxItem> pathBoxItems = new();

			value = NormalizePath(value);

			var (components, paths) = GetComponentsAndRelativePaths(value);
			for (int i = 0; i < components.Count; i++)
			{
				if (!_ftpPaths.Contains(paths[i], StringComparer.OrdinalIgnoreCase))
					pathBoxItems.Add(GetPathItem(components[i], paths[i]));
			}

			return pathBoxItems;
		}

		public static string GetResolvedPath(string path)
		{
			var withoutEnvironment = GetPathWithoutEnvironmentVariable(path);
			return ResolvePath(withoutEnvironment);
		}

		public async static Task<BaseStorageFile> DangerousGetFileFromPathAsync
			(string value, StorageFolderWithPath rootFolder = null, StorageFolderWithPath parentFolder = null)
				=> (await DangerousGetFileWithPathFromPathAsync(value, rootFolder, parentFolder)).Item;
		public async static Task<StorageFileWithPath> DangerousGetFileWithPathFromPathAsync
			(string value, StorageFolderWithPath rootFolder = null, StorageFolderWithPath parentFolder = null)
		{
			if (rootFolder is not null)
			{
				var currComponents = GetDirectoryPathComponents(value);

				if (parentFolder is not null && value.IsSubPathOf(parentFolder.Path))
				{
					var folder = parentFolder.Item;
					var prevComponents = GetDirectoryPathComponents(parentFolder.Path);
					var path = parentFolder.Path;
					foreach (var component in currComponents.ExceptBy(prevComponents, c => c.Path).SkipLast(1))
					{
						folder = await folder.GetFolderAsync(component.Title);
						path = PathNormalization.Combine(path, folder.Name);
					}
					var file = await folder.GetFileAsync(currComponents.Last().Title);
					path = PathNormalization.Combine(path, file.Name);
					return new StorageFileWithPath(file, path);
				}
				else if (value.IsSubPathOf(rootFolder.Path))
				{
					var folder = rootFolder.Item;
					var path = rootFolder.Path;
					foreach (var component in currComponents.Skip(1).SkipLast(1))
					{
						folder = await folder.GetFolderAsync(component.Title);
						path = PathNormalization.Combine(path, folder.Name);
					}
					var file = await folder.GetFileAsync(currComponents.Last().Title);
					path = PathNormalization.Combine(path, file.Name);
					return new StorageFileWithPath(file, path);
				}
			}

			// "::{" not a valid root
			if (parentFolder is not null && !Path.IsPathRooted(value) && !ShellStorageFolder.IsShellPath(value))
			{
				// Relative path
				var fullPath = Path.GetFullPath(Path.Combine(parentFolder.Path, value));
				return new StorageFileWithPath(await BaseStorageFile.GetFileFromPathAsync(fullPath));
			}
			return new StorageFileWithPath(await BaseStorageFile.GetFileFromPathAsync(value));
		}
		public async static Task<IList<StorageFileWithPath>> GetFilesWithPathAsync
			(this StorageFolderWithPath parentFolder, uint maxNumberOfItems = uint.MaxValue)
				=> (await parentFolder.Item.GetFilesAsync(CommonFileQuery.DefaultQuery, 0, maxNumberOfItems))
					.Select(x => new StorageFileWithPath(x, string.IsNullOrEmpty(x.Path) ? PathNormalization.Combine(parentFolder.Path, x.Name) : x.Path)).ToList();

		public async static Task<BaseStorageFolder> DangerousGetFolderFromPathAsync
			(string value, StorageFolderWithPath rootFolder = null, StorageFolderWithPath parentFolder = null)
				=> (await DangerousGetFolderWithPathFromPathAsync(value, rootFolder, parentFolder)).Item;
		public async static Task<StorageFolderWithPath> DangerousGetFolderWithPathFromPathAsync
			(string value, StorageFolderWithPath rootFolder = null, StorageFolderWithPath parentFolder = null)
		{
			if (rootFolder is not null)
			{
				var currComponents = GetDirectoryPathComponents(value);

				if (rootFolder.Path == value)
				{
					return rootFolder;
				}
				else if (parentFolder is not null && value.IsSubPathOf(parentFolder.Path))
				{
					var folder = parentFolder.Item;
					var prevComponents = GetDirectoryPathComponents(parentFolder.Path);
					var path = parentFolder.Path;
					foreach (var component in currComponents.ExceptBy(prevComponents, c => c.Path))
					{
						folder = await folder.GetFolderAsync(component.Title);
						path = PathNormalization.Combine(path, folder.Name);
					}
					return new StorageFolderWithPath(folder, path);
				}
				else if (value.IsSubPathOf(rootFolder.Path))
				{
					var folder = rootFolder.Item;
					var path = rootFolder.Path;
					foreach (var component in currComponents.Skip(1))
					{
						folder = await folder.GetFolderAsync(component.Title);
						path = PathNormalization.Combine(path, folder.Name);
					}
					return new StorageFolderWithPath(folder, path);
				}
			}

			if (parentFolder is not null && !Path.IsPathRooted(value) && !ShellStorageFolder.IsShellPath(value)) // "::{" not a valid root
			{
				// Relative path
				var fullPath = Path.GetFullPath(Path.Combine(parentFolder.Path, value));
				return new StorageFolderWithPath(await BaseStorageFolder.GetFolderFromPathAsync(fullPath));
			}
			else
			{
				return new StorageFolderWithPath(await BaseStorageFolder.GetFolderFromPathAsync(value));
			}
		}
		public async static Task<IList<StorageFolderWithPath>> GetFoldersWithPathAsync
			(this StorageFolderWithPath parentFolder, uint maxNumberOfItems = uint.MaxValue)
				=> (await parentFolder.Item.GetFoldersAsync(CommonFolderQuery.DefaultQuery, 0, maxNumberOfItems))
					.Select(x => new StorageFolderWithPath(x, string.IsNullOrEmpty(x.Path) ? PathNormalization.Combine(parentFolder.Path, x.Name) : x.Path)).ToList();
		public async static Task<IList<StorageFolderWithPath>?> GetFoldersWithPathAsync
			(this StorageFolderWithPath parentFolder, string nameFilter, uint maxNumberOfItems = uint.MaxValue)
		{
			if (parentFolder is null)
				return null;

			var queryOptions = new QueryOptions
			{
				ApplicationSearchFilter = $"System.FileName:{nameFilter}*"
			};
			var queryResult = parentFolder.Item.CreateFolderQueryWithOptions(queryOptions);

			return (await queryResult.GetFoldersAsync(0, maxNumberOfItems))
				.Select(x => new StorageFolderWithPath(x, string.IsNullOrEmpty(x.Path) ? PathNormalization.Combine(parentFolder.Path, x.Name) : x.Path)).ToList();
		}

		private static PathBoxItem GetPathItem(string component, string path)
		{
			if (component.StartsWith(CommonPaths.RecycleBinPath, StringComparison.Ordinal))
			{
				// Handle the recycle bin: use the localized folder name
				return new PathBoxItem()
				{
					Title = ApplicationData.Current.LocalSettings.Values.Get("RecycleBin_Title", "Recycle Bin"),
					Path = path,
				};
			}
			else if (component.Contains(':', StringComparison.Ordinal))
			{
				var drives = App.DrivesManager.Drives.Concat(App.NetworkDrivesManager.Drives).Concat(App.CloudDrivesManager.Drives);
				var drive = drives.FirstOrDefault(y => y.ItemType is NavigationControlItemType.Drive && y.Path.Contains(component, StringComparison.OrdinalIgnoreCase));
				return new PathBoxItem()
				{
					Title = drive is not null ? drive.Text : $@"Drive ({component})",
					Path = path,
				};
			}
			else
			{
				if (path.EndsWith('\\') || path.EndsWith('/'))
					path = path.Remove(path.Length - 1);

				return new PathBoxItem
				{
					Title = component,
					Path = path
				};
			}
		}

		private static (List<string>, List<string>) GetComponentsAndRelativePaths(string value)
		{
			List<string> components = new();
			List<string> paths = new();

			for (int i = 0, lastIndex = 0; i < value.Length; i++)
			{
				if (value[i] == '?' || value[i] == Path.DirectorySeparatorChar || value[i] == Path.AltDirectorySeparatorChar)
				{
					if (lastIndex == i)
					{
						++lastIndex;
						continue;
					}

					var component = value.Substring(lastIndex, i - lastIndex);
					var path = value.Substring(0, i + 1);

					if (component == "..")
					{
						var lastListIndex = components.Count - 1;
						components.RemoveAt(lastListIndex);
						paths.RemoveAt(lastListIndex);
					}
					else
					{
						components.Add(component);
						paths.Add(path);
					}

					lastIndex = i + 1;
				}
			}

			return (components, paths);
		}

		private static string NormalizePath(string path)
		{
			if (path.Contains('/', StringComparison.Ordinal))
			{
				if (!path.EndsWith('/'))
					path += "/";
			}
			else if (!path.EndsWith('\\'))
			{
				path += "\\";
			}

			return path;
		}

		private static string ResolvePath(string path)
		{
			var trailingSeparator = path.EndsWith('\\') ? "\\" : path.EndsWith('/') ? "/" : "";
			var (components, _) = GetComponentsAndRelativePaths(path);

			return string.Join(Path.DirectorySeparatorChar, components) + trailingSeparator;
		}

		private static string GetPathWithoutEnvironmentVariable(string path)
		{
			if (path.StartsWith("~\\", StringComparison.Ordinal))
				path = $"{CommonPaths.HomePath}{path.Remove(0, 1)}";

			path = path.Replace("%temp%", CommonPaths.TempPath, StringComparison.OrdinalIgnoreCase);

			path = path.Replace("%tmp%", CommonPaths.TempPath, StringComparison.OrdinalIgnoreCase);

			path = path.Replace("%localappdata%", CommonPaths.LocalAppDataPath, StringComparison.OrdinalIgnoreCase);

			path = path.Replace("%homepath%", CommonPaths.HomePath, StringComparison.OrdinalIgnoreCase);

			return Environment.ExpandEnvironmentVariables(path);
		}
	}
}