// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.Controls;
using Files.App.Data.Enums;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;

namespace Files.App.ViewModels.Settings
{
	public sealed partial class SettingsPageViewModel : ObservableObject
	{
		public ObservableCollection<SettingsNavigationItem> NavigationItems { get; } = [];

		[ObservableProperty]
		private SettingsPageKind _selectedPage = SettingsPageKind.GeneralPage;

		public SettingsPageViewModel()
		{
			NavigationItems.Add(CreateNavigationItem(SettingsPageKind.GeneralPage, "SettingsItemGeneral", Strings.General.GetLocalizedResource(), "App.ThemedIcons.Settings.General"));
			NavigationItems.Add(CreateNavigationItem(SettingsPageKind.AppearancePage, "SettingsItemAppearance", Strings.Appearance.GetLocalizedResource(), "App.ThemedIcons.Settings.Appearance"));
			NavigationItems.Add(CreateNavigationItem(SettingsPageKind.LayoutPage, "SettingsItemLayout", Strings.Layout.GetLocalizedResource(), "App.ThemedIcons.Settings.Layout"));
			NavigationItems.Add(CreateNavigationItem(SettingsPageKind.FoldersPage, "SettingsItemFolders", Strings.FilesAndFolders.GetLocalizedResource(), "App.ThemedIcons.Settings.FilesFolders"));
			NavigationItems.Add(CreateNavigationItem(SettingsPageKind.ActionsPage, "SettingsItemActions", Strings.Actions.GetLocalizedResource(), "App.ThemedIcons.Settings.KeyboardActions"));
			NavigationItems.Add(CreateNavigationItem(SettingsPageKind.TagsPage, "SettingsItemTags", Strings.FileTags.GetLocalizedResource(), "App.ThemedIcons.Settings.Tags"));
			NavigationItems.Add(CreateNavigationItem(SettingsPageKind.DevToolsPage, "SettingsItemDevTools", Strings.DevTools.GetLocalizedResource(), "App.ThemedIcons.Settings.DevTools"));
			NavigationItems.Add(CreateNavigationItem(SettingsPageKind.AdvancedPage, "SettingsItemAdvanced", Strings.Advanced.GetLocalizedResource(), "App.ThemedIcons.Settings.Advanced"));
			NavigationItems.Add(CreateNavigationItem(SettingsPageKind.AboutPage, "SettingsItemAbout", Strings.About.GetLocalizedResource(), "App.ThemedIcons.Info"));

			SetSelectedPage(SettingsPageKind.GeneralPage);
		}

		public void SetSelectedPage(SettingsPageKind pageKind)
		{
			SelectedPage = pageKind;

			foreach (var item in NavigationItems)
			{
				var isSelected = item.PageKind == pageKind;
				item.IconElement.IsFilled = isSelected;
				item.IconElement.IconType = ThemedIconTypes.Outline;
			}
		}

		private static SettingsNavigationItem CreateNavigationItem(SettingsPageKind pageKind, string automationId, string text, string iconStyleKey)
		{
			var iconStyle = (Style)Application.Current.Resources[iconStyleKey];
			var iconElement = new ThemedIcon()
			{
				Width = 16,
				Height = 16,
				IconType = ThemedIconTypes.Outline,
				Style = iconStyle,
			};

			return new SettingsNavigationItem(pageKind, automationId, text, iconElement);
		}
	}

	public sealed partial class SettingsNavigationItem : ObservableObject, ISidebarItemModel
	{
		public SettingsPageKind PageKind { get; }
		public string AutomationId { get; }
		public string Text { get; }
		public ThemedIcon IconElement { get; }

		// ISidebarItemModel
		public object? Children => null;
		public bool PaddedItem => false;
		[ObservableProperty] private bool _isExpanded;

		// DefaultSidebarItemTemplate bindings
		public object? ToolTip => Text;
		public object? ItemDecorator => null;

		public SettingsNavigationItem(SettingsPageKind pageKind, string automationId, string text, ThemedIcon iconElement)
		{
			PageKind = pageKind;
			AutomationId = automationId;
			Text = text;
			IconElement = iconElement;
		}
	}
}
