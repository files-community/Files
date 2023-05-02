// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Files.App.UserControls.Settings
{
	//// Implement properties for ItemsControl like behavior.
	public partial class SettingsExpander
	{
		public IList<object> Items
		{
			get { return (IList<object>)GetValue(ItemsProperty); }
			set { SetValue(ItemsProperty, value); }
		}

		public static readonly DependencyProperty ItemsProperty =
			DependencyProperty.Register(nameof(Items), typeof(IList<object>), typeof(SettingsExpander), new PropertyMetadata(null, OnItemsConnectedPropertyChanged));

		public object ItemsSource
		{
			get { return (object)GetValue(ItemsSourceProperty); }
			set { SetValue(ItemsSourceProperty, value); }
		}

		public static readonly DependencyProperty ItemsSourceProperty =
			DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(SettingsExpander), new PropertyMetadata(null, OnItemsConnectedPropertyChanged));

		public object ItemTemplate
		{
			get { return (object)GetValue(ItemTemplateProperty); }
			set { SetValue(ItemTemplateProperty, value); }
		}

		public static readonly DependencyProperty ItemTemplateProperty =
			DependencyProperty.Register(nameof(ItemTemplate), typeof(object), typeof(SettingsExpander), new PropertyMetadata(null));

		public StyleSelector ItemContainerStyleSelector
		{
			get { return (StyleSelector)GetValue(ItemContainerStyleSelectorProperty); }
			set { SetValue(ItemContainerStyleSelectorProperty, value); }
		}

		public static readonly DependencyProperty ItemContainerStyleSelectorProperty =
			DependencyProperty.Register(nameof(ItemContainerStyleSelector), typeof(StyleSelector), typeof(SettingsExpander), new PropertyMetadata(null));

		private static void OnItemsConnectedPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyObject is SettingsExpander expander && expander._itemsRepeater is not null)
			{
				var datasource = expander.ItemsSource;

				if (datasource is null)
				{
					datasource = expander.Items;
				}

				expander._itemsRepeater.ItemsSource = datasource;
			}
		}

		private void ItemsRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
		{
			if (ItemContainerStyleSelector != null &&
				args.Element is FrameworkElement element &&
				element.ReadLocalValue(FrameworkElement.StyleProperty) == DependencyProperty.UnsetValue)
			{
				// TODO: Get item from args.Index?
				element.Style = ItemContainerStyleSelector.SelectStyle(null, element);
			}
		}
	}
}
