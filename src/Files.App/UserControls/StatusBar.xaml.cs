// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Data.Commands;
using Files.App.Utils.Terminal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls
{
	public sealed partial class StatusBar : UserControl
	{
		public ICommandManager Commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();

		public MainPageViewModel MainPageViewModel { get; } = Ioc.Default.GetRequiredService<MainPageViewModel>();

		public DirectoryPropertiesViewModel? DirectoryPropertiesViewModel
		{
			get => (DirectoryPropertiesViewModel)GetValue(DirectoryPropertiesViewModelProperty);
			set => SetValue(DirectoryPropertiesViewModelProperty, value);
		}

		// Using a DependencyProperty as the backing store for DirectoryPropertiesViewModel.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty DirectoryPropertiesViewModelProperty =
			DependencyProperty.Register(nameof(DirectoryPropertiesViewModel), typeof(DirectoryPropertiesViewModel), typeof(StatusBar), new PropertyMetadata(null));

		public SelectedItemsPropertiesViewModel? SelectedItemsPropertiesViewModel
		{
			get => (SelectedItemsPropertiesViewModel)GetValue(SelectedItemsPropertiesViewModelProperty);
			set => SetValue(SelectedItemsPropertiesViewModelProperty, value);
		}

		public static readonly DependencyProperty SelectedItemsPropertiesViewModelProperty =
			DependencyProperty.Register(nameof(SelectedItemsPropertiesViewModel), typeof(SelectedItemsPropertiesViewModel), typeof(StatusBar), new PropertyMetadata(null));

		public bool ShowInfoText
		{
			get => (bool)GetValue(ShowInfoTextProperty);
			set => SetValue(ShowInfoTextProperty, value);
		}

		// Using a DependencyProperty as the backing store for HideInfoText.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ShowInfoTextProperty =
			DependencyProperty.Register(nameof(ShowInfoText), typeof(bool), typeof(StatusBar), new PropertyMetadata(null));

		public StatusBar()
		{
			InitializeComponent();
		}

		private async void BranchesFlyout_Opening(object _, object e)
		{
			if (DirectoryPropertiesViewModel is null)
				return;

			DirectoryPropertiesViewModel.IsBranchesFlyoutExpaned = true;
			DirectoryPropertiesViewModel.ShowLocals = true;
			await DirectoryPropertiesViewModel.LoadBranches();
			DirectoryPropertiesViewModel.SelectedBranchIndex = DirectoryPropertiesViewModel.ACTIVE_BRANCH_INDEX;
		}

		private void BranchesList_ItemClick(object sender, ItemClickEventArgs e)
		{
			BranchesFlyout.Hide();
		}

		private void BranchesFlyout_Closing(object _, object e)
		{
			if (DirectoryPropertiesViewModel is null)
				return;

			DirectoryPropertiesViewModel.IsBranchesFlyoutExpaned = false;
		}

		private async void DeleteBranch_Click(object sender, RoutedEventArgs e)
		{
			if (DirectoryPropertiesViewModel is null)
				return;

			BranchesFlyout.Hide();
			await DirectoryPropertiesViewModel.ExecuteDeleteBranch(((BranchItem)((Button)sender).DataContext).Name);
		}

		private void TerminalCloseButton_Click(object sender, RoutedEventArgs e)
		{
			MainPageViewModel.TerminalCloseCommand.Execute(((Button)sender).Tag.ToString());
		}

		private void ShellProfileList_ItemClick(object sender, ItemClickEventArgs e)
		{
			MainPageViewModel.TerminalAddCommand.Execute((ShellProfile)e.ClickedItem);
		}
	}
}
