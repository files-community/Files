// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.ViewModels.Settings
{
	public sealed partial class FolderAliasesViewModel : ObservableObject
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private IFoldersSettingsService FoldersSettingsService { get; } = Ioc.Default.GetRequiredService<IFoldersSettingsService>();

		public ObservableCollection<FolderAliasItem> Aliases { get; }

		public bool IsEmpty
			=> Aliases.Count is 0;

		public FolderAliasesViewModel()
		{
			Aliases = new(FoldersSettingsService.FolderAliases ?? []);
			Aliases.CollectionChanged += (s, e) => OnPropertyChanged(nameof(IsEmpty));
		}

		public void StartWatching()
		{
			UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;
			Reload();
		}

		public void StopWatching()
		{
			UserSettingsService.OnSettingChangedEvent -= UserSettingsService_OnSettingChangedEvent;
		}

		public bool IsNameInUse(string name, FolderAliasItem? ignoredItem)
			=> Aliases.Any(x => x != ignoredItem && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

		public void Add(FolderAliasItem item)
		{
			Aliases.Add(item);
			Save();
		}

		public void Replace(FolderAliasItem oldItem, FolderAliasItem newItem)
		{
			var index = Aliases.IndexOf(oldItem);
			if (index < 0)
				return;

			Aliases[index] = newItem;
			Save();
		}

		public void Remove(FolderAliasItem item)
		{
			if (Aliases.Remove(item))
				Save();
		}

		public void RestoreDefaults()
		{
			FoldersSettingsService.FolderAliases = null;
			Reload();
		}

		private void Save()
		{
			FoldersSettingsService.FolderAliases = [.. Aliases];
		}

		private void Reload()
		{
			var aliases = FoldersSettingsService.FolderAliases ?? [];
			if (aliases.Select(x => (x.Name, x.Path)).SequenceEqual(Aliases.Select(x => (x.Name, x.Path))))
				return;

			Aliases.Clear();
			foreach (var alias in aliases)
				Aliases.Add(alias);
		}

		private void UserSettingsService_OnSettingChangedEvent(object? sender, SettingChangedEventArgs e)
		{
			if (e.SettingName is nameof(IFoldersSettingsService.FolderAliases))
				Reload();
		}
	}
}
