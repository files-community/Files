// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;

namespace Files.Backend.ViewModels.Dialogs
{
	public sealed class SettingsDialogViewModel : ObservableObject
	{
		public ObservableCollection<SettingsNavItem> SettingsNavItems { get; }

		public SettingsDialogViewModel()
		{
			SettingsNavItems = new ObservableCollection<SettingsNavItem>
			{
				new SettingsNavItem
				{
					Name = "General",
					AutomationId = "SettingsItemGeneral",
					Tag = "0",
					IsSelected = true,
				},
				new SettingsNavItem
				{
					Name = "Appearance",
					AutomationId = "SettingsItemAppearance",
					Tag = "1",
				},
				new SettingsNavItem
				{
					Name = "Folders",
					AutomationId = "SettingsItemFolders",
					Tag = "2",
				},
				new SettingsNavItem
				{
					Name = "Tags",
					AutomationId = "SettingsItemTags",
					Tag = "3",
				},
				new SettingsNavItem
				{
					Name = "Advanced",
					AutomationId = "SettingsItemAdvanced",
					Tag = "4",
				},
				new SettingsNavItem
				{
					Name = "About",
					AutomationId = "SettingsItemAbout",
					Tag = "5",
				},
			};

			SelectedItem = SettingsNavItems.First();
		}

		private SettingsNavItem selectedItem;
		public SettingsNavItem SelectedItem
		{
			get => selectedItem;
			set => SetProperty(ref selectedItem, value);
		}
	}

	public class SettingsNavItem : ObservableObject
	{
		public string? Name;

		public string? AutomationId;

		public string? Tag;

		public bool IsSelected;
	}
}
