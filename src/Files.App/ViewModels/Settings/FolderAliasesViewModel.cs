// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.ViewModels.Settings
{
	public sealed partial class FolderAliasesViewModel : ObservableObject
	{
		private IFoldersSettingsService FoldersSettingsService { get; } = Ioc.Default.GetRequiredService<IFoldersSettingsService>();

		public ObservableCollection<FolderAliasItem> Aliases { get; }

		public bool IsEmpty
			=> Aliases.Count is 0;

		public FolderAliasesViewModel()
		{
			Aliases = new(FoldersSettingsService.FolderAliases ?? []);
			Aliases.CollectionChanged += (s, e) => OnPropertyChanged(nameof(IsEmpty));
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

			Aliases.Clear();
			foreach (var alias in FoldersSettingsService.FolderAliases ?? [])
				Aliases.Add(alias);
		}

		private void Save()
		{
			FoldersSettingsService.FolderAliases = [.. Aliases];
		}
	}
}
