// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Controls;
using Files.App.ViewModels.Properties;
using Files.Shared.Helpers;
using Microsoft.UI.Xaml;
using Windows.Storage;

namespace Files.App.Data.Factories
{
	public static class PropertiesNavigationItemsFactory
	{
		public static ObservableCollection<PropertiesNavigationItem> Initialize(object item)
		{
			ObservableCollection<PropertiesNavigationItem> propertiesNavigationItems = [];

			var generalItem = CreateNavigationItem(PropertiesNavigationViewItemType.General, Strings.General.GetLocalizedResource(), "App.ThemedIcons.Properties.General");
			var securityItem = CreateNavigationItem(PropertiesNavigationViewItemType.Security, Strings.Security.GetLocalizedResource(), "App.ThemedIcons.Properties.Security");
			var hashesItem = CreateNavigationItem(PropertiesNavigationViewItemType.Hashes, Strings.Hashes.GetLocalizedResource(), "App.ThemedIcons.Properties.Hashes");
			var shortcutItem = CreateNavigationItem(PropertiesNavigationViewItemType.Shortcut, Strings.Shortcut.GetLocalizedResource(), "App.ThemedIcons.Properties.Shortcut");
			var libraryItem = CreateNavigationItem(PropertiesNavigationViewItemType.Library, Strings.Library.GetLocalizedResource(), "App.ThemedIcons.Properties.Library");
			var detailsItem = CreateNavigationItem(PropertiesNavigationViewItemType.Details, Strings.Details.GetLocalizedResource(), "App.ThemedIcons.Properties.Info");
			var customizationItem = CreateNavigationItem(PropertiesNavigationViewItemType.Customization, Strings.Customization.GetLocalizedResource(), "App.ThemedIcons.Properties.CustomizeFolder");
			var compatibilityItem = CreateNavigationItem(PropertiesNavigationViewItemType.Compatibility, Strings.Compatibility.GetLocalizedResource(), "App.ThemedIcons.Properties.Compatability");
			var signaturesItem = CreateNavigationItem(PropertiesNavigationViewItemType.Signatures, Strings.Signatures.GetLocalizedResource(), "App.ThemedIcons.Properties.Signatures");

			propertiesNavigationItems.Add(generalItem);
			propertiesNavigationItems.Add(signaturesItem);
			propertiesNavigationItems.Add(securityItem);
			propertiesNavigationItems.Add(hashesItem);
			propertiesNavigationItems.Add(shortcutItem);
			propertiesNavigationItems.Add(libraryItem);
			propertiesNavigationItems.Add(detailsItem);
			propertiesNavigationItems.Add(customizationItem);
			propertiesNavigationItems.Add(compatibilityItem);

			if (item is List<ListedItem> listedItems)
			{
				var firstFileExtension = listedItems.FirstOrDefault()?.FileExtension;
				var commonFileExt = listedItems.All(x => x.FileExtension == firstFileExtension) ? firstFileExtension : null;
				var compatibilityItemEnabled = listedItems.All(listedItem => FileExtensionHelpers.IsExecutableFile(listedItem is IShortcutItem sht ? sht.TargetPath : commonFileExt, true));
				var onlyFiles = listedItems.All(listedItem => listedItem.PrimaryItemAttribute == StorageItemTypes.File || listedItem.IsArchive);

				if (!compatibilityItemEnabled)
					propertiesNavigationItems.Remove(compatibilityItem);

				if (!onlyFiles)
					propertiesNavigationItems.Remove(detailsItem);

				propertiesNavigationItems.Remove(libraryItem);
				propertiesNavigationItems.Remove(shortcutItem);
				propertiesNavigationItems.Remove(securityItem);
				propertiesNavigationItems.Remove(customizationItem);
				propertiesNavigationItems.Remove(hashesItem);
				propertiesNavigationItems.Remove(signaturesItem);
			}
			else if (item is ListedItem listedItem)
			{
				var isShortcut = listedItem.IsShortcut;
				var isLibrary = listedItem.IsLibrary;
				var fileExt = listedItem.FileExtension;
				var isFolder = listedItem.PrimaryItemAttribute == Windows.Storage.StorageItemTypes.Folder;

				var securityItemEnabled = !isLibrary && !listedItem.IsRecycleBinItem;
				var hashItemEnabled = !(isFolder && !listedItem.IsArchive) && !isLibrary && !listedItem.IsRecycleBinItem;
				var detailsItemEnabled = !(isFolder && !listedItem.IsArchive) && !isLibrary && !listedItem.IsRecycleBinItem;
				var customizationItemEnabled = !isLibrary && (isFolder && !listedItem.IsArchive || isShortcut);
				var compatibilityItemEnabled = FileExtensionHelpers.IsExecutableFile(listedItem is IShortcutItem sht ? sht.TargetPath : fileExt, true);
				var signaturesItemEnabled =
					!isFolder &&
					!isLibrary &&
					!listedItem.IsRecycleBinItem &&
					FileExtensionHelpers.IsSignableFile(fileExt, true);

				if (!securityItemEnabled)
					propertiesNavigationItems.Remove(securityItem);

				if (!hashItemEnabled)
					propertiesNavigationItems.Remove(hashesItem);

				if (!signaturesItemEnabled)
					propertiesNavigationItems.Remove(signaturesItem);

				if (!isShortcut)
					propertiesNavigationItems.Remove(shortcutItem);

				if (!isLibrary)
					propertiesNavigationItems.Remove(libraryItem);

				if (!detailsItemEnabled)
					propertiesNavigationItems.Remove(detailsItem);

				if (!customizationItemEnabled)
					propertiesNavigationItems.Remove(customizationItem);

				if (!compatibilityItemEnabled)
					propertiesNavigationItems.Remove(compatibilityItem);
			}
			else if (item is DriveItem)
			{
				propertiesNavigationItems.Remove(hashesItem);
				propertiesNavigationItems.Remove(shortcutItem);
				propertiesNavigationItems.Remove(libraryItem);
				propertiesNavigationItems.Remove(detailsItem);
				propertiesNavigationItems.Remove(customizationItem);
				propertiesNavigationItems.Remove(compatibilityItem);
				propertiesNavigationItems.Remove(signaturesItem);
			}

			return propertiesNavigationItems;
		}

		private static PropertiesNavigationItem CreateNavigationItem(PropertiesNavigationViewItemType itemType, string text, string iconStyleKey)
		{
			var iconStyle = (Style)Application.Current.Resources[iconStyleKey];
			var iconElement = new ThemedIcon()
			{
				Width = 16,
				Height = 16,
				IconType = ThemedIconTypes.Outline,
				Style = iconStyle,
			};

			return new PropertiesNavigationItem(itemType, text, iconElement);
		}
	}
}
