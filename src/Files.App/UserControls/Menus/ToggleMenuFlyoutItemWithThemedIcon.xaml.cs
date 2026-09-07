// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT;

// The User Control element template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls
{
	public sealed partial class ToggleMenuFlyoutItemWithThemedIcon : ToggleMenuFlyoutItem
	{
		public Style? ThemedIconStyle
		{
			[DynamicWindowsRuntimeCast(typeof(Style))]
			get { return GetValue(ThemedIconStyleProperty) as Style; }
			set { SetValue(ThemedIconStyleProperty, value); }
		}

		public static readonly DependencyProperty ThemedIconStyleProperty =
			DependencyProperty.Register("ThemedIconStyle", typeof(Style), typeof(ToggleMenuFlyoutItemWithThemedIcon), new PropertyMetadata(null, OnThemedIconStyleChanged));

		[DynamicWindowsRuntimeCast(typeof(ToggleMenuFlyoutItem))]
		private static void OnThemedIconStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is ToggleMenuFlyoutItem item)
				item.Icon = e.NewValue is not null ? new IconSourceElement() : null;
		}

		public ToggleMenuFlyoutItemWithThemedIcon()
		{
			InitializeComponent();
		}
	}
}
