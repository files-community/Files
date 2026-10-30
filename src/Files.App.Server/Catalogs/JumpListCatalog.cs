// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources.Core;
using Windows.Globalization;
using Windows.UI.StartScreen;
using Windows.Win32;

namespace Files.App.Server;

internal static class JumpListCatalog
{
	private const string PinnedGroup = "ms-resource:///Resources/JumpListPinnedGroupHeader";
	private const string RecentGroup = "ms-resource:///Resources/JumpListRecentGroupHeader";
	private static readonly SemaphoreSlim gate = new(1);
	private static readonly CachedTask<bool> pinned = new(RefreshCoreAsync, TimeSpan.FromMinutes(5));

	internal static void Invalidate() => pinned.Invalidate();
	internal static Task RefreshAsync() => pinned.GetAsync();

	private static async Task<bool> RefreshCoreAsync()
	{
		var folders = await InitializationCache.PinnedFolders.GetAsync();
		await ModifyAsync(list =>
		{
			foreach (var item in list.Items.Where(item => item.GroupName == PinnedGroup).ToList())
				list.Items.Remove(item);
			foreach (var folder in folders.Where(folder => folder.IsPinned))
				Add(list, folder.Path, folder.Name, PinnedGroup);
		});
		return true;
	}

	internal static async Task AddAsync(string path)
	{
		var name = await STATask.Run(() =>
		{
			using var item = ShellPath.Open(path);
			return item.Name ?? Path.GetFileName(path.TrimEnd('\\'));
		}, Program.Logger);
		var fallback = Path.GetFileName(path.TrimEnd('\\', '/'));
		await ModifyAsync(list => Add(list, path, name ?? (string.IsNullOrEmpty(fallback) ? path : fallback), RecentGroup));
	}

	internal static Task RemoveAsync(string path) => ModifyAsync(list =>
	{
		foreach (var item in list.Items.Where(item => item.Arguments == path || item.Arguments == path + '?').ToList())
			list.Items.Remove(item);
	});

	internal static async Task<IReadOnlyList<string>> GetAsync()
	{
		IReadOnlyList<string> result = Array.Empty<string>();
		await ModifyAsync(list => result = list.Items.Select(item => item.Arguments).ToArray(), save: false);
		return result;
	}

	private static void Add(JumpList list, string path, string name, string group)
	{
		var resource = GetNameResource(path);
		if (resource is not null)
		{
			if (resource is "Desktop" or "Downloads")
				name = $"ms-resource:///Resources/{resource}";
			else
			{
				var context = new ResourceContext();
				var language = ApplicationLanguages.PrimaryLanguageOverride;
				if (!string.IsNullOrEmpty(language))
					context.Languages = new[] { language };
				name = ResourceManager.Current.MainResourceMap.GetValue($"Resources/{resource}", context).ValueAsString;
			}
		}
		if (path.EndsWith('\\'))
			path += '?';
		if (Path.GetExtension(path).Equals(".library-ms", StringComparison.OrdinalIgnoreCase))
		{
			var library = Path.GetFileNameWithoutExtension(path);
			if (library is "Documents" or "Pictures" or "Music" or "Videos")
				name = $"ms-resource:///Resources/{library}";
		}
		var item = Windows.UI.StartScreen.JumpListItem.CreateWithArguments(path, name);
		item.Description = path;
		item.GroupName = group;
		item.Logo = new Uri("ms-appx:///Assets/FolderIcon.png");
		if (group == RecentGroup)
		{
			foreach (var duplicate in list.Items.Where(old => old.Arguments.Equals(path, StringComparison.OrdinalIgnoreCase)).ToList())
				list.Items.Remove(duplicate);
			list.Items.Insert(0, item);
			foreach (var excess in list.Items.Where(old => old.GroupName == RecentGroup).Skip(20).ToList())
				list.Items.Remove(excess);
		}
		else
			list.Items.Insert(list.Items.Count(old => old.GroupName == PinnedGroup), item);
	}

	private static string? GetNameResource(string path)
	{
		if (path.StartsWith(@"\\SHELL\", StringComparison.OrdinalIgnoreCase) || path.Equals("Shell:MyComputerFolder", StringComparison.OrdinalIgnoreCase))
			return "ThisPC";
		if (path.Equals("Shell:NetworkPlacesFolder", StringComparison.OrdinalIgnoreCase))
			return "Network";
		if (path.Equals("Shell:RecycleBinFolder", StringComparison.OrdinalIgnoreCase))
			return "RecycleBin";
		var userPaths = Windows.Storage.UserDataPaths.GetDefault();
		if (path.Equals(userPaths.Desktop, StringComparison.OrdinalIgnoreCase))
			return "Desktop";
		if (path.Equals(userPaths.Downloads, StringComparison.OrdinalIgnoreCase))
			return "Downloads";
		return null;
	}

	private static async Task ModifyAsync(Action<JumpList> action, bool save = true)
	{
		await gate.WaitAsync();
		try
		{
			PInvoke.SetCurrentProcessExplicitAppUserModelID($"{Package.Current.Id.FamilyName}!App").ThrowOnFailure();
			if (!JumpList.IsSupported())
				return;
			using var lease = await Task.Run(() => FileWriteLease.Acquire(Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "jump-list")));
			var list = await JumpList.LoadCurrentAsync();
			list.SystemGroupKind = JumpListSystemGroupKind.None;
			action(list);
			if (save)
				await list.SaveAsync();
		}
		finally
		{
			gate.Release();
		}
	}
}
