// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using WinRT;

// The User Control element template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls
{
	public sealed partial class MenuFlyoutItemWithImage : MenuFlyoutItem
	{
		public BitmapImage? BitmapIcon
		{
			[DynamicWindowsRuntimeCast(typeof(BitmapImage))]
			get { return GetValue(BitmapIconProperty) as BitmapImage; }
			set { SetValue(BitmapIconProperty, value); }
		}

		public static readonly DependencyProperty BitmapIconProperty =
			DependencyProperty.Register("BitmapIcon", typeof(BitmapImage), typeof(MenuFlyoutItemWithImage), new PropertyMetadata(null, OnBitmapIconChanged));

		[DynamicWindowsRuntimeCast(typeof(MenuFlyoutItem))]
		private static void OnBitmapIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is MenuFlyoutItem item)
				item.Icon = e.NewValue is not null ? new IconSourceElement() : null;
		}

		public MenuFlyoutItemWithImage()
		{
			InitializeComponent();
		}
	}
}
