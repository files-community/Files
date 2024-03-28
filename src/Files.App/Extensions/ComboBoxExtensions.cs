// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Extensions
{
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
				comboBox.DropDownClosed -= ComboBox_DropDownClosed;
				comboBox.DropDownClosed += ComboBox_DropDownClosed;
				comboBox.SizeChanged -= ComboBox_SizeChanged;
				comboBox.SizeChanged += ComboBox_SizeChanged;
			}
		}

		private static void ComboBox_DropDownClosed(object? sender, object e)
		{
			if (sender is ComboBox comboBox &&
				comboBox.FindDescendant<ContentPresenter>() is ContentPresenter contentPresenter &&
				contentPresenter.FindDescendant<TextBlock>() is TextBlock textBlock)
			{
				comboBox.MinWidth = 12 + textBlock.ActualWidth + 38;
			}
		}

		private static void ComboBox_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (sender is ComboBox comboBox &&
				comboBox.FindDescendant<ContentPresenter>() is ContentPresenter contentPresenter &&
				contentPresenter.FindDescendant<TextBlock>() is TextBlock textBlock)
			{
				comboBox.MinWidth = 12 + textBlock.ActualWidth + 38;
			}
		}
	}
}
