// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.KeyboardShortcut
{
	public sealed partial class KeyboardShortcutItem
	{
		public static readonly DependencyProperty IsSurroundedProperty =
			DependencyProperty.Register(
				nameof(IsSurrounded),
				typeof(bool),
				typeof(KeyboardShortcutItem),
				new PropertyMetadata(defaultValue: true, (d, e) => ((KeyboardShortcutItem)d).OnIsSurroundedPropertyChanged((bool)e.OldValue, (bool)e.NewValue)));

		public bool IsSurrounded
		{
			get => (bool)GetValue(IsSurroundedProperty);
			set => SetValue(IsSurroundedProperty, value);
		}

		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register(
				nameof(Text),
				typeof(string),
				typeof(KeyboardShortcutItem),
				new PropertyMetadata(defaultValue: string.Empty, (d, e) => ((KeyboardShortcutItem)d).OnTextPropertyChanged((string)e.OldValue, (string)e.NewValue)));

		public string Text
		{
			get => (string)GetValue(TextProperty);
			set => SetValue(TextProperty, value);
		}

		public void OnIsSurroundedPropertyChanged(bool oldValue, bool newValue)
		{
			OnIsSurroundedChanged();
		}

		public void OnTextPropertyChanged(string oldValue, string newValue)
		{
			OnTextChanged();
		}
	}
}
