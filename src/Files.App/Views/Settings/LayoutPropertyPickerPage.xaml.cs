// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;
using WinRT;

namespace Files.App.Views.Settings
{
	/// <summary>
	/// Lets the user choose what items are sorted or grouped by from a grouped list.
	/// </summary>
	public sealed partial class LayoutPropertyPickerPage : Page
	{
		public LayoutPropertyPickerViewModel ViewModel { get; private set; } = null!;

		public LayoutPropertyPickerPage()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			if (e.Parameter is LayoutPropertyPickerNavigationArguments { Window: var window, ViewModel: var viewModel })
			{
				ViewModel = viewModel;
				ViewModel.CloseRequested += (_, _) => window.Close();
				window.SetTitleBar(WindowTitleBar);
				Bindings.Update();
			}
		}

		private void ChoicesList_Loaded(object sender, RoutedEventArgs e)
		{
			if (ViewModel.SelectedItem is { } item)
				ChoicesList.ScrollIntoView(item);
		}

		private void ChoicesList_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (e.ClickedItem is LayoutPropertyPickerItem item)
				ViewModel.Activate(item);
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		[DynamicWindowsRuntimeCast(typeof(Control))]
		private void ChoicesList_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if ((e.OriginalSource as FrameworkElement)?.DataContext is not LayoutPropertyPickerItem item)
				return;

			switch (e.Key)
			{
				case VirtualKey.Right when item.IsGroup && !item.IsExpanded:
				case VirtualKey.Left when item.IsGroup && item.IsExpanded:
					ViewModel.ToggleExpanded(item);
					e.Handled = true;
					break;
				case VirtualKey.Left when item is { IsChoice: true, Group: not null }:
					(ChoicesList.ContainerFromItem(item.Group) as Control)?.Focus(FocusState.Keyboard);
					e.Handled = true;
					break;
			}
		}
	}
}
