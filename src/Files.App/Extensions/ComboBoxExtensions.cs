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
				comboBox.SelectionChanged -= ComboBox_SelectionChanged;
				comboBox.SelectionChanged += ComboBox_SelectionChanged;
				comboBox.SizeChanged -= ComboBox_SizeChanged;
				comboBox.SizeChanged += ComboBox_SizeChanged;
			}
		}

		private static void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (sender is ComboBox comboBox &&
				comboBox.FindDescendant<ContentPresenter>() is ContentPresenter contentPresenter &&
				contentPresenter.FindDescendant<FrameworkElement>() is FrameworkElement frameworkElement)
			{
				comboBox.MinWidth = 12 + frameworkElement.ActualWidth + 38;
			}
		}

		private static void ComboBox_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (sender is ComboBox comboBox &&
				comboBox.FindDescendant<ContentPresenter>() is ContentPresenter contentPresenter)
			{
				comboBox.MinWidth = 12 + contentPresenter.ActualWidth + 38;
			}
		}
	}
}
