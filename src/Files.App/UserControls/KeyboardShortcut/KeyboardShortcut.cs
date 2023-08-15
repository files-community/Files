// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.UserControls.KeyboardShortcut
{
	public sealed partial class KeyboardShortcut : Control
	{
		internal const string KeyboardShortcutItemsRepeater = "PART_KeyboardShortcutItemsRepeater";

		public KeyboardShortcut()
		{
			DefaultStyleKey = typeof(KeyboardShortcut);
		}

		private void OnShortcutTextChanged()
		{
			// Generate items
			var splitItems = Regex.Split(ShortcutText, @"(?<=[,+])");
			List<KeyboardShortcutItem> items = new();

			foreach (var item in splitItems)
			{
				if (item == "+" || item == ",")
				{
					items.Add(new() { Text = item, IsSurrounded = false });
				}
				else
				{
					items.Add(new() { Text = item });
				}
			}

			// Set value
			if (GetTemplateChild(KeyboardShortcutItemsRepeater) is ItemsRepeater keyboardShortcutItemsRepeater)
			{
				keyboardShortcutItemsRepeater.ItemsSource = items;
			}
		}
	}
}
