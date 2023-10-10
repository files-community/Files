// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls
{
	public sealed partial class StatusBar : UserControl
	{
		private readonly ICommandManager Commands = Ioc.Default.GetRequiredService<ICommandManager>();

		public static readonly DependencyProperty DirectoryPropertiesViewModelProperty =
			DependencyProperty.Register(
				nameof(DirectoryPropertiesViewModel),
				typeof(DirectoryPropertiesViewModel),
				typeof(StatusBar),
				new PropertyMetadata(null));

		public DirectoryPropertiesViewModel? DirectoryPropertiesViewModel
		{
			get => (DirectoryPropertiesViewModel)GetValue(DirectoryPropertiesViewModelProperty);
			set => SetValue(DirectoryPropertiesViewModelProperty, value);
		}

		public static readonly DependencyProperty SelectedItemsPropertiesViewModelProperty =
			DependencyProperty.Register(
				nameof(SelectedItemsPropertiesViewModel),
				typeof(SelectedItemsPropertiesViewModel),
				typeof(StatusBar),
				new PropertyMetadata(null));

		public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel
		{
			get => (SelectedItemsPropertiesViewModel)GetValue(SelectedItemsPropertiesViewModelProperty);
			set => SetValue(SelectedItemsPropertiesViewModelProperty, value);
		}

		public static readonly DependencyProperty ShowInfoTextProperty =
			DependencyProperty.Register(
				nameof(ShowInfoText),
				typeof(bool),
				typeof(StatusBar),
				new PropertyMetadata(null));

		public bool ShowInfoText
		{
			get => (bool)GetValue(ShowInfoTextProperty);
			set => SetValue(ShowInfoTextProperty, value);
		}

		public StatusBar()
		{
			InitializeComponent();
		}

		private void BranchesFlyout_Opening(object sender, object e)
		{
			if (DirectoryPropertiesViewModel is null)
				return;

			DirectoryPropertiesViewModel.ShowLocals = true;
			DirectoryPropertiesViewModel.SelectedBranchIndex = DirectoryPropertiesViewModel.ACTIVE_BRANCH_INDEX;
		}

		private void BranchesList_ItemClick(object sender, ItemClickEventArgs e)
		{
			BranchesFlyout.Hide();
		}
	}
}
