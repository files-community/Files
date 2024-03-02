// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls
{
	/// <summary>
	/// Displays status bar content shown on the bottom of the shell page within the main app window.
	/// </summary>
	public sealed partial class StatusBar : UserControl
	{
		// Dependency injections

		public ICommandManager Commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();

		// Properties

		public StatusBarViewModel? ViewModel
		{
			get => (StatusBarViewModel)GetValue(DirectoryPropertiesViewModelProperty);
			set => SetValue(DirectoryPropertiesViewModelProperty, value);
		}

		public static readonly DependencyProperty DirectoryPropertiesViewModelProperty =
			DependencyProperty.Register(
				nameof(ViewModel),
				typeof(StatusBarViewModel),
				typeof(StatusBar),
				new PropertyMetadata(null));

		public SelectedItemsPropertiesViewModel? SelectedItemsPropertiesViewModel
		{
			get => (SelectedItemsPropertiesViewModel)GetValue(SelectedItemsPropertiesViewModelProperty);
			set => SetValue(SelectedItemsPropertiesViewModelProperty, value);
		}

		public static readonly DependencyProperty SelectedItemsPropertiesViewModelProperty =
			DependencyProperty.Register(
				nameof(SelectedItemsPropertiesViewModel),
				typeof(SelectedItemsPropertiesViewModel),
				typeof(StatusBar),
				new PropertyMetadata(null));

		public bool ShowInfoText
		{
			get => (bool)GetValue(ShowInfoTextProperty);
			set => SetValue(ShowInfoTextProperty, value);
		}

		public static readonly DependencyProperty ShowInfoTextProperty =
			DependencyProperty.Register(
				nameof(ShowInfoText),
				typeof(bool),
				typeof(StatusBar),
				new PropertyMetadata(null));

		// Constructor

		public StatusBar()
		{
			InitializeComponent();
		}

		// Event Methods

		private async void BranchesFlyout_Opening(object _, object e)
		{
			if (ViewModel is null)
				return;

			ViewModel.IsBranchesFlyoutExpanded = true;
			ViewModel.ShowLocals = true;
			await ViewModel.LoadBranches();
			ViewModel.SelectedBranchIndex = StatusBarViewModel.ACTIVE_BRANCH_INDEX;
		}

		private void BranchesList_ItemClick(object sender, ItemClickEventArgs e)
		{
			BranchesFlyout.Hide();
		}

		private void BranchesFlyout_Closing(object _, object e)
		{
			if (ViewModel is null)
				return;

			ViewModel.IsBranchesFlyoutExpanded = false;
		}

		private async void DeleteBranch_Click(object sender, RoutedEventArgs e)
		{
			if (ViewModel is null)
				return;

			BranchesFlyout.Hide();
			await ViewModel.ExecuteDeleteBranch(((BranchItem)((Button)sender).DataContext).Name);
		}
	}
}
