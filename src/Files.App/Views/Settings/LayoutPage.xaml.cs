// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Views.Settings
{
	public sealed partial class LayoutPage : Page
	{
		private bool _columnOptionsAdded;

		public LayoutPage()
		{
			InitializeComponent();
			Loaded += LayoutPage_Loaded;
			Unloaded += (_, _) => ViewModel.StopListening();
		}

		private async void LayoutPage_Loaded(object sender, RoutedEventArgs e)
		{
			await ViewModel.LoadColumnOptionsAsync();
			if (!IsLoaded)
				return;

			AddColumnOptions();
			ViewModel.StartListening();
		}

		private void AddColumnOptions()
		{
			if (_columnOptionsAdded)
				return;

			_columnOptionsAdded = true;

			foreach (var column in ViewModel.SortColumnOptions)
				DefaultSortComboBox.Items.Add(new ComboBoxItem { Content = ViewModel.GetColumnLabel(column.Id) });

			foreach (var column in ViewModel.GroupColumnOptions)
				DefaultGroupComboBox.Items.Add(new ComboBoxItem { Content = ViewModel.GetColumnLabel(column.Id) });

			foreach (var column in ViewModel.ColumnOptions)
			{
				var label = ViewModel.GetColumnLabel(column.Id);
				var toggle = new ToggleSwitch { IsOn = ViewModel.IsDefaultColumn(column.Id) };
				AutomationProperties.SetName(toggle, label);
				toggle.Toggled += (_, _) => ViewModel.SetDefaultColumn(column.Id, toggle.IsOn);
				ColumnsExpander.Items.Add(new SettingsCard { Header = label, Content = toggle });
			}
		}
	}
}
