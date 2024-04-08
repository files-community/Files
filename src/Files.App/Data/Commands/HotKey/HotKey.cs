// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Frozen;
using System.Runtime.InteropServices;
using System.Text;
using Forms = System.Windows.Forms;

namespace Files.App.Data.Commands
{
	/// <summary>
	/// Represents hot key.
	/// </summary>
	[DebuggerDisplay("{LocalizedLabel}")]
	public readonly struct HotKey : IEquatable<HotKey>
	{
		public static FrozenDictionary<KeyModifiers, string> LocalizedModifiers { get; } = new Dictionary<KeyModifiers, string>()
		{
			[KeyModifiers.Alt] = GetLocalizedKey("Menu"),
			[KeyModifiers.Ctrl] = GetLocalizedKey("Control"),
			[KeyModifiers.Shift] = GetLocalizedKey("Shift"),
			[KeyModifiers.Win] = GetLocalizedKey("Windows"),
		}.ToFrozenDictionary();

		public static FrozenDictionary<Keys, string> LocalizedKeys { get; } = new Dictionary<Keys, string>()
		{
			[Keys.Enter] = GetLocalizedKey("Enter"),
			[Keys.Space] = GetLocalizedKey("Space"),
			[Keys.Escape] = GetLocalizedKey("Escape"),
			[Keys.Back] = GetLocalizedKey("Back"),
			[Keys.Tab] = GetLocalizedKey("Tab"),
			[Keys.Insert] = GetLocalizedKey("Insert"),
			[Keys.Delete] = GetLocalizedKey("Delete"),
			[Keys.Left] = GetLocalizedKey("Left"),
			[Keys.Right] = GetLocalizedKey("Right"),
			[Keys.Down] = GetLocalizedKey("Down"),
			[Keys.Up] = GetLocalizedKey("Up"),
			[Keys.Home] = GetLocalizedKey("Home"),
			[Keys.End] = GetLocalizedKey("End"),
			[Keys.PageDown] = GetLocalizedKey("PageDown"),
			[Keys.PageUp] = GetLocalizedKey("PageUp"),
			[Keys.Separator] = GetLocalizedKey("Separator"),
			[Keys.Pause] = GetLocalizedKey("Pause"),
			[Keys.Sleep] = GetLocalizedKey("Sleep"),
			[Keys.Clear] = GetLocalizedKey("Clear"),
			[Keys.Print] = GetLocalizedKey("Print"),
			[Keys.Help] = GetLocalizedKey("Help"),
			[Keys.Mouse4] = GetLocalizedKey("Mouse4"),
			[Keys.Mouse5] = GetLocalizedKey("Mouse5"),
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
			[Keys.Pad0] = GetLocalizedKey("Pad0"),
			[Keys.Pad1] = GetLocalizedKey("Pad1"),
			[Keys.Pad2] = GetLocalizedKey("Pad2"),
			[Keys.Pad3] = GetLocalizedKey("Pad3"),
			[Keys.Pad4] = GetLocalizedKey("Pad4"),
			[Keys.Pad5] = GetLocalizedKey("Pad5"),
			[Keys.Pad6] = GetLocalizedKey("Pad6"),
			[Keys.Pad7] = GetLocalizedKey("Pad7"),
			[Keys.Pad8] = GetLocalizedKey("Pad8"),
			[Keys.Pad9] = GetLocalizedKey("Pad9"),
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
			[Keys.Application] = GetLocalizedKey("Application"),
			[Keys.Application1] = GetLocalizedKey("Application1"),
			[Keys.Application2] = GetLocalizedKey("Application2"),
			[Keys.Mail] = GetLocalizedKey("Mail"),
			[Keys.GoHome] = GetLocalizedKey("BrowserGoHome"),
			[Keys.GoBack] = GetLocalizedKey("BrowserGoBack"),
			[Keys.GoForward] = GetLocalizedKey("BrowserGoForward"),
			[Keys.Refresh] = GetLocalizedKey("BrowserRefresh"),
			[Keys.BrowserStop] = GetLocalizedKey("BrowserStop"),
			[Keys.Search] = GetLocalizedKey("BrowserSearch"),
			[Keys.Favorites] = GetLocalizedKey("BrowserFavorites"),
			[Keys.PlayPause] = GetLocalizedKey("MediaPlayPause"),
			[Keys.MediaStop] = GetLocalizedKey("MediaStop"),
			[Keys.PreviousTrack] = GetLocalizedKey("MediaPreviousTrack"),
			[Keys.NextTrack] = GetLocalizedKey("MediaNextTrack"),
			[Keys.MediaSelect] = GetLocalizedKey("MediaSelect"),
			[Keys.Mute] = GetLocalizedKey("MediaMute"),
			[Keys.VolumeDown] = GetLocalizedKey("MediaVolumeDown"),
			[Keys.VolumeUp] = GetLocalizedKey("MediaVolumeUp"),
		}.ToFrozenDictionary();

		/// <summary>
		/// Gets the none value.
		/// </summary>
		public static HotKey None { get; } = new(Keys.None, KeyModifiers.None);

		/// <summary>
		/// Gets the value that indicates whether the hotkey is none.
		/// </summary>
		public bool IsNone => Key is Keys.None && Modifier is KeyModifiers.None;

		/// <summary>
		/// Gets the value that indicates whether the key should be visible.
		/// </summary>
		public bool IsVisible { get; init; }

		/// <summary>
		/// Gets the key.
		/// </summary>
		public Keys Key { get; }

		/// <summary>
		/// Gets the modifier.
		/// </summary>
		public KeyModifiers Modifier { get; }

		/// <summary>
		/// Gets the raw label of the hotkey.
		/// </summary>
		/// <remarks>
		/// For example, this is "Ctrl+A" and "Ctrl+Menu+C"
		/// </remarks>
		public string RawLabel
		{
			get
			{
				return (Key, Modifier) switch
				{
					(Keys.None, KeyModifiers.None) => string.Empty,
					(Keys.None, _) => $"{GetModifierCode(Modifier)}",
					(_, KeyModifiers.None) => $"{Key}",
					_ => $"{GetModifierCode(Modifier)}+{Key}",
				};

				static string GetModifierCode(KeyModifiers modifiers)
				{
					StringBuilder builder = new();
					if (modifiers.HasFlag(KeyModifiers.Alt))
						builder.Append($"+{KeyModifiers.Alt}");
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

		/// <summary>
		/// Gets the localized label of the hotkey to shown in the UI.
		/// </summary>
		/// <remarks>
		/// For example, this is "Ctrl+A" and "Ctrl+Alt+C"
		/// </remarks>
		public string LocalizedLabel
		{
			get
			{
				return (Key, Modifier) switch
				{
					(Keys.None, KeyModifiers.None) => string.Empty,
					(Keys.None, _) => GetModifierCode(Modifier),
					(_, KeyModifiers.None) => LocalizedKeys[Key],
					_ => $"{GetModifierCode(Modifier)}+{LocalizedKeys[Key]}",
				};

				static string GetModifierCode(KeyModifiers modifier)
				{
					StringBuilder builder = new();
					if (modifier.HasFlag(KeyModifiers.Alt))
						builder.Append($"+{LocalizedModifiers[KeyModifiers.Alt]}");
					if (modifier.HasFlag(KeyModifiers.Ctrl))
						builder.Append($"+{LocalizedModifiers[KeyModifiers.Ctrl]}");
					if (modifier.HasFlag(KeyModifiers.Shift))
						builder.Append($"+{LocalizedModifiers[KeyModifiers.Shift]}");
					if (modifier.HasFlag(KeyModifiers.Win))
						builder.Append($"+{LocalizedModifiers[KeyModifiers.Win]}");
					builder.Remove(0, 1);
					return builder.ToString();
				}
			}
		}

		/// <summary>
		/// Initializes an instance of <see cref="HotKey"/>.
		/// </summary>
		/// <param name="key">A key</param>
		/// <param name="modifier">A modifier</param>
		/// <param name="isVisible">A value that indicates the hotkey should be available.</param>
		public HotKey(Keys key, KeyModifiers modifier = KeyModifiers.None, bool isVisible = true)
		{
			if (!Enum.IsDefined(key) || !Enum.IsDefined(modifier))
				return;

			IsVisible = isVisible;
			Key = key;
			Modifier = modifier;
		}

		/// <summary>
		/// Parses humanized hotkey code with separators.
		/// </summary>
		/// <param name="code">Humanized code to parse.</param>
		/// <param name="localized">Whether the code is localized.</param>
		/// <returns>Humanized code with a format <see cref="HotKey"/>.</returns>
		public static HotKey Parse(string code, bool localized = true)
		{
			var key = Keys.None;
			var modifier = KeyModifiers.None;
			bool isVisible = true;

			code = code.Trim();
			var parts = code.Split('+').Select(part => part.Trim());

			foreach (var part in parts)
			{
				if (localized)
				{
					key |= LocalizedKeys.FirstOrDefault(x => x.Value == part).Key;
					modifier |= LocalizedModifiers.FirstOrDefault(x => x.Value == part).Key;
				}
				else
				{
					if (Enum.TryParse(part, true, out Keys partKey))
						key = partKey;
					if (Enum.TryParse(part, true, out KeyModifiers partModifier))
						modifier |= partModifier;
				}
			}

			return new(key, modifier, isVisible);
		}

		/// <summary>
		/// Converts this <see cref="HotKey"/> instance into a <see cref="HotKeyCollection"/> instance.
		/// </summary>
		/// <returns></returns>
		public HotKeyCollection AsCollection()
		{
			return new(this);
		}

		// Operator overloads

		public static implicit operator string(HotKey hotKey) => hotKey.LocalizedLabel;
		public static bool operator ==(HotKey a, HotKey b) => a.Equals(b);
		public static bool operator !=(HotKey a, HotKey b) => !a.Equals(b);

		// Default methods

		public override string ToString() => LocalizedLabel;
		public override int GetHashCode() => (Key, Modifier, IsVisible).GetHashCode();
		public override bool Equals(object? other) => other is HotKey hotKey && Equals(hotKey);
		public bool Equals(HotKey other) => (other.Key, other.Modifier, other.IsVisible).Equals((Key, Modifier, IsVisible));

		// Private methods

		private static string GetLocalizedKey(string key)
		{
			return $"Key/{key}".GetLocalizedResource();
		}

		private static string GetKeyCharacter(Forms.Keys key)
		{
			var buffer = new StringBuilder(256);
			var state = new byte[256];
			_ = Win32PInvoke.ToUnicode((uint)key, 0, state, buffer, 256, 0);

			return buffer.ToString();
		}
	}
}
