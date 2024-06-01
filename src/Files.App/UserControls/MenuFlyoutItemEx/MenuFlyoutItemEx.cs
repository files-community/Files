// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Files.App.UserControls
{
	/// <summary>
	/// Represents control that is derived from <see cref="MenuFlyoutItem"/> in order to use <see cref="ThemedIcon"/> or <see cref="BitmapImage"/> for the icon area.
	/// </summary>
	public sealed partial class MenuFlyoutItemEx : MenuFlyoutItem
	{
		public Style ThemedIconStyle
		{
			get => (Style)GetValue(ThemedIconStyleProperty);
			set => SetValue(ThemedIconStyleProperty, value);
		}

		public BitmapImage Image
		{
			get => (BitmapImage)GetValue(BitmapIconProperty);
			set => SetValue(BitmapIconProperty, value);
		}

		public static readonly DependencyProperty ThemedIconStyleProperty =
			DependencyProperty.Register(
				nameof(ThemedIconStyle),
				typeof(Style),
				typeof(MenuFlyoutItemEx),
				new PropertyMetadata(null, OnThemedIconStyleChanged));

		public static readonly DependencyProperty BitmapIconProperty =
			DependencyProperty.Register(
				nameof(Image),
				typeof(BitmapImage),
				typeof(MenuFlyoutItemEx),
				new PropertyMetadata(null, OnBitmapIconChanged));

		public MenuFlyoutItemEx()
		{
			DefaultStyleKey = typeof(MenuFlyoutItemEx);
		}

		private static void OnThemedIconStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is MenuFlyoutItem item)
				item.Icon = e.NewValue is not null ? new IconSourceElement() : null;
		}

		private static void OnBitmapIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is MenuFlyoutItem item)
				item.Icon = e.NewValue is not null ? new IconSourceElement() : null;
		}
	}
}
