// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.KeyboardShortcut
{
	public sealed partial class KeyboardShortcut
	{
		public static readonly DependencyProperty HotKeysProperty =
			DependencyProperty.Register(
				nameof(HotKeys),
				typeof(HotKeyCollection),
				typeof(KeyboardShortcut),
				new PropertyMetadata(defaultValue: new(), (d, e) => ((KeyboardShortcut)d).OnHotKeysPropertyChanged()));

		public HotKeyCollection HotKeys
		{
			get => (HotKeyCollection)GetValue(HotKeysProperty);
			set => SetValue(HotKeysProperty, value);
		}

		private void OnHotKeysPropertyChanged()
		{
			OnHotKeysChanged();
		}
	}
}
