// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Files.App.UserControls
{
	public sealed partial class MenuFlyoutItemWithImage : MenuFlyoutItem
	{
		public BitmapImage BitmapIcon
		{
			get { return (BitmapImage)GetValue(BitmapIconProperty); }
			set { SetValue(BitmapIconProperty, value); }
		}

		public static readonly DependencyProperty BitmapIconProperty =
			DependencyProperty.Register("BitmapIcon", typeof(BitmapImage), typeof(MenuFlyoutItemWithImage), new PropertyMetadata(null, OnBitmapIconChanged));

		private static void OnBitmapIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			(d as MenuFlyoutItem).Icon = e.NewValue is not null ? new IconSourceElement() : null;
		}

		public MenuFlyoutItemWithImage()
		{
			InitializeComponent();
		}
	}
}