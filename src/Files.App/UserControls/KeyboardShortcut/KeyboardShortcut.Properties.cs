// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.KeyboardShortcut
{
	public sealed partial class KeyboardShortcut
	{
		public static readonly DependencyProperty ShortcutTextProperty =
			DependencyProperty.Register(
				nameof(ShortcutText),
				typeof(string),
				typeof(KeyboardShortcut),
				new PropertyMetadata(defaultValue: string.Empty, (d, e) => ((KeyboardShortcut)d).OnShortcutTextPropertyChanged((string)e.OldValue, (string)e.NewValue)));

		public string ShortcutText
		{
			get => (string)GetValue(ShortcutTextProperty);
			set => SetValue(ShortcutTextProperty, value);
		}

		private void OnShortcutTextPropertyChanged(string oldValue, string newValue)
		{
			OnShortcutTextChanged();
		}
	}
}
