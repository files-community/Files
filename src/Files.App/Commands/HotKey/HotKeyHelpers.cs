using Files.App.Extensions;
using Microsoft.UI.Input;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text;
using Windows.System;
using Windows.UI.Core;
using Forms = System.Windows.Forms;

namespace Files.App.Commands
{
	internal static class HotKeyHelpers
	{
		private static readonly IImmutableDictionary<KeyModifiers, string> modifiers = new Dictionary<KeyModifiers, string>()
		{
			[KeyModifiers.Menu] = "Menu".GetString(),
			[KeyModifiers.Ctrl] = "Control".GetString(),
			[KeyModifiers.Shift] = "Shift".GetString(),
			[KeyModifiers.Win] = "Windows".GetString(),
		}.ToImmutableDictionary();

		private static readonly IImmutableDictionary<Keys, string> keys = new Dictionary<Keys, string>()
		{
			[Keys.Enter] = "Enter".GetString(),
			[Keys.Space] = "Space".GetString(),
			[Keys.Escape] = "Escape".GetString(),
			[Keys.Back] = "Back".GetString(),
			[Keys.Tab] = "Tab".GetString(),
			[Keys.Insert] = "Insert".GetString(),
			[Keys.Delete] = "Delete".GetString(),
			[Keys.Left] = "Left".GetString(),
			[Keys.Right] = "Right".GetString(),
			[Keys.Down] = "Down".GetString(),
			[Keys.Up] = "Up".GetString(),
			[Keys.Home] = "Home".GetString(),
			[Keys.End] = "End".GetString(),
			[Keys.PageDown] = "PageDown".GetString(),
			[Keys.PageUp] = "PageUp".GetString(),
			[Keys.Separator] = "Separator".GetString(),
			[Keys.Pause] = "Pause".GetString(),
			[Keys.Sleep] = "Sleep".GetString(),
			[Keys.Clear] = "Clear".GetString(),
			[Keys.Print] = "Print".GetString(),
			[Keys.Help] = "Help".GetString(),
			[Keys.Mouse4] = "Mouse4".GetString(),
			[Keys.Mouse5] = "Mouse5".GetString(),
			[Keys.F1] = "F1",
			[Keys.F2] = "F2",
			[Keys.F3] = "F3",
			[Keys.F4] = "F4",
			[Keys.F5] = "F5",
			[Keys.F6] = "F6",
			[Keys.F7] = "F7",
			[Keys.F8] = "F8",
			[Keys.F9] = "F9",
			[Keys.F10] = "F10",
			[Keys.F11] = "F11",
			[Keys.F12] = "F12",
			[Keys.F13] = "F13",
			[Keys.F14] = "F14",
			[Keys.F15] = "F15",
			[Keys.F16] = "F16",
			[Keys.F17] = "F17",
			[Keys.F18] = "F18",
			[Keys.F19] = "F19",
			[Keys.F20] = "F20",
			[Keys.F21] = "F21",
			[Keys.F22] = "F22",
			[Keys.F23] = "F23",
			[Keys.F24] = "F24",
			[Keys.Number0] = "0",
			[Keys.Number1] = "1",
			[Keys.Number2] = "2",
			[Keys.Number3] = "3",
			[Keys.Number4] = "4",
			[Keys.Number5] = "5",
			[Keys.Number6] = "6",
			[Keys.Number7] = "7",
			[Keys.Number8] = "8",
			[Keys.Number9] = "9",
			[Keys.Pad0] = "Pad0".GetString(),
			[Keys.Pad1] = "Pad1".GetString(),
			[Keys.Pad2] = "Pad2".GetString(),
			[Keys.Pad3] = "Pad3".GetString(),
			[Keys.Pad4] = "Pad4".GetString(),
			[Keys.Pad5] = "Pad5".GetString(),
			[Keys.Pad6] = "Pad6".GetString(),
			[Keys.Pad7] = "Pad7".GetString(),
			[Keys.Pad8] = "Pad8".GetString(),
			[Keys.Pad9] = "Pad9".GetString(),
			[Keys.A] = "A",
			[Keys.B] = "B",
			[Keys.C] = "C",
			[Keys.D] = "D",
			[Keys.E] = "E",
			[Keys.F] = "F",
			[Keys.G] = "G",
			[Keys.H] = "H",
			[Keys.I] = "I",
			[Keys.J] = "J",
			[Keys.K] = "K",
			[Keys.L] = "L",
			[Keys.M] = "M",
			[Keys.N] = "N",
			[Keys.O] = "O",
			[Keys.P] = "P",
			[Keys.Q] = "Q",
			[Keys.R] = "R",
			[Keys.S] = "S",
			[Keys.T] = "T",
			[Keys.U] = "U",
			[Keys.V] = "V",
			[Keys.W] = "W",
			[Keys.X] = "X",
			[Keys.Y] = "Y",
			[Keys.Z] = "Z",
			[Keys.Add] = "+",
			[Keys.Subtract] = "-",
			[Keys.Multiply] = "*",
			[Keys.Divide] = "/",
			[Keys.Oem1] = GetCharacter(Forms.Keys.Oem1),
			[Keys.Oem2] = GetCharacter(Forms.Keys.Oem2),
			[Keys.Oem3] = GetCharacter(Forms.Keys.Oem3),
			[Keys.Oem4] = GetCharacter(Forms.Keys.Oem4),
			[Keys.Oem5] = GetCharacter(Forms.Keys.Oem5),
			[Keys.Oem6] = GetCharacter(Forms.Keys.Oem6),
			[Keys.Oem7] = GetCharacter(Forms.Keys.Oem7),
			[Keys.Oem8] = GetCharacter(Forms.Keys.Oem8),
			[Keys.OemPlus] = GetCharacter(Forms.Keys.Oemplus),
			[Keys.OemComma] = GetCharacter(Forms.Keys.Oemcomma),
			[Keys.OemMinus] = GetCharacter(Forms.Keys.OemMinus),
			[Keys.OemPeriod] = GetCharacter(Forms.Keys.OemPeriod),
			[Keys.Oem102] = GetCharacter(Forms.Keys.Oem102),
			[Keys.OemClear] = GetCharacter(Forms.Keys.OemClear),
			[Keys.Application] = "Application".GetString(),
			[Keys.Application1] = "Application1".GetString(),
			[Keys.Application2] = "Application2".GetString(),
			[Keys.Mail] = "Mail".GetString(),
			[Keys.GoHome] = "BrowserGoHome".GetString(),
			[Keys.GoBack] = "BrowserGoBack".GetString(),
			[Keys.GoForward] = "BrowserGoForward".GetString(),
			[Keys.Refresh] = "BrowserRefresh".GetString(),
			[Keys.BrowserStop] = "BrowserStop".GetString(),
			[Keys.Search] = "BrowserSearch".GetString(),
			[Keys.Favorites] = "BrowserFavorites".GetString(),
			[Keys.PlayPause] = "MediaPlayPause".GetString(),
			[Keys.MediaStop] = "MediaStop".GetString(),
			[Keys.PreviousTrack] = "MediaPreviousTrack".GetString(),
			[Keys.NextTrack] = "MediaNextTrack".GetString(),
			[Keys.MediaSelect] = "MediaSelect".GetString(),
			[Keys.Mute] = "MediaMute".GetString(),
			[Keys.VolumeDown] = "MediaVolumeDown".GetString(),
			[Keys.VolumeUp] = "MediaVolumeUp".GetString(),
		}.ToImmutableDictionary();

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
			{
				modifiers |= VirtualKeyModifiers.Menu;
			}
			if (IsPressed(VirtualKey.Control))
			{
				modifiers |= VirtualKeyModifiers.Control;
			}
			if (IsPressed(VirtualKey.Shift))
			{
				modifiers |= VirtualKeyModifiers.Shift;
			}
			if (IsPressed(VirtualKey.LeftWindows) || IsPressed(VirtualKey.RightWindows))
			{
				modifiers |= VirtualKeyModifiers.Windows;
			}

			return (KeyModifiers)modifiers;

			static bool IsPressed(VirtualKey key)
				=> InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);
		}

		public static string ToString(HotKey hotKey)
		{
			if (hotKey.Key is Keys.None)
				return string.Empty;
			if (hotKey.Modifiers is KeyModifiers.None)
				return keys[hotKey.Key];

			StringBuilder modifierBuilder = new();
			if (hotKey.Modifiers.HasFlag(KeyModifiers.Menu))
				modifierBuilder.Append($"{modifiers[KeyModifiers.Menu]}+");
			if (hotKey.Modifiers.HasFlag(KeyModifiers.Ctrl))
				modifierBuilder.Append($"{modifiers[KeyModifiers.Ctrl]}+");
			if (hotKey.Modifiers.HasFlag(KeyModifiers.Shift))
				modifierBuilder.Append($"{modifiers[KeyModifiers.Shift]}+");
			if (hotKey.Modifiers.HasFlag(KeyModifiers.Win))
				modifierBuilder.Append($"{modifiers[KeyModifiers.Win]}+");

			return $"{modifierBuilder}{keys[hotKey.Key]}";
		}

		private static string GetString(this string key) => $"Key/{key}".GetLocalizedResource();

		private static string GetCharacter(Forms.Keys key)
		{
			var buffer = new StringBuilder(256);
			var state = new byte[256];
			_ = ToUnicode((uint)key, 0, state, buffer, 256, 0);
			return buffer.ToString();
		}

		[DllImport("user32.dll")]
		private static extern int ToUnicode
		(
			uint virtualKeyCode,
			uint scanCode,
			byte[] keyboardState,
			[Out, MarshalAs(UnmanagedType.LPWStr, SizeConst = 64)] StringBuilder receivingBuffer,
			int bufferSize,
			uint flags
		);


	}
}
