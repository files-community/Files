// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Extensions
{
	/// <summary>
	/// Provides extension for ComboBox.
	/// </summary>
	/// <remarks>
	/// - IsKeepWidthEnabled: Prevents from opening popup at wrong position.
	/// </remarks>
	public sealed class ComboBoxExtensions : DependencyObject
	{
		private static double _cachedWidth;

		public static readonly DependencyProperty IsKeepWidthEnabledProperty =
			DependencyProperty.RegisterAttached(
				"IsKeepWidthEnabled",
				typeof(bool),
				typeof(ComboBoxExtensions),
				new PropertyMetadata(false, OnIsKeepWidthEnabledProperty));

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
				comboBox.DropDownOpened -= ComboBox_DropDownOpened;
				comboBox.DropDownOpened += ComboBox_DropDownOpened;
				comboBox.DropDownClosed -= ComboBox_DropDownClosed;
				comboBox.DropDownClosed += ComboBox_DropDownClosed;
			}
		}

		private static void ComboBox_DropDownOpened(object? sender, object e)
		{
			if (sender is ComboBox comboBox)
				comboBox.Width = _cachedWidth;
		}

		private static void ComboBox_DropDownClosed(object? sender, object e)
		{
			if (sender is ComboBox comboBox)
				comboBox.Width = double.NaN;
		}
	}
}
