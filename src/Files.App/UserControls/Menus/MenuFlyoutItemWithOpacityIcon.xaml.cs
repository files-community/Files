// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// The User Control element template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls
{
	public sealed partial class MenuFlyoutItemWithOpacityIcon: MenuFlyoutItem
	{
		public Style OpacityStyle
		{
			get { return (Style)GetValue(OpacityStyleProperty); }
			set { SetValue(OpacityStyleProperty, value); }
		}

		public static readonly DependencyProperty OpacityStyleProperty =
			DependencyProperty.Register("OpacityStyle", typeof(Style), typeof(MenuFlyoutItemWithOpacityIcon), new PropertyMetadata(null, OnOpacityStyleChanged));

		private static void OnOpacityStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			(d as MenuFlyoutItem).Icon = e.NewValue is not null ? new IconSourceElement() : null;
		}

		public MenuFlyoutItemWithOpacityIcon()
		{
			InitializeComponent();
		}
	}
}