using Files.App.Actions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Files.App.Commands
{
	[DebuggerDisplay("{Code}")]
	public readonly struct HotKeyCollection : IEnumerable<HotKey>, IEquatable<HotKeyCollection>
	{
		private static readonly char[] parseSeparators = new char[] { ',', ';', ' ', '\t' };

		private readonly ImmutableArray<HotKey> hotKeys;

		public static HotKeyCollection Empty { get; } = new(ImmutableArray<HotKey>.Empty);

		public HotKey this[int index] => index >= 0 && index < hotKeys.Length ? hotKeys[index] : HotKey.None;

		public bool IsEmpty => hotKeys.IsDefaultOrEmpty;
		public int Length => hotKeys.Length;

		public string Code => string.Join(',', hotKeys.Select(hotKey => hotKey.Code));
		public string Label => string.Join(',', hotKeys.Where(hotKey => hotKey.IsVisible).Select(hotKey => hotKey.Label));

		public HotKeyCollection() => hotKeys = ImmutableArray<HotKey>.Empty;
		public HotKeyCollection(params HotKey[] hotKeys) => this.hotKeys = Clean(hotKeys);
		public HotKeyCollection(IEnumerable<HotKey> hotKeys) => this.hotKeys = Clean(hotKeys);

		public static bool operator ==(HotKeyCollection hotKeysA, HotKeyCollection hotKeysB) => hotKeysA.Equals(hotKeysB);
		public static bool operator !=(HotKeyCollection hotKeysA, HotKeyCollection hotKeysB) => !hotKeysA.Equals(hotKeysB);

		public static HotKeyCollection Parse(string code)
		{
			var hotKeys = code
				.Replace("!", " !")
				.Split(parseSeparators)
				.Select(part => part.Trim())
				.Select(HotKey.Parse);
			return new(hotKeys);
		}

		public void Contains(HotKey hotKey) => hotKeys.Contains(hotKey);

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IEnumerator<HotKey> GetEnumerator() => (hotKeys as IEnumerable<HotKey>).GetEnumerator();

		public override string ToString() => Label;
		public override int GetHashCode() => hotKeys.GetHashCode();
		public override bool Equals(object? other) => other is HotKeyCollection hotKeys && Equals(hotKeys);
		public bool Equals(HotKeyCollection other) => hotKeys.SequenceEqual(other.hotKeys);

		private static ImmutableArray<HotKey> Clean(IEnumerable<HotKey> hotKeys)
		{
			return hotKeys
				.Distinct()
				.Where(hotKey => !hotKey.IsNone)
				.GroupBy(hotKey => hotKey with { IsVisible = true})
				.Select(group => group.OrderBy(hotKey => hotKey.IsVisible).Last())
				.ToImmutableArray();
		}
	}
}
