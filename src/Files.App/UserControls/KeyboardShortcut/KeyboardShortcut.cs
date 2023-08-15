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
			List<KeyboardShortcutItem> items = new();

			foreach (var item in ShortcutText.Split(',').ToList())
			{
				foreach (var item2 in item.Split("+").ToList())
				{
					items.Add(new() { Text = item2 });
					items.Add(new() { Text = "+", IsSurrounded = false });
				}

				if (items.Last().Text == "+")
					items.Remove(items.Last());

				items.Add(new() { Text = ",", IsSurrounded = false });
			}

			if (items.Last().Text == ",")
				items.Remove(items.Last());

			// Set value
			if (GetTemplateChild(KeyboardShortcutItemsRepeater) is ItemsRepeater keyboardShortcutItemsRepeater)
			{
				keyboardShortcutItemsRepeater.ItemsSource = items;
			}
		}
	}
}
