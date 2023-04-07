using Microsoft.UI.Input;
using System.Collections.Generic;
using System.Collections.Immutable;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.Commands
{
	internal static class HotKeyHelpers
	{
		private static readonly ImmutableArray<Keys> globalKeys = new List<Keys>
		{
			Keys.GoHome,
			Keys.GoBack,
			Keys.GoForward,
			Keys.Application,
			Keys.Favorites,
			Keys.Search,
			Keys.Refresh,
			Keys.Pause,
			Keys.Sleep,
			Keys.Print,
			Keys.F1,
			Keys.F2,
			Keys.F3,
			Keys.F4,
			Keys.F5,
			Keys.F6,
			Keys.F7,
			Keys.F8,
			Keys.F9,
			Keys.F10,
			Keys.F11,
			Keys.F12,
			Keys.F13,
			Keys.F14,
			Keys.F15,
			Keys.F16,
			Keys.F17,
			Keys.F18,
			Keys.F19,
			Keys.F20,
			Keys.F21,
			Keys.F22,
			Keys.F23,
			Keys.F24,
		}.ToImmutableArray();

		private static readonly ImmutableArray<HotKey> textBoxHotKeys = new List<HotKey>
		{
			new HotKey(Keys.X, KeyModifiers.Ctrl), // Cut
			new HotKey(Keys.C, KeyModifiers.Ctrl), // Copy
			new HotKey(Keys.V, KeyModifiers.Ctrl), // Paste
			new HotKey(Keys.A, KeyModifiers.Ctrl), // Select all
			new HotKey(Keys.Z, KeyModifiers.Ctrl), // Cancel
		}.ToImmutableArray();

		public static bool IsGlobalKey(this Keys key) => globalKeys.Contains(key);
		public static bool IsTextBoxHotKey(this HotKey hotKey) => textBoxHotKeys.Contains(hotKey);

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
