// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Backend.Helpers;
using Microsoft.UI.Xaml;

namespace Files.App.Data.Factories
{
	public static class PropertiesNavigationViewItemFactory
	{
		public static ObservableCollection<NavigationViewItemButtonStyleItem> Initialize(object item)
		{
			ObservableCollection<NavigationViewItemButtonStyleItem> PropertiesNavigationViewItems = new();

			var generalItem = new NavigationViewItemButtonStyleItem()
			{
				Name = "General".GetLocalizedResource(),
				ItemType = PropertiesNavigationViewItemType.General,
				OpacityIconStyle = (Style)Application.Current.Resources["ColorIconGeneralProperties"],
			};
			var securityItem = new NavigationViewItemButtonStyleItem()
			{
				Name = "Security".GetLocalizedResource(),
				ItemType = PropertiesNavigationViewItemType.Security,
				OpacityIconStyle = (Style)Application.Current.Resources["ColorIconSecurityProperties"],
			};
			var hashesItem = new NavigationViewItemButtonStyleItem()
			{
				Name = "Hashes".GetLocalizedResource(),
				ItemType = PropertiesNavigationViewItemType.Hashes,
				OpacityIconStyle = (Style)Application.Current.Resources["ColorIconHashesProperties"],
			};
			var shortcutItem = new NavigationViewItemButtonStyleItem()
			{
				Name = "Shortcut".GetLocalizedResource(),
				ItemType = PropertiesNavigationViewItemType.Shortcut,
				OpacityIconStyle = (Style)Application.Current.Resources["ColorIconShortcutProperties"],
			};
			var libraryItem = new NavigationViewItemButtonStyleItem()
			{
				Name = "Library".GetLocalizedResource(),
				ItemType = PropertiesNavigationViewItemType.Library,
				OpacityIconStyle = (Style)Application.Current.Resources["ColorIconLibraryProperties"],
			};
			var detailsItem = new NavigationViewItemButtonStyleItem()
			{
				Name = "Details".GetLocalizedResource(),
				ItemType = PropertiesNavigationViewItemType.Details,
				OpacityIconStyle = (Style)Application.Current.Resources["ColorIconDetailsProperties"],
			};
			var customizationItem = new NavigationViewItemButtonStyleItem()
			{
				Name = "Customization".GetLocalizedResource(),
				ItemType = PropertiesNavigationViewItemType.Customization,
				OpacityIconStyle = (Style)Application.Current.Resources["ColorIconCustomizationProperties"],
			};
			var compatibilityItem = new NavigationViewItemButtonStyleItem()
			{
				Name = "Compatibility".GetLocalizedResource(),
				ItemType = PropertiesNavigationViewItemType.Compatibility,
				OpacityIconStyle = (Style)Application.Current.Resources["ColorIconCompatibilityProperties"],
			};

			PropertiesNavigationViewItems.Add(generalItem);
			PropertiesNavigationViewItems.Add(securityItem);
			PropertiesNavigationViewItems.Add(hashesItem);
			PropertiesNavigationViewItems.Add(shortcutItem);
			PropertiesNavigationViewItems.Add(libraryItem);
			PropertiesNavigationViewItems.Add(detailsItem);
			PropertiesNavigationViewItems.Add(customizationItem);
			PropertiesNavigationViewItems.Add(compatibilityItem);

			if (item is List<ListedItem> listedItems)
			{
				var commonFileExt = listedItems.Select(x => x.FileExtension).Distinct().Count() == 1 ? listedItems.First().FileExtension : null;
				var compatibilityItemEnabled = listedItems.All(listedItem => FileExtensionHelpers.IsExecutableFile(listedItem is ShortcutItem sht ? sht.TargetPath : commonFileExt, true));

				if (!compatibilityItemEnabled)
					PropertiesNavigationViewItems.Remove(compatibilityItem);

				PropertiesNavigationViewItems.Remove(libraryItem);
				PropertiesNavigationViewItems.Remove(shortcutItem);
				PropertiesNavigationViewItems.Remove(detailsItem);
				PropertiesNavigationViewItems.Remove(securityItem);
				PropertiesNavigationViewItems.Remove(customizationItem);
				PropertiesNavigationViewItems.Remove(hashesItem);
			}
			else if (item is ListedItem listedItem)
			{
				var isShortcut = listedItem.IsShortcut;
				var isLibrary = listedItem.IsLibrary;
				var fileExt = listedItem.FileExtension;
				var isFolder = listedItem.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder;

				var securityItemEnabled = !isLibrary && !listedItem.IsRecycleBinItem;
				var hashItemEnabled = !(isFolder && !listedItem.IsArchive) && !isLibrary && !listedItem.IsRecycleBinItem;
				var detailsItemEnabled = fileExt is not null && !isShortcut && !isLibrary;
				var customizationItemEnabled = !isLibrary && (isFolder && !listedItem.IsArchive || isShortcut && !listedItem.IsLinkItem);
				var compatibilityItemEnabled = FileExtensionHelpers.IsExecutableFile(listedItem is ShortcutItem sht ? sht.TargetPath : fileExt, true);

				if (!securityItemEnabled)
					PropertiesNavigationViewItems.Remove(securityItem);

				if (!hashItemEnabled)
					PropertiesNavigationViewItems.Remove(hashesItem);

				if (!isShortcut)
					PropertiesNavigationViewItems.Remove(shortcutItem);

				if (!isLibrary)
					PropertiesNavigationViewItems.Remove(libraryItem);

				if (!detailsItemEnabled)
					PropertiesNavigationViewItems.Remove(detailsItem);

				if (!customizationItemEnabled)
					PropertiesNavigationViewItems.Remove(customizationItem);

				if (!compatibilityItemEnabled)
					PropertiesNavigationViewItems.Remove(compatibilityItem);
			}
			else if (item is DriveItem)
			{
				PropertiesNavigationViewItems.Remove(hashesItem);
				PropertiesNavigationViewItems.Remove(shortcutItem);
				PropertiesNavigationViewItems.Remove(libraryItem);
				PropertiesNavigationViewItems.Remove(detailsItem);
				PropertiesNavigationViewItems.Remove(customizationItem);
				PropertiesNavigationViewItems.Remove(compatibilityItem);
			}

			return PropertiesNavigationViewItems;
		}
	}
}
