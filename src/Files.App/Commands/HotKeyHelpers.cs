using System.Collections.Generic;
using System.Collections.Immutable;
using Windows.System;

namespace Files.App.Commands
{
	internal static class HotKeyHelpers
	{
		private static readonly ImmutableArray<VirtualKey> globalKeys = new List<VirtualKey>
		{
			VirtualKey.GoHome,
			VirtualKey.GoBack,
			VirtualKey.GoForward,
			VirtualKey.Application,
			VirtualKey.Favorites,
			VirtualKey.Search,
			VirtualKey.Refresh,
			VirtualKey.Pause,
			VirtualKey.Sleep,
			VirtualKey.Print,
			VirtualKey.F1,
			VirtualKey.F2,
			VirtualKey.F3,
			VirtualKey.F4,
			VirtualKey.F5,
			VirtualKey.F6,
			VirtualKey.F7,
			VirtualKey.F8,
			VirtualKey.F9,
			VirtualKey.F10,
			VirtualKey.F11,
			VirtualKey.F12,
			VirtualKey.F13,
			VirtualKey.F14,
			VirtualKey.F15,
			VirtualKey.F16,
			VirtualKey.F17,
			VirtualKey.F18,
			VirtualKey.F19,
			VirtualKey.F20,
			VirtualKey.F21,
			VirtualKey.F22,
			VirtualKey.F23,
			VirtualKey.F24,
		}.ToImmutableArray();

		private static readonly ImmutableArray<HotKey> textBoxHotKeys = new List<HotKey>
		{
			new HotKey(VirtualKey.X, VirtualKeyModifiers.Control), // Cut
			new HotKey(VirtualKey.C, VirtualKeyModifiers.Control), // Copy
			new HotKey(VirtualKey.V, VirtualKeyModifiers.Control), // Paste
			new HotKey(VirtualKey.A, VirtualKeyModifiers.Control), // Select all
			new HotKey(VirtualKey.Z, VirtualKeyModifiers.Control), // Cancel
		}.ToImmutableArray();

		public static bool IsGlobalKey(this VirtualKey key) => globalKeys.Contains(key);
		public static bool IsTextBoxHotKey(this HotKey hotKey) => textBoxHotKeys.Contains(hotKey);
	}
}
