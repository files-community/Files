// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Utils.Terminal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls
{
	public sealed partial class TerminalPane : UserControl
	{
		public MainPageViewModel? MainPageViewModel
		{
			get => (MainPageViewModel)GetValue(MainPageViewModelProperty);
			set => SetValue(MainPageViewModelProperty, value);
		}

		public static readonly DependencyProperty MainPageViewModelProperty =
			DependencyProperty.Register(nameof(MainPageViewModel), typeof(MainPageViewModel), typeof(TerminalPane), new PropertyMetadata(null));

		private const double DefaultSidebarWidth = 200;
		private const double CollapsedSidebarWidth = 44;

		private bool _isSidebarExpanded = true;
		private double _expandedSidebarWidth = DefaultSidebarWidth;

		public TerminalPane()
		{
			InitializeComponent();
		}

		private void TerminalProfile_ItemClick(object sender, ItemClickEventArgs e)
		{
			MainPageViewModel?.TerminalAddCommand.Execute((ShellProfile)e.ClickedItem);
		}

		private void TerminalListItemClose_Click(object sender, RoutedEventArgs e)
		{
			MainPageViewModel?.TerminalCloseCommand.Execute(((Button)sender).Tag?.ToString());
		}

		private void TerminalSidebarToggle_Click(object sender, RoutedEventArgs e)
		{
			if (_isSidebarExpanded)
				CollapseSidebar();
			else
				ExpandSidebar();
		}

		private void CollapseSidebar()
		{
			_expandedSidebarWidth = SidebarColumn.ActualWidth > 0 ? SidebarColumn.ActualWidth : DefaultSidebarWidth;
			_isSidebarExpanded = false;
			SidebarColumn.MinWidth = 0;
			SidebarColumn.Width = new GridLength(CollapsedSidebarWidth);
			VisualStateManager.GoToState(this, "CollapsedState", true);
		}

		private void ExpandSidebar()
		{
			_isSidebarExpanded = true;
			SidebarColumn.MinWidth = 120;
			SidebarColumn.Width = new GridLength(_expandedSidebarWidth);
			VisualStateManager.GoToState(this, "ExpandedState", true);
		}
	}
}
