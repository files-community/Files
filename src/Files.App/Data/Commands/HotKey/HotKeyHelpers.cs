// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Input;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.Data.Commands
{
	internal static class HotKeyHelpers
	{
		public static KeyModifiers GetCurrentKeyModifiers()
		{
			var modifiers = VirtualKeyModifiers.None;

			if (IsPressed(VirtualKey.Menu))
				modifiers |= VirtualKeyModifiers.Menu;
			if (IsPressed(VirtualKey.Control))
				modifiers |= VirtualKeyModifiers.Control;
			if (IsPressed(VirtualKey.Shift))
				modifiers |= VirtualKeyModifiers.Shift;
			if (IsPressed(VirtualKey.LeftWindows) || IsPressed(VirtualKey.RightWindows))
				modifiers |= VirtualKeyModifiers.Windows;

			return (KeyModifiers)modifiers;

			static bool IsPressed(VirtualKey key)
				=> InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);
		}
	}
}
