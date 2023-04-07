using Files.App.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Forms = System.Windows.Forms;

namespace Files.App.Commands
{
	public readonly struct HotKey : IEquatable<HotKey>
	{
		private static readonly IImmutableDictionary<KeyModifiers, string> modifiers = new Dictionary<KeyModifiers, string>()
		{
			[KeyModifiers.Menu] = GetKeyString("Menu"),
			[KeyModifiers.Ctrl] = GetKeyString("Control"),
			[KeyModifiers.Shift] = GetKeyString("Shift"),
			[KeyModifiers.Win] = GetKeyString("Windows"),
		}.ToImmutableDictionary();

		private static readonly IImmutableDictionary<Keys, string> keys = new Dictionary<Keys, string>()
		{
			[Keys.Enter] = GetKeyString("Enter"),
			[Keys.Space] = GetKeyString("Space"),
			[Keys.Escape] = GetKeyString("Escape"),
			[Keys.Back] = GetKeyString("Back"),
			[Keys.Tab] = GetKeyString("Tab"),
			[Keys.Insert] = GetKeyString("Insert"),
			[Keys.Delete] = GetKeyString("Delete"),
			[Keys.Left] = GetKeyString("Left"),
			[Keys.Right] = GetKeyString("Right"),
			[Keys.Down] = GetKeyString("Down"),
			[Keys.Up] = GetKeyString("Up"),
			[Keys.Home] = GetKeyString("Home"),
			[Keys.End] = GetKeyString("End"),
			[Keys.PageDown] = GetKeyString("PageDown"),
			[Keys.PageUp] = GetKeyString("PageUp"),
			[Keys.Separator] = GetKeyString("Separator"),
			[Keys.Pause] = GetKeyString("Pause"),
			[Keys.Sleep] = GetKeyString("Sleep"),
			[Keys.Clear] = GetKeyString("Clear"),
			[Keys.Print] = GetKeyString("Print"),
			[Keys.Help] = GetKeyString("Help"),
			[Keys.Mouse4] = GetKeyString("Mouse4"),
			[Keys.Mouse5] = GetKeyString("Mouse5"),
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
			[Keys.Pad0] = GetKeyString("Pad0"),
			[Keys.Pad1] = GetKeyString("Pad1"),
			[Keys.Pad2] = GetKeyString("Pad2"),
			[Keys.Pad3] = GetKeyString("Pad3"),
			[Keys.Pad4] = GetKeyString("Pad4"),
			[Keys.Pad5] = GetKeyString("Pad5"),
			[Keys.Pad6] = GetKeyString("Pad6"),
			[Keys.Pad7] = GetKeyString("Pad7"),
			[Keys.Pad8] = GetKeyString("Pad8"),
			[Keys.Pad9] = GetKeyString("Pad9"),
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
			[Keys.Oem1] = GetKeyCharacter(Forms.Keys.Oem1),
			[Keys.Oem2] = GetKeyCharacter(Forms.Keys.Oem2),
			[Keys.Oem3] = GetKeyCharacter(Forms.Keys.Oem3),
			[Keys.Oem4] = GetKeyCharacter(Forms.Keys.Oem4),
			[Keys.Oem5] = GetKeyCharacter(Forms.Keys.Oem5),
			[Keys.Oem6] = GetKeyCharacter(Forms.Keys.Oem6),
			[Keys.Oem7] = GetKeyCharacter(Forms.Keys.Oem7),
			[Keys.Oem8] = GetKeyCharacter(Forms.Keys.Oem8),
			[Keys.OemPlus] = GetKeyCharacter(Forms.Keys.Oemplus),
			[Keys.OemComma] = GetKeyCharacter(Forms.Keys.Oemcomma),
			[Keys.OemMinus] = GetKeyCharacter(Forms.Keys.OemMinus),
			[Keys.OemPeriod] = GetKeyCharacter(Forms.Keys.OemPeriod),
			[Keys.Oem102] = GetKeyCharacter(Forms.Keys.Oem102),
			[Keys.OemClear] = GetKeyCharacter(Forms.Keys.OemClear),
			[Keys.Application] = GetKeyString("Application"),
			[Keys.Application1] = GetKeyString("Application1"),
			[Keys.Application2] = GetKeyString("Application2"),
			[Keys.Mail] = GetKeyString("Mail"),
			[Keys.GoHome] = GetKeyString("BrowserGoHome"),
			[Keys.GoBack] = GetKeyString("BrowserGoBack"),
			[Keys.GoForward] = GetKeyString("BrowserGoForward"),
			[Keys.Refresh] = GetKeyString("BrowserRefresh"),
			[Keys.BrowserStop] = GetKeyString("BrowserStop"),
			[Keys.Search] = GetKeyString("BrowserSearch"),
			[Keys.Favorites] = GetKeyString("BrowserFavorites"),
			[Keys.PlayPause] = GetKeyString("MediaPlayPause"),
			[Keys.MediaStop] = GetKeyString("MediaStop"),
			[Keys.PreviousTrack] = GetKeyString("MediaPreviousTrack"),
			[Keys.NextTrack] = GetKeyString("MediaNextTrack"),
			[Keys.MediaSelect] = GetKeyString("MediaSelect"),
			[Keys.Mute] = GetKeyString("MediaMute"),
			[Keys.VolumeDown] = GetKeyString("MediaVolumeDown"),
			[Keys.VolumeUp] = GetKeyString("MediaVolumeUp"),
		}.ToImmutableDictionary();

		public static HotKey None { get; } = new(Keys.None, KeyModifiers.None);

		public bool IsNone => Key is Keys.None && Modifier is KeyModifiers.None;

		public bool IsVisible { get; }

		public Keys Key { get; }
		public KeyModifiers Modifier { get; }

		public string Code
		{
			get
			{
				return (Key, Modifier) switch
				{
					(Keys.None, KeyModifiers.None) => string.Empty,
					(Keys.None, _) => $"{GetVisibleCode(IsVisible)}{GetModifierCode(Modifier)}",
					(_, KeyModifiers.None) => $"{GetVisibleCode(IsVisible)}{Key}",
					_ => $"{GetVisibleCode(IsVisible)}{GetModifierCode(Modifier)}+{Key}",
				};

				static string GetVisibleCode(bool isVisible) => isVisible ? string.Empty : "!";

				static string GetModifierCode(KeyModifiers modifiers)
				{
					StringBuilder builder = new();
					if (modifiers.HasFlag(KeyModifiers.Menu))
						builder.Append($"+{KeyModifiers.Menu}");
					if (modifiers.HasFlag(KeyModifiers.Ctrl))
						builder.Append($"+{KeyModifiers.Ctrl}");
					if (modifiers.HasFlag(KeyModifiers.Shift))
						builder.Append($"+{KeyModifiers.Shift}");
					if (modifiers.HasFlag(KeyModifiers.Win))
						builder.Append($"+{KeyModifiers.Win}");
					builder.Remove(0, 1);
					return builder.ToString();
				}
			}
		}

		public string Label
		{
			get
			{
				if (IsVisible)
					return string.Empty;

				return (Key, Modifier) switch
				{
					(Keys.None, KeyModifiers.None) => string.Empty,
					(Keys.None, _) => GetModifierCode(Modifier),
					(_, KeyModifiers.None) => keys[Key],
					_ => $"{GetModifierCode(Modifier)}+{keys[Key]}",
				};

				static string GetModifierCode(KeyModifiers modifier)
				{
					StringBuilder builder = new();
					if (modifier.HasFlag(KeyModifiers.Menu))
						builder.Append($"+{modifiers[KeyModifiers.Menu]}");
					if (modifier.HasFlag(KeyModifiers.Ctrl))
						builder.Append($"+{modifiers[KeyModifiers.Ctrl]}");
					if (modifier.HasFlag(KeyModifiers.Shift))
						builder.Append($"+{modifiers[KeyModifiers.Shift]}");
					if (modifier.HasFlag(KeyModifiers.Win))
						builder.Append($"+{modifiers[KeyModifiers.Win]}");
					builder.Remove(0, 1);
					return builder.ToString();
				}
			}
		}

		public HotKey(Keys key, bool isVisible = true) : this(key, KeyModifiers.None, isVisible) { }
		public HotKey(Keys key, KeyModifiers modifier, bool isVisible = true)
		{
			if (!Enum.IsDefined(key) || !Enum.IsDefined(modifier))
				return;

			IsVisible = isVisible;
			Key = key;
			Modifier = modifier;
		}

		public void Deconstruct(out Keys key, out KeyModifiers modifier)
			=> (key, modifier) = (Key, Modifier);
		public void Deconstruct(out Keys key, out KeyModifiers modifier, out bool isVisible)
			=> (key, modifier, isVisible) = (Key, Modifier, IsVisible);

		public static HotKey Parse(string code)
		{
			var key = Keys.None;
			var modifier = KeyModifiers.None;

			var parts = code.Split('+').Select(part => part.Trim());
			foreach (var part in parts)
			{
				if (Enum.TryParse(part, out Keys partKey))
					key = partKey;
				if (Enum.TryParse(part, out KeyModifiers partModifier))
					modifier |= partModifier;
			}
			return new(key, modifier);
		}

		public HotKeyCollection AsCollection() => new(this);

		public static implicit operator string(HotKey hotKey) => hotKey.Label;

		public static bool operator ==(HotKey a, HotKey b) => a.Equals(b);
		public static bool operator !=(HotKey a, HotKey b) => !a.Equals(b);

		public override string ToString() => Label;

		public override int GetHashCode() => (Key, Modifier).GetHashCode();
		public override bool Equals(object? other) => other is HotKey hotKey && Equals(hotKey);
		public bool Equals(HotKey other) => (other.Key, other.Modifier).Equals((Key, Modifier));

		private static string GetKeyString(string key) => $"Key/{key}".GetLocalizedResource();

		private static string GetKeyCharacter(Forms.Keys key)
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
