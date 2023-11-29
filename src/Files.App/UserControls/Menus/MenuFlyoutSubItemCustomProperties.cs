// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Files.App.UserControls.Menus
{
	[Microsoft.UI.Xaml.Data.Bindable]
	public class MenuFlyoutSubItemCustomProperties : DependencyObject
	{
		public static readonly DependencyProperty BitmapIconProperty =
			DependencyProperty.Register("BitmapIcon", typeof(BitmapImage), typeof(MenuFlyoutSubItemCustomProperties), new PropertyMetadata(null, OnBitmapIconChanged));

		public static BitmapImage GetBitmapIcon(DependencyObject obj)
		{
			return (BitmapImage)obj.GetValue(BitmapIconProperty);
		}

		public static void SetBitmapIcon(DependencyObject obj, BitmapImage value)
		{
			obj.SetValue(BitmapIconProperty, value);
		}

		private static void OnBitmapIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			(d as MenuFlyoutSubItem).Icon = e.NewValue is not null ? new IconSourceElement() : null;
		}
	}
}
