// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.KeyboardShortcut
{
	public sealed partial class KeyboardShortcutItem
	{
		public static readonly DependencyProperty ItemTypeProperty =
			DependencyProperty.Register(
				nameof(ItemType),
				typeof(KeyboardShortcutItemKind),
				typeof(KeyboardShortcutItem),
				new PropertyMetadata(defaultValue: KeyboardShortcutItemKind.Default, (d, e) => ((KeyboardShortcutItem)d).OnItemTypePropertyChanged((KeyboardShortcutItemKind)e.OldValue, (KeyboardShortcutItemKind)e.NewValue)));

		public KeyboardShortcutItemKind ItemType
		{
			get => (KeyboardShortcutItemKind)GetValue(ItemTypeProperty);
			set => SetValue(ItemTypeProperty, value);
		}

		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register(
				nameof(Text),
				typeof(string),
				typeof(KeyboardShortcutItem),
				new PropertyMetadata(defaultValue: string.Empty, (d, e) => ((KeyboardShortcutItem)d).OnTextPropertyChanged((string)e.OldValue, (string)e.NewValue)));

		public string Text
		{
			get => (string)GetValue(ItemTypeProperty);
			set => SetValue(ItemTypeProperty, value);
		}

		public void OnItemTypePropertyChanged(KeyboardShortcutItemKind oldValue, KeyboardShortcutItemKind newValue)
		{
			OnItemTypeChanged();
		}

		public void OnTextPropertyChanged(string oldValue, string newValue)
		{
			OnTextChanged();
		}
	}
}
