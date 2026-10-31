// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using WinRT;

namespace Files.App.UserControls.Menus
{
	[Microsoft.UI.Xaml.Data.Bindable]
	public sealed class MenuFlyoutSubItemCustomProperties : DependencyObject
	{
		public static readonly DependencyProperty BitmapIconProperty =
			DependencyProperty.Register("BitmapIcon", typeof(BitmapImage), typeof(MenuFlyoutSubItemCustomProperties), new PropertyMetadata(null, OnBitmapIconChanged));

		[DynamicWindowsRuntimeCast(typeof(BitmapImage))]
		public static BitmapImage? GetBitmapIcon(DependencyObject obj)
		{
			return obj.GetValue(BitmapIconProperty) as BitmapImage;
		}

		public static void SetBitmapIcon(DependencyObject obj, BitmapImage? value)
		{
			obj.SetValue(BitmapIconProperty, value);
		}

		[DynamicWindowsRuntimeCast(typeof(MenuFlyoutSubItem))]
		private static void OnBitmapIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is MenuFlyoutSubItem item)
				item.Icon = e.NewValue is not null ? new IconSourceElement() : null;
		}

		public static readonly DependencyProperty ThemedIconStyleProperty =
			DependencyProperty.Register("ThemedIconStyle", typeof(Style), typeof(MenuFlyoutSubItemCustomProperties), new PropertyMetadata(null, OnThemedIconStyleChanged));

		[DynamicWindowsRuntimeCast(typeof(Style))]
		public static Style GetThemedIconStyle(DependencyObject obj)
		{
			return (Style)obj.GetValue(ThemedIconStyleProperty);
		}

		public static void SetThemedIconStyle(DependencyObject obj, Style value)
		{
			obj.SetValue(ThemedIconStyleProperty, value);
		}

		[DynamicWindowsRuntimeCast(typeof(MenuFlyoutSubItem))]
		private static void OnThemedIconStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			// Reserve the icon column (the ThemedIcon itself is drawn by the template); mirrors the BitmapIcon path.
			if (d is MenuFlyoutSubItem item)
				item.Icon = e.NewValue is not null ? new IconSourceElement() : null;
		}
	}
}
