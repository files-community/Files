// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Extensions
{
	internal class ComboBoxExtensions : DependencyObject
	{
		public static readonly DependencyProperty IsKeepWidthEnabledProperty =
			DependencyProperty.RegisterAttached(
				"IsKeepWidthEnabled",
				typeof(bool),
				typeof(ComboBoxExtensions),
				new PropertyMetadata(null, OnIsKeepWidthEnabledProperty));

		public static bool GetIsKeepWidthEnabled(DependencyObject obj)
		{
			return (bool)obj.GetValue(IsKeepWidthEnabledProperty);
		}

		public static void SetIsKeepWidthEnabled(DependencyObject obj, bool value)
		{
			obj.SetValue(IsKeepWidthEnabledProperty, value);
		}

		private static void OnIsKeepWidthEnabledProperty(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is ComboBox comboBox)
			{
				comboBox.MinWidth = comboBox.ActualWidth;

				comboBox.SelectionChanged -= ComboBox_SelectionChanged;
				comboBox.SelectionChanged += ComboBox_SelectionChanged;
			}
		}

		private static void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (sender is ComboBox comboBox)
				comboBox.MinWidth = comboBox.ActualWidth;
		}
	}
}
