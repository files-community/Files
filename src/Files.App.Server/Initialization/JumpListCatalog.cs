// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.ApplicationModel;
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
		await ModifyAsync(list => Add(list, path, name ?? path, RecentGroup));
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

	private static async Task ModifyAsync(Action<JumpList> action, bool save = true)
	{
		await gate.WaitAsync();
		try
		{
			PInvoke.SetCurrentProcessExplicitAppUserModelID($"{Package.Current.Id.FamilyName}!App").ThrowOnFailure();
			if (!JumpList.IsSupported())
				return;
			using var lease = await Task.Run(() => SettingsStore.AcquireWriteLease(Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "jump-list")));
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
