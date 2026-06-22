// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
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

		public UIElement InnerContent
		{
			get { return (UIElement)GetValue(InnerContentProperty); }
			set { SetValue(InnerContentProperty, value); }
		}
		public static readonly DependencyProperty InnerContentProperty =
			DependencyProperty.Register(nameof(InnerContent), typeof(UIElement), typeof(SidebarView), new PropertyMetadata(null));

		public UIElement SidebarContent
		{
			get { return (UIElement)GetValue(SidebarContentProperty); }
			set { SetValue(SidebarContentProperty, value); }
		}
		public static readonly DependencyProperty SidebarContentProperty =
			DependencyProperty.Register("SidebarContent", typeof(UIElement), typeof(SidebarView), new PropertyMetadata(null));

		public UIElement Header
		{
			get { return (UIElement)GetValue(HeaderProperty); }
			set { SetValue(HeaderProperty, value); }
		}
		public static readonly DependencyProperty HeaderProperty =
			DependencyProperty.Register(nameof(Header), typeof(UIElement), typeof(SidebarView), new PropertyMetadata(null));

		public UIElement Footer
		{
			get { return (UIElement)GetValue(FooterProperty); }
			set { SetValue(FooterProperty, value); }
		}
		public static readonly DependencyProperty FooterProperty =
			DependencyProperty.Register("Footer", typeof(UIElement), typeof(SidebarView), new PropertyMetadata(null));

		public Microsoft.UI.Xaml.Media.Brush PaneBackgroundBrush
		{
			get { return (Microsoft.UI.Xaml.Media.Brush)GetValue(PaneBackgroundBrushProperty); }
			set { SetValue(PaneBackgroundBrushProperty, value); }
		}
		public static readonly DependencyProperty PaneBackgroundBrushProperty =
			DependencyProperty.Register(nameof(PaneBackgroundBrush), typeof(Microsoft.UI.Xaml.Media.Brush), typeof(SidebarView), new PropertyMetadata(null));

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
			DependencyProperty.Register(nameof(NegativeOpenPaneLength), typeof(double), typeof(SidebarView), new PropertyMetadata(-240d));

		public bool CanResizePane
		{
			get => (bool)GetValue(CanResizePaneProperty);
			set => SetValue(CanResizePaneProperty, value);
		}
		public static readonly DependencyProperty CanResizePaneProperty =
			DependencyProperty.Register(nameof(CanResizePane), typeof(bool), typeof(SidebarView), new PropertyMetadata(true, OnPropertyChanged));

		public ISidebarItemModel SelectedItem
		{
			get => (ISidebarItemModel)GetValue(SelectedItemProperty);
			set
			{
				SetValue(SelectedItemProperty, value);
			}
		}
		public static readonly DependencyProperty SelectedItemProperty =
			DependencyProperty.Register(nameof(SelectedItem), typeof(ISidebarItemModel), typeof(SidebarView), new PropertyMetadata(null, OnSelectedItemChanged));

		// Broadcasts SelectedItem changes to every realized row in MenuItemsHost instead of relying on each row's own RegisterPropertyChangedCallback. The per-row callback only registers after Loaded fires, so a row prepared but not yet loaded (or unloaded then re-loaded mid-recycle) can otherwise miss a SelectedItem change and keep its stale IsSelected — visible as multiple "selected" rows.
		private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is not SidebarView view || view.MenuItemsHost is null)
				return;
			for (int i = 0; ; i++)
			{
				var element = view.MenuItemsHost.TryGetElement(i);
				if (element is null)
					break;
				if (element is SidebarItem sidebarItem)
					sidebarItem.ReevaluateSelectionFromOwner();
			}
		}

		public object MenuItemsSource
		{
			get => (object)GetValue(MenuItemsSourceProperty);
			set => SetValue(MenuItemsSourceProperty, value);
		}
		public static readonly DependencyProperty MenuItemsSourceProperty =
			DependencyProperty.Register(nameof(MenuItemsSource), typeof(object), typeof(SidebarView), new PropertyMetadata(null));

		// Default TimeSpan.Zero disables hover-to-open; the hosting app supplies the timing policy.
		public TimeSpan HoverToOpenDelay
		{
			get => (TimeSpan)GetValue(HoverToOpenDelayProperty);
			set => SetValue(HoverToOpenDelayProperty, value);
		}
		public static readonly DependencyProperty HoverToOpenDelayProperty =
			DependencyProperty.Register(nameof(HoverToOpenDelay), typeof(TimeSpan), typeof(SidebarView), new PropertyMetadata(TimeSpan.Zero));

		// Default TimeSpan.Zero disables hover-to-expand; the hosting app supplies the timing policy.
		public TimeSpan HoverToExpandDelay
		{
			get => (TimeSpan)GetValue(HoverToExpandDelayProperty);
			set => SetValue(HoverToExpandDelayProperty, value);
		}
		public static readonly DependencyProperty HoverToExpandDelayProperty =
			DependencyProperty.Register(nameof(HoverToExpandDelay), typeof(TimeSpan), typeof(SidebarView), new PropertyMetadata(TimeSpan.Zero));

		// Off by default; flat-list sidebars (Settings) collapse the chevron column. Opt in for hierarchical sidebars (main tree view).
		public bool SupportsExpansion { get; set; }

		public static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is not SidebarView control) return;

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
			else if (e.Property == CanResizePaneProperty)
			{
				control.UpdateResizerAvailability();
			}
		}
	}
}
