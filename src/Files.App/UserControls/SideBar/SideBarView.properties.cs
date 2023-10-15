// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;

namespace Files.App.UserControls.SideBar
{
	public sealed partial class SideBarView
	{
		public SideBarPaneDisplayMode DisplayMode
		{
			get => (SideBarPaneDisplayMode)GetValue(DisplayModeProperty);
			set => SetValue(DisplayModeProperty, value);
		}

		public static readonly DependencyProperty DisplayModeProperty =
			DependencyProperty.Register(
				nameof(DisplayMode),
				typeof(SideBarPaneDisplayMode),
				typeof(SideBarView),
				new PropertyMetadata(SideBarPaneDisplayMode.Expanded, OnPropertyChanged));

		public UIElement InnerContent
		{
			get => (UIElement)GetValue(InnerContentProperty);
			set => SetValue(InnerContentProperty, value);
		}

		public static readonly DependencyProperty InnerContentProperty =
			DependencyProperty.Register(
				nameof(InnerContent),
				typeof(UIElement),
				typeof(SideBarView),
				new PropertyMetadata(null));

		public UIElement Footer
		{
			get => (UIElement)GetValue(FooterProperty);
			set => SetValue(FooterProperty, value);
		}

		public static readonly DependencyProperty FooterProperty =
			DependencyProperty.Register(
				nameof(Footer),
				typeof(UIElement),
				typeof(SideBarView),
				new PropertyMetadata(null));

		public bool IsPaneOpen
		{
			get => (bool)GetValue(IsPaneOpenProperty);
			set => SetValue(IsPaneOpenProperty, value);
		}

		public static readonly DependencyProperty IsPaneOpenProperty =
			DependencyProperty.Register(
				nameof(IsPaneOpen),
				typeof(bool),
				typeof(SideBarView),
				new PropertyMetadata(false, OnPropertyChanged));

		public double OpenPaneLength
		{
			get => (double)GetValue(OpenPaneLengthProperty);
			set
			{
				SetValue(OpenPaneLengthProperty, value);
				NegativeOpenPaneLength = -value;
			}
		}

		public static readonly DependencyProperty OpenPaneLengthProperty =
			DependencyProperty.Register(
				nameof(OpenPaneLength),
				typeof(double),
				typeof(SideBarView),
				new PropertyMetadata(240d, OnPropertyChanged));

		public double NegativeOpenPaneLength
		{
			get => (double)GetValue(NegativeOpenPaneLengthProperty);
			set => SetValue(NegativeOpenPaneLengthProperty, value);
		}

		public static readonly DependencyProperty NegativeOpenPaneLengthProperty =
			DependencyProperty.Register(
				nameof(NegativeOpenPaneLength),
				typeof(double),
				typeof(SideBarView),
				new PropertyMetadata(null));

		public ISideBarViewModel ViewModel
		{
			get => (ISideBarViewModel)GetValue(ViewModelProperty);
			set => SetValue(ViewModelProperty, value);
		}

		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register(
				nameof(ViewModel),
				typeof(ISideBarViewModel),
				typeof(SideBarView),
				new PropertyMetadata(null));

		public ISideBarItemModel SelectedItem
		{
			get => (ISideBarItemModel)GetValue(SelectedItemProperty);
			set => SetValue(SelectedItemProperty, value);
		}

		public static readonly DependencyProperty SelectedItemProperty =
			DependencyProperty.Register(
				nameof(SelectedItem),
				typeof(ISideBarItemModel),
				typeof(SideBarView),
				new PropertyMetadata(null));

		public static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is not SideBarView control) return;

			if (e.Property == OpenPaneLengthProperty)
				control.UpdateOpenPaneLengthColumn();
			else if (e.Property == DisplayModeProperty)
				control.UpdateDisplayMode();
			else if (e.Property == IsPaneOpenProperty)
				control.UpdateMinimalMode();
		}
	}
}
