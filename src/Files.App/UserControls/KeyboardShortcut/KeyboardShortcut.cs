// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Files.App.UserControls.KeyboardShortcut
{
	public sealed partial class KeyboardShortcut : Control
	{
		internal const string KeyboardShortcutItemsControl = "PART_KeyboardShortcutItemsControl";

		public KeyboardShortcut()
		{
			DefaultStyleKey = typeof(KeyboardShortcut);
		}

		private void OnItemTypeChanged()
		{
		}

		private void OnSizeChanged()
		{
		}

		private void OnHotKeysChanged()
		{
			if (HotKeys.IsEmpty)
				return;

			List<KeyboardShortcutItem> items = new();

			foreach (var item in HotKeys)
			{
				if (items.Any())
				{
					items.Add(new() { Text = ",", ItemType = KeyboardShortcutItemKind.TextOnly, Size = Size });
				}

				switch(item.Key, item.Modifier)
				{
					// No keys or modifiers specified
					case (Keys.None, KeyModifiers.None):
						break;

					// Key modifiers only
					case (Keys.None, _):
						GetModifierCode(item.Modifier);
						items.RemoveAt(items.Count - 1);
						break;

					// Keys only
					case (_, KeyModifiers.None):
						var key = HotKey.keys[item.Key];
						items.Add(new() { Text = key, ItemType = ItemType, Size = Size });
						break;

					// Others
					default:
						GetModifierCode(item.Modifier);
						key = HotKey.keys[item.Key];
						items.Add(new() { Text = key, ItemType = ItemType, Size = Size });
						break;
				}

				void GetModifierCode(KeyModifiers modifier)
				{
					if (modifier.HasFlag(KeyModifiers.Menu))
					{
						items.Add(new() { Text = HotKey.modifiers[KeyModifiers.Menu], ItemType = ItemType, Size = Size });
						items.Add(new() { Text = "+", ItemType = KeyboardShortcutItemKind.TextOnly, Size = Size });
					}
					if (modifier.HasFlag(KeyModifiers.Ctrl))
					{
						items.Add(new() { Text = HotKey.modifiers[KeyModifiers.Ctrl], ItemType = ItemType, Size = Size });
						items.Add(new() { Text = "+", ItemType = KeyboardShortcutItemKind.TextOnly, Size = Size });
					}
					if (modifier.HasFlag(KeyModifiers.Shift))
					{
						items.Add(new() { Text = HotKey.modifiers[KeyModifiers.Shift], ItemType = ItemType, Size = Size });
						items.Add(new() { Text = "+", ItemType = KeyboardShortcutItemKind.TextOnly, Size = Size });
					}
					if (modifier.HasFlag(KeyModifiers.Win))
					{
						items.Add(new() { Text = HotKey.modifiers[KeyModifiers.Win], ItemType = ItemType, Size = Size });
						items.Add(new() { Text = "+", ItemType = KeyboardShortcutItemKind.TextOnly, Size = Size });
					}
				}
			}

			// Set value
			if (GetTemplateChild(KeyboardShortcutItemsControl) is ItemsControl keyboardShortcutItemsControl)
			{
				keyboardShortcutItemsControl.ItemsSource = items;
			}
		}
	}
}
