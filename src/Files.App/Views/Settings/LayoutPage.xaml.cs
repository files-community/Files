// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT;

namespace Files.App.Views.Settings
{
	public sealed partial class LayoutPage : Page
	{
		public LayoutPage()
		{
			InitializeComponent();
			Loaded += (_, _) => ViewModel.StartListening();
			Unloaded += (_, _) => ViewModel.StopListening();
		}

		[DynamicWindowsRuntimeCast(typeof(MenuFlyout))]
		private async void AddColumnMenuFlyout_Opening(object sender, object e)
		{
			if (sender is not MenuFlyout menuFlyout)
				return;

			menuFlyout.Items.Clear();

			var builtInColumns = DetailsLayoutColumnIds.All
				.Where(id => id is not (DetailsLayoutColumnIds.Icon or DetailsLayoutColumnIds.Name) && !ViewModel.HasDefaultDetailsColumn(id))
				.ToArray();

			AddColumnGroup(menuFlyout.Items, Strings.General.GetLocalizedResource(), builtInColumns
				.Where(id => !ColumnValueHelper.IsGitColumn(id))
				.Select(id => (id, LayoutViewModel.GetColumnDisplayName(id, null))));

			AddColumnGroup(menuFlyout.Items, Strings.Git.GetLocalizedResource(), builtInColumns
				.Where(ColumnValueHelper.IsGitColumn)
				.Select(id => (id, LayoutViewModel.GetColumnDisplayName(id, null))));

			var catalog = await ShellPropertyCatalog.GetAsync();
			foreach (var (resourceKey, propertyNames) in ShellPropertyCatalog.MenuCategories)
			{
				AddColumnGroup(menuFlyout.Items, resourceKey.GetLocalizedResource(), propertyNames
					.Where(name => catalog.ContainsKey(name) && !ViewModel.HasDefaultDetailsColumn(name))
					.Select(name => (name, catalog[name].DisplayName)));
			}

			var moreColumns = new MenuFlyoutSubItem { Text = Strings.MoreColumns.GetLocalizedResource() };
			var catalogGroups = catalog.Values
				.Where(info => !DetailsLayoutColumnDefinitions.IsBuiltIn(info.Name) && !ViewModel.HasDefaultDetailsColumn(info.Name))
				.GroupBy(info => ShellPropertyCatalog.GetGroupDisplayName(info.Group))
				.OrderBy(group => group.Key, StringComparer.CurrentCultureIgnoreCase);

			foreach (var group in catalogGroups)
			{
				AddColumnGroup(moreColumns.Items, group.Key, group
					.OrderBy(info => info.DisplayName, StringComparer.CurrentCultureIgnoreCase)
					.Select(info => (info.Name, info.DisplayName)));
			}

			if (moreColumns.Items.Count > 0)
				menuFlyout.Items.Add(moreColumns);
		}

		private void AddColumnGroup(IList<MenuFlyoutItemBase> items, string groupName, IEnumerable<(string Id, string DisplayName)> columns)
		{
			var subItem = new MenuFlyoutSubItem { Text = groupName };
			foreach (var (id, displayName) in columns)
			{
				var item = new MenuFlyoutItem { Text = displayName };
				item.Click += (_, _) => ViewModel.AddDefaultDetailsColumn(id, displayName);
				subItem.Items.Add(item);
			}

			if (subItem.Items.Count > 0)
				items.Add(subItem);
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private void MoveColumnUp_Click(object sender, RoutedEventArgs e)
		{
			if ((sender as FrameworkElement)?.DataContext is DetailsLayoutColumnSettingItem column)
				ViewModel.MoveDefaultDetailsColumn(column, -1);
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private void MoveColumnDown_Click(object sender, RoutedEventArgs e)
		{
			if ((sender as FrameworkElement)?.DataContext is DetailsLayoutColumnSettingItem column)
				ViewModel.MoveDefaultDetailsColumn(column, 1);
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private void RemoveColumn_Click(object sender, RoutedEventArgs e)
		{
			if ((sender as FrameworkElement)?.DataContext is DetailsLayoutColumnSettingItem column)
				ViewModel.RemoveDefaultDetailsColumn(column);
		}
	}
}
