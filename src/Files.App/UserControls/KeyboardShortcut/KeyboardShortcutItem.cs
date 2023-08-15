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
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.UserControls.KeyboardShortcut
{
	public sealed partial class KeyboardShortcutItem : Control
	{
		internal const string TextSurroundingBorder = "PART_TextSurroundingBorder";
		internal const string ShortcutText = "PART_ShortcutText";

		public KeyboardShortcutItem()
		{
			DefaultStyleKey = typeof(KeyboardShortcutItem);
		}

		private void OnIsSurroundedChanged()
		{
		}

		private void OnTextChanged()
		{
		}
	}
}
