// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.UserControls.Widgets
{
	public sealed partial class RecentFilesWidget : UserControl
	{
		private RecentFilesWidgetViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<RecentFilesWidgetViewModel>();

		public RecentFilesWidget()
		{
			InitializeComponent();
		}

		private void RecentFilesListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			ViewModel.Button_RightTapped(sender, e);
		}

		private void RecentFilesListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			ViewModel.RecentFilesListView_ItemClick(sender, e);
		}
	}
}
