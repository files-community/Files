// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;

namespace Files.App.Controls
{
	public sealed partial class SidebarView
	{
		[GeneratedDependencyProperty(DefaultValue = SidebarDisplayMode.Expanded)]
		public partial SidebarDisplayMode DisplayMode { get; set; }

		[GeneratedDependencyProperty]
		public partial UIElement? InnerContent { get; set; }

		[GeneratedDependencyProperty]
		public partial UIElement? SidebarContent { get; set; }

		[GeneratedDependencyProperty]
		public partial UIElement? Footer { get; set; }

		[GeneratedDependencyProperty]
		public partial bool IsPaneOpen { get; set; }

		[GeneratedDependencyProperty(DefaultValue = 240D)]
		public partial double OpenPaneLength { get; set; }

		[GeneratedDependencyProperty]
		public partial double NegativeOpenPaneLength { get; set; }

		[GeneratedDependencyProperty]
		public partial ISidebarViewModel? ViewModel { get; set; }

		[GeneratedDependencyProperty]
		public partial ISidebarItemModel? SelectedItem { get; set; }

		[GeneratedDependencyProperty]
		public partial object? MenuItemsSource { get; set; }

		partial void OnDisplayModePropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			UpdateDisplayMode();
		}

		partial void OnIsPaneOpenPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			UpdateMinimalMode();
		}

		partial void OnOpenPaneLengthPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			NegativeOpenPaneLength = -(double)(e.NewValue);
			UpdateOpenPaneLengthColumn();
		}
	}
}
