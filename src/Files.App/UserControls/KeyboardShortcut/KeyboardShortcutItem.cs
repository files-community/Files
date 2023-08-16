// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.KeyboardShortcut
{
	public sealed partial class KeyboardShortcutItem : Control
	{
		internal const string MainTextTextBlock = "PART_MainTextTextBlock";

		public KeyboardShortcutItem()
		{
			DefaultStyleKey = typeof(KeyboardShortcutItem);
		}

		private void OnItemTypeChanged()
		{
		}

		private void OnTextChanged()
		{
			// Set value
			if (GetTemplateChild(MainTextTextBlock) is TextBlock mainTextTextBlock)
			{
				mainTextTextBlock.Text = Text;
			}
		}
	}
}
