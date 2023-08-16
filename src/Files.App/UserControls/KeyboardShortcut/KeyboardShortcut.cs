// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Text;

namespace Files.App.UserControls.KeyboardShortcut
{
	public sealed partial class KeyboardShortcut : Control
	{
		internal const string KeyboardShortcutItemsControl = "PART_KeyboardShortcutItemsControl";

		public KeyboardShortcut()
		{
			DefaultStyleKey = typeof(KeyboardShortcut);
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
					items.Add(new() { Text = ",", ItemType = KeyboardShortcutItemKind.Reveal });
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
						items.Add(new() { Text = key, ItemType = KeyboardShortcutItemKind.Default });
						break;

					// Others
					default:
						GetModifierCode(item.Modifier);
						key = HotKey.keys[item.Key];
						items.Add(new() { Text = key, ItemType = KeyboardShortcutItemKind.Default });
						break;
				}

				void GetModifierCode(KeyModifiers modifier)
				{
					if (modifier.HasFlag(KeyModifiers.Menu))
					{
						items.Add(new() { Text = HotKey.modifiers[KeyModifiers.Menu], ItemType = KeyboardShortcutItemKind.Default });
						items.Add(new() { Text = "+", ItemType = KeyboardShortcutItemKind.Reveal });
					}
					if (modifier.HasFlag(KeyModifiers.Ctrl))
					{
						items.Add(new() { Text = HotKey.modifiers[KeyModifiers.Ctrl], ItemType = KeyboardShortcutItemKind.Default });
						items.Add(new() { Text = "+", ItemType = KeyboardShortcutItemKind.Reveal });
					}
					if (modifier.HasFlag(KeyModifiers.Shift))
					{
						items.Add(new() { Text = HotKey.modifiers[KeyModifiers.Shift], ItemType = KeyboardShortcutItemKind.Default });
						items.Add(new() { Text = "+", ItemType = KeyboardShortcutItemKind.Reveal });
					}
					if (modifier.HasFlag(KeyModifiers.Win))
					{
						items.Add(new() { Text = HotKey.modifiers[KeyModifiers.Win], ItemType = KeyboardShortcutItemKind.Default });
						items.Add(new() { Text = "+", ItemType = KeyboardShortcutItemKind.Reveal });
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
