// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Files.App.UserControls.Menus
{
	[Microsoft.UI.Xaml.Data.Bindable]
	public sealed class MenuFlyoutSubItemCustomProperties : DependencyObject
	{
		public static readonly DependencyProperty BitmapIconProperty =
			DependencyProperty.Register("BitmapIcon", typeof(BitmapImage), typeof(MenuFlyoutSubItemCustomProperties), new PropertyMetadata(null, OnBitmapIconChanged));

		public static BitmapImage? GetBitmapIcon(DependencyObject obj)
		{
			return obj.GetValue(BitmapIconProperty) as BitmapImage;
		}

		public static void SetBitmapIcon(DependencyObject obj, BitmapImage? value)
		{
			obj.SetValue(BitmapIconProperty, value);
		}

		private static void OnBitmapIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is MenuFlyoutSubItem item)
				item.Icon = e.NewValue is not null ? new IconSourceElement() : null;
		}
	}
}
