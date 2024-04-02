// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.KeyboardShortcut
{
	public class KeyboardShortcutItemStyleSelector : StyleSelector
	{
		public Style DefaultItemStyle { get; set; }

		public Style AccentItemStyle { get; set; }

		public Style RevealItemStyle { get; set; }

		protected override Style SelectStyleCore(object item, DependencyObject container)
		{
			if (container is KeyboardShortcutItem keyboardShortcutItem)
			{
				return keyboardShortcutItem.ItemType switch
				{
					KeyboardShortcutItemKind.Default => DefaultItemStyle,
					KeyboardShortcutItemKind.Accent => AccentItemStyle,
					KeyboardShortcutItemKind.Reveal => RevealItemStyle,
					_ => DefaultItemStyle,
				};
			}

			return DefaultItemStyle;
		}
	}
}
