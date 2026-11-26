// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using WinRT;

namespace Files.App.Views.Settings
{
	/// <summary>
	/// Edits a layout template, opened from <see cref="LayoutPage"/>.
	/// </summary>
	public sealed partial class LayoutTemplatePage : Page
	{
		public LayoutTemplateEditorViewModel ViewModel { get; private set; } = null!;

		public LayoutTemplatePage()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			if (e.Parameter is LayoutTemplateEditorViewModel viewModel)
			{
				ViewModel = viewModel;
				ViewModel.Deleted += ViewModel_Deleted;
				ViewModel.ManageColumnsRequested += ViewModel_ManageColumnsRequested;
				ViewModel.ChoosePropertyRequested += ViewModel_ChoosePropertyRequested;
				Bindings.Update();
			}
		}

		[DynamicWindowsRuntimeCast(typeof(Style))]
		public static Style GetIconStyle(string key)
			=> (Style)Application.Current.Resources[key];

		[DynamicWindowsRuntimeCast(typeof(ToggleButton))]
		private void LayoutModeChoice_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not ToggleButton { Tag: LayoutModeChoice choice } button)
				return;

			ViewModel.SelectedLayoutModeIndex = (int)choice.LayoutMode;
			button.IsChecked = choice.IsSelected;
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			base.OnNavigatedFrom(e);
			if (ViewModel is null)
				return;

			ViewModel.Deleted -= ViewModel_Deleted;
			ViewModel.ManageColumnsRequested -= ViewModel_ManageColumnsRequested;
			ViewModel.ChoosePropertyRequested -= ViewModel_ChoosePropertyRequested;
			ViewModel.Save();
		}

		private void LayoutBreadcrumb_Click(object sender, RoutedEventArgs e)
		{
			GoBack();
		}

		private void GoBack()
		{
			if (Frame.CanGoBack)
				Frame.GoBack(new SuppressNavigationTransitionInfo());
		}

		private void ViewModel_Deleted(object? sender, EventArgs e)
		{
			GoBack();
		}

		private void ViewModel_ManageColumnsRequested(object? sender, ColumnCustomizationViewModel columns)
		{
			ColumnCustomizationWindow.Show(columns);
		}

		private void ViewModel_ChoosePropertyRequested(object? sender, LayoutPropertyPickerViewModel picker)
		{
			ColumnCustomizationWindow.Show(picker);
		}
	}
}
