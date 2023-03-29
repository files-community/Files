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
		private static readonly IImmutableDictionary<VirtualKeyModifiers, string> modifiers = new Dictionary<VirtualKeyModifiers, string>()
		{
			[VirtualKeyModifiers.Menu] = "Menu".GetString(),
			[VirtualKeyModifiers.Control] = "Control".GetString(),
			[VirtualKeyModifiers.Shift] = "Shift".GetString(),
			[VirtualKeyModifiers.Windows] = "Windows".GetString(),
		}.ToImmutableDictionary();

		private static readonly IImmutableDictionary<VirtualKey, string> keys = new Dictionary<VirtualKey, string>()
		{
			[VirtualKey.Enter] = "Enter".GetString(),
			[VirtualKey.Space] = "Space".GetString(),
			[VirtualKey.Escape] = "Escape".GetString(),
			[VirtualKey.Back] = "Back".GetString(),
			[VirtualKey.Tab] = "Tab".GetString(),
			[VirtualKey.Insert] = "Insert".GetString(),
			[VirtualKey.Delete] = "Delete".GetString(),
			[VirtualKey.Left] = "Left".GetString(),
			[VirtualKey.Right] = "Right".GetString(),
			[VirtualKey.Down] = "Down".GetString(),
			[VirtualKey.Up] = "Up".GetString(),
			[VirtualKey.Home] = "Home".GetString(),
			[VirtualKey.End] = "End".GetString(),
			[VirtualKey.PageDown] = "PageDown".GetString(),
			[VirtualKey.PageUp] = "PageUp".GetString(),
			[VirtualKey.Separator] = "Separator".GetString(),
			[VirtualKey.Pause] = "Pause".GetString(),
			[VirtualKey.Sleep] = "Sleep".GetString(),
			[VirtualKey.Clear] = "Clear".GetString(),
			[VirtualKey.Print] = "Print".GetString(),
			[VirtualKey.Help] = "Help".GetString(),
			[VirtualKey.XButton1] = "Mouse4".GetString(),
			[VirtualKey.XButton2] = "Mouse5".GetString(),
			[VirtualKey.F1] = "F1",
			[VirtualKey.F2] = "F2",
			[VirtualKey.F3] = "F3",
			[VirtualKey.F4] = "F4",
			[VirtualKey.F5] = "F5",
			[VirtualKey.F6] = "F6",
			[VirtualKey.F7] = "F7",
			[VirtualKey.F8] = "F8",
			[VirtualKey.F9] = "F9",
			[VirtualKey.F10] = "F10",
			[VirtualKey.F11] = "F11",
			[VirtualKey.F12] = "F12",
			[VirtualKey.F13] = "F13",
			[VirtualKey.F14] = "F14",
			[VirtualKey.F15] = "F15",
			[VirtualKey.F16] = "F16",
			[VirtualKey.F17] = "F17",
			[VirtualKey.F18] = "F18",
			[VirtualKey.F19] = "F19",
			[VirtualKey.F20] = "F20",
			[VirtualKey.F21] = "F21",
			[VirtualKey.F22] = "F22",
			[VirtualKey.F23] = "F23",
			[VirtualKey.F24] = "F24",
			[VirtualKey.Number0] = "0",
			[VirtualKey.Number1] = "1",
			[VirtualKey.Number2] = "2",
			[VirtualKey.Number3] = "3",
			[VirtualKey.Number4] = "4",
			[VirtualKey.Number5] = "5",
			[VirtualKey.Number6] = "6",
			[VirtualKey.Number7] = "7",
			[VirtualKey.Number8] = "8",
			[VirtualKey.Number9] = "9",
			[VirtualKey.NumberPad0] = "Pad0".GetString(),
			[VirtualKey.NumberPad1] = "Pad1".GetString(),
			[VirtualKey.NumberPad2] = "Pad2".GetString(),
			[VirtualKey.NumberPad3] = "Pad3".GetString(),
			[VirtualKey.NumberPad4] = "Pad4".GetString(),
			[VirtualKey.NumberPad5] = "Pad5".GetString(),
			[VirtualKey.NumberPad6] = "Pad6".GetString(),
			[VirtualKey.NumberPad7] = "Pad7".GetString(),
			[VirtualKey.NumberPad8] = "Pad8".GetString(),
			[VirtualKey.NumberPad9] = "Pad9".GetString(),
			[VirtualKey.A] = "A",
			[VirtualKey.B] = "B",
			[VirtualKey.C] = "C",
			[VirtualKey.D] = "D",
			[VirtualKey.E] = "E",
			[VirtualKey.F] = "F",
			[VirtualKey.G] = "G",
			[VirtualKey.H] = "H",
			[VirtualKey.I] = "I",
			[VirtualKey.J] = "J",
			[VirtualKey.K] = "K",
			[VirtualKey.L] = "L",
			[VirtualKey.M] = "M",
			[VirtualKey.N] = "N",
			[VirtualKey.O] = "O",
			[VirtualKey.P] = "P",
			[VirtualKey.Q] = "Q",
			[VirtualKey.R] = "R",
			[VirtualKey.S] = "S",
			[VirtualKey.T] = "T",
			[VirtualKey.U] = "U",
			[VirtualKey.V] = "V",
			[VirtualKey.W] = "W",
			[VirtualKey.X] = "X",
			[VirtualKey.Y] = "Y",
			[VirtualKey.Z] = "Z",
			[VirtualKey.Add] = "+",
			[VirtualKey.Subtract] = "-",
			[VirtualKey.Multiply] = "*",
			[VirtualKey.Divide] = "/",
			[(VirtualKey)186] = GetCharacter(Forms.Keys.Oem1),
			[(VirtualKey)187] = GetCharacter(Forms.Keys.Oemplus),
			[(VirtualKey)188] = GetCharacter(Forms.Keys.Oemcomma),
			[(VirtualKey)189] = GetCharacter(Forms.Keys.OemMinus),
			[(VirtualKey)190] = GetCharacter(Forms.Keys.OemPeriod),
			[(VirtualKey)191] = GetCharacter(Forms.Keys.Oem2),
			[(VirtualKey)192] = GetCharacter(Forms.Keys.Oem3),
			[(VirtualKey)219] = GetCharacter(Forms.Keys.Oem4),
			[(VirtualKey)220] = GetCharacter(Forms.Keys.Oem5),
			[(VirtualKey)221] = GetCharacter(Forms.Keys.Oem6),
			[(VirtualKey)222] = GetCharacter(Forms.Keys.Oem7),
			[(VirtualKey)223] = GetCharacter(Forms.Keys.Oem8),
			[(VirtualKey)226] = GetCharacter(Forms.Keys.Oem102),
			[(VirtualKey)254] = GetCharacter(Forms.Keys.OemClear),
			[VirtualKey.Application] = "Application".GetString(),
			[(VirtualKey)182] = "Application1".GetString(),
			[(VirtualKey)183] = "Application2".GetString(),
			[(VirtualKey)180] = "Mail".GetString(),
			[VirtualKey.GoHome] = "BrowserGoHome".GetString(),
			[VirtualKey.GoBack] = "BrowserGoBack".GetString(),
			[VirtualKey.GoForward] = "BrowserGoForward".GetString(),
			[VirtualKey.Refresh] = "BrowserRefresh".GetString(),
			[VirtualKey.Stop] = "BrowserStop".GetString(),
			[VirtualKey.Search] = "BrowserSearch".GetString(),
			[VirtualKey.Favorites] = "BrowserFavorites".GetString(),
			[(VirtualKey)179] = "MediaPlayPause".GetString(),
			[(VirtualKey)178] = "MediaStop".GetString(),
			[(VirtualKey)177] = "MediaPreviousTrack".GetString(),
			[(VirtualKey)176] = "MediaNextTrack".GetString(),
			[(VirtualKey)181] = "MediaSelect".GetString(),
			[(VirtualKey)173] = "MediaMute".GetString(),
			[(VirtualKey)174] = "MediaVolumeDown".GetString(),
			[(VirtualKey)175] = "MediaVolumeUp".GetString(),
		}.ToImmutableDictionary();

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

		public static bool IsValid(this VirtualKeyModifiers modifier) => modifiers.ContainsKey(modifier);
		public static bool IsValid(this VirtualKey key) => keys.ContainsKey(key);

		public static bool IsGlobalKey(this VirtualKey key) => globalKeys.Contains(key);
		public static bool IsTextBoxHotKey(this HotKey hotKey) => textBoxHotKeys.Contains(hotKey);

		public static VirtualKeyModifiers GetCurrentKeyModifiers()
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

			return modifiers;

			static bool IsPressed(VirtualKey key)
				=> InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);
		}

		public static string ToString(HotKey hotKey)
		{
			return hotKey.Modifiers is VirtualKeyModifiers.None
				? ToString(hotKey.Key)
				: $"{ToString(hotKey.Modifiers)}+{ToString(hotKey.Key)}";
		}
		public static string ToString(VirtualKeyModifiers modifier)
		{
			StringBuilder builder = new();
			if (modifier.HasFlag(VirtualKeyModifiers.Menu))
				builder.Append($"+{modifiers[VirtualKeyModifiers.Menu]}");
			if (modifier.HasFlag(VirtualKeyModifiers.Control))
				builder.Append($"+{modifiers[VirtualKeyModifiers.Control]}");
			if (modifier.HasFlag(VirtualKeyModifiers.Shift))
				builder.Append($"+{modifiers[VirtualKeyModifiers.Shift]}");
			if (modifier.HasFlag(VirtualKeyModifiers.Windows))
				builder.Append($"+{modifiers[VirtualKeyModifiers.Windows]}");
			if (modifier is not VirtualKeyModifiers.None)
				builder.Remove(0, 1);
			return builder.ToString();
		}
		public static string ToString(VirtualKey key)
		{
			return keys.TryGetValue(key, out string? label) ? label : string.Empty;
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
