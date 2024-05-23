// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;

namespace Files.App.UserControls.Sidebar
{
	public sealed partial class SidebarView
	{
		public SidebarDisplayMode DisplayMode
		{
			get { return (SidebarDisplayMode)GetValue(DisplayModeProperty); }
			set { SetValue(DisplayModeProperty, value); }
		}
		public static readonly DependencyProperty DisplayModeProperty =
			DependencyProperty.Register(nameof(DisplayMode), typeof(SidebarDisplayMode), typeof(SidebarView), new PropertyMetadata(SidebarDisplayMode.Expanded, OnPropertyChanged));

		public UIElement ContentHeader
		{
			get { return (UIElement)GetValue(ContentHeaderProperty); }
			set { SetValue(ContentHeaderProperty, value); }
		}
		public static readonly DependencyProperty ContentHeaderProperty =
			DependencyProperty.Register(nameof(ContentHeader), typeof(UIElement), typeof(SidebarView), new PropertyMetadata(null));
		
		public UIElement InnerContent
		{
			get { return (UIElement)GetValue(InnerContentProperty); }
			set { SetValue(InnerContentProperty, value); }
		}
		public static readonly DependencyProperty InnerContentProperty =
			DependencyProperty.Register(nameof(InnerContent), typeof(UIElement), typeof(SidebarView), new PropertyMetadata(null));
		
		public UIElement ContentFooter
		{
			get { return (UIElement)GetValue(ContentFooterProperty); }
			set { SetValue(ContentFooterProperty, value); }
		}
		public static readonly DependencyProperty ContentFooterProperty =
			DependencyProperty.Register(nameof(ContentFooter), typeof(UIElement), typeof(SidebarView), new PropertyMetadata(null));

		public object PaneFooterItemsSource
		{
			get => (object)GetValue(PaneFooterItemsSourceProperty);
			set => SetValue(PaneFooterItemsSourceProperty, value);
		}
		public static readonly DependencyProperty PaneFooterItemsSourceProperty =
			DependencyProperty.Register(nameof(PaneFooterItemsSource), typeof(object), typeof(SidebarView), new PropertyMetadata(null, OnPropertyChanged));

		public bool IsPaneOpen
		{
			get { return (bool)GetValue(IsPaneOpenProperty); }
			set { SetValue(IsPaneOpenProperty, value); }
		}
		public static readonly DependencyProperty IsPaneOpenProperty =
			DependencyProperty.Register(nameof(IsPaneOpen), typeof(bool), typeof(SidebarView), new PropertyMetadata(false, OnPropertyChanged));

		public double OpenPaneLength
		{
			get { return (double)GetValue(OpenPaneLengthProperty); }
			set
			{
				SetValue(OpenPaneLengthProperty, value);
				NegativeOpenPaneLength = -value;
			}
		}
		public static readonly DependencyProperty OpenPaneLengthProperty =
			DependencyProperty.Register(nameof(OpenPaneLength), typeof(double), typeof(SidebarView), new PropertyMetadata(240d, OnPropertyChanged));

		public double NegativeOpenPaneLength
		{
			get { return (double)GetValue(NegativeOpenPaneLengthProperty); }
			set { SetValue(NegativeOpenPaneLengthProperty, value); }
		}
		public static readonly DependencyProperty NegativeOpenPaneLengthProperty =
			DependencyProperty.Register(nameof(NegativeOpenPaneLength), typeof(double), typeof(SidebarView), new PropertyMetadata(null));

		public ISidebarViewModel ViewModel
		{
			get => (ISidebarViewModel)GetValue(ViewModelProperty);
			set => SetValue(ViewModelProperty, value);
		}
		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register(nameof(ViewModel), typeof(ISidebarViewModel), typeof(SidebarView), new PropertyMetadata(null));

		public ISidebarItemModel SelectedItem
		{
			get => (ISidebarItemModel)GetValue(SelectedItemProperty);
			set
			{
				SetValue(SelectedItemProperty, value);
			}
		}
		public static readonly DependencyProperty SelectedItemProperty =
			DependencyProperty.Register(nameof(SelectedItem), typeof(ISidebarItemModel), typeof(SidebarView), new PropertyMetadata(null));

		public static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is not SidebarView control)
				return;

			if (e.Property == OpenPaneLengthProperty)
			{
				control.UpdateOpenPaneLengthColumn();
			}
			else if (e.Property == DisplayModeProperty)
			{
				control.UpdateDisplayMode();
			}
			else if (e.Property == IsPaneOpenProperty)
			{
				control.UpdateMinimalMode();
			}
			else if (e.Property == PaneFooterItemsSourceProperty)
			{
				control.UpdatePaneFooterVisibility();
			}
		}
	}
}
