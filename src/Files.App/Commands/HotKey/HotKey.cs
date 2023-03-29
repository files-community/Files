using System;
using Windows.System;

namespace Files.App.Commands
{
	public readonly struct HotKey : IEquatable<HotKey>
	{
		public static HotKey None { get; } = new(Keys.None, KeyModifiers.None);

		public bool IsNone => Key is VirtualKey.None;

		public VirtualKey Key { get; } = VirtualKey.None;
		public VirtualKeyModifiers Modifiers { get; } = VirtualKeyModifiers.None;

		public Keys CommandKey => (Keys)Key;
		public KeyModifiers CommandKeyModifiers => (KeyModifiers)Modifiers;

		public HotKey(Keys key) : this(key, KeyModifiers.None) {}
		public HotKey(Keys key, KeyModifiers modifier)
		{
			if (key is Keys.None)
				return;

			Key = (VirtualKey)key;
			Modifiers = (VirtualKeyModifiers)modifier;
		}

		public HotKey(VirtualKey key) : this(key, VirtualKeyModifiers.None) {}
		public HotKey(VirtualKey key, VirtualKeyModifiers modifiers)
		{
			if (!key.IsValid())
				return;

			if (IsModifier(key))
				throw new ArgumentException("The key cannot be a modifier.", nameof(key));

			Key = key;
			Modifiers = modifiers;

			static bool IsModifier(VirtualKey key)
				=> key is VirtualKey.Menu or VirtualKey.LeftMenu or VirtualKey.RightMenu
				or VirtualKey.Control or VirtualKey.LeftControl or VirtualKey.RightControl
				or VirtualKey.Shift or VirtualKey.LeftShift or VirtualKey.RightShift
				or VirtualKey.LeftWindows or VirtualKey.RightWindows;
		}

		public void Deconstruct(out VirtualKey key, out VirtualKeyModifiers modifiers)
			=> (key, modifiers) = (Key, Modifiers);

		public static implicit operator string(HotKey hotKey) => hotKey.ToString();

		public static bool operator ==(HotKey a, HotKey b) => a.Equals(b);
		public static bool operator !=(HotKey a, HotKey b) => !a.Equals(b);

		public override string ToString() => HotKeyHelpers.ToString(this);

		public override int GetHashCode() => (Key, Modifiers).GetHashCode();
		public override bool Equals(object? other) => other is HotKey hotKey && Equals(hotKey);
		public bool Equals(HotKey other) => (other.Key, other.Modifiers).Equals((Key, Modifiers));
	}
}
