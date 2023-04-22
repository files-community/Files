// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;

namespace Files.App.Commands
{
	public readonly struct HotKey : IEquatable<HotKey>
	{
		public static HotKey None { get; } = new(Keys.None, KeyModifiers.None);

		public bool IsNone => Key is Keys.None;

		public Keys Key { get; }
		public KeyModifiers Modifiers { get; }

		public HotKey(Keys key) : this(key, KeyModifiers.None) {}
		public HotKey(Keys key, KeyModifiers modifiers)
		{
			if (key is Keys.None || !Enum.IsDefined(key))
				return;

			Key = key;
			Modifiers = modifiers;
		}

		public void Deconstruct(out Keys key, out KeyModifiers modifiers)
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
