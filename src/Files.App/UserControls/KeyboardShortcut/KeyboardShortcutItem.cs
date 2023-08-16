// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

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

		private void OnSizeChanged()
		{
		}

		private void OnTextChanged()
		{
		}
	}
}
