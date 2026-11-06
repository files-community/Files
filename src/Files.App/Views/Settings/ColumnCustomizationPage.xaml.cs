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
	/// Lets the user choose the columns of <see cref="DetailsLayoutPage"/> from a grouped list.
	/// </summary>
	public sealed partial class ColumnCustomizationPage : Page
	{
		public ColumnCustomizationViewModel ViewModel { get; private set; } = null!;

		public FrameworkElement TitleBarElement => WindowTitleBar;

		public ColumnCustomizationPage()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			if (e.Parameter is ColumnCustomizationNavigationArguments { Window: var window, ViewModel: var viewModel })
			{
				ViewModel = viewModel;
				ViewModel.CloseRequested += (_, _) => window.Close();
				Bindings.Update();
			}
		}

		private void ColumnsList_ItemClick(object sender, ItemClickEventArgs e)
		{
			(e.ClickedItem as ColumnPickerItem)?.ActivateCommand.Execute(null);
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		[DynamicWindowsRuntimeCast(typeof(Control))]
		private void ColumnsList_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if ((e.OriginalSource as FrameworkElement)?.DataContext is not ColumnPickerItem item)
				return;

			switch (e.Key)
			{
				case VirtualKey.Right when item.IsGroup && !item.IsExpanded:
				case VirtualKey.Left when item.IsGroup && item.IsExpanded:
					ViewModel.ToggleExpanded(item);
					e.Handled = true;
					break;
				case VirtualKey.Left when item.IsColumn:
					(ColumnsList.ContainerFromItem(item.Group) as Control)?.Focus(FocusState.Keyboard);
					e.Handled = true;
					break;
			}
		}
	}
}
