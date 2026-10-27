// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

		private async void CustomizeColumns_Click(object sender, RoutedEventArgs e)
		{
			var catalog = await ShellPropertyCatalog.GetAsync();
			var availableColumns = DetailsLayoutColumnIds.All
				.Where(id => id != DetailsLayoutColumnIds.Icon)
				.Concat(catalog.Keys.Where(id => !DetailsLayoutColumnDefinitions.IsBuiltIn(id)));

			var builtInDefaults = DetailsLayoutColumnDefinitions.All.Where(definition => definition.IsVisibleByDefault).Select(definition => definition.Id);
			var viewModel = new ColumnCustomizationViewModel(
				DetailsLayoutColumnDefaults.GetColumns(),
				availableColumns,
				builtInDefaults,
				id => DetailsLayoutColumnDefaults.GetDisplayName(id, catalog),
				Strings.CustomizeDefaultColumns.GetLocalizedResource(),
				Strings.CustomizeDefaultColumnsDescription.GetLocalizedResource());

			viewModel.Saved += (_, columns) => DetailsLayoutColumnDefaults.SetColumns(columns);
			ColumnCustomizationWindow.Show(viewModel);
		}
	}
}
