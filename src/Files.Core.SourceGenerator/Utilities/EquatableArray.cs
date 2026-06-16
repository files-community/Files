// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Files.Core.SourceGenerator.Utilities;

/// <summary>
/// An immutable array wrapper that supports value-based equality comparison.
/// </summary>
/// <remarks>
/// For the reference implementation, see <a href="https://github.com/Sergio0694/ComputeSharp/blob/main/src/ComputeSharp.SourceGeneration/Helpers/EquatableArray%7BT%7D.cs">EquatableArray{T}@ComputeSharp</a>
/// </remarks>
/// <typeparam name="T"></typeparam>
/// <param name="array"></param>
internal readonly struct EquatableArray<T>(ImmutableArray<T> array) : IEquatable<EquatableArray<T>>, IEnumerable<T>
	where T : IEquatable<T>
{
	private readonly T[]? array = ImmutableCollectionsMarshal.AsArray(array);

	public ref readonly T this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref AsImmutableArray().ItemRef(index);
	}

	public bool IsEmpty
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsImmutableArray().IsEmpty;
	}

	public bool IsDefaultOrEmpty
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsImmutableArray().IsDefaultOrEmpty;
	}

	public int Length
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => AsImmutableArray().Length;
	}

	public bool Equals(EquatableArray<T> array)
	{
		return AsSpan().SequenceEqual(array.AsSpan());
	}

	public override bool Equals(object? obj)
	{
		return obj is EquatableArray<T> array && Equals(this, array);
	}

	public override unsafe int GetHashCode()
	{
		if (this.array is not T[] array)
			return 0;

		HashCode hashCode = default;

		if (typeof(T) == typeof(byte))
		{
			ReadOnlySpan<T> span = array;
			ref T r0 = ref MemoryMarshal.GetReference(span);
			ref byte r1 = ref Unsafe.As<T, byte>(ref r0);

			fixed (byte* p = &r1)
			{
				ReadOnlySpan<byte> bytes = new(p, span.Length);

				hashCode.AddBytes(bytes);
			}
		}
		else
		{
			foreach (T item in array)
				hashCode.Add(item);
		}

		return hashCode.ToHashCode();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ImmutableArray<T> AsImmutableArray()
	{
		return ImmutableCollectionsMarshal.AsImmutableArray(this.array);
	}

	public static EquatableArray<T> FromImmutableArray(ImmutableArray<T> array)
	{
		return new(array);
	}

	public ReadOnlySpan<T> AsSpan()
	{
		return AsImmutableArray().AsSpan();
	}

	public T[] ToArray()
	{
		return [.. AsImmutableArray()];
	}

	public ImmutableArray<T>.Enumerator GetEnumerator()
	{
		return AsImmutableArray().GetEnumerator();
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return ((IEnumerable<T>)AsImmutableArray()).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable)AsImmutableArray()).GetEnumerator();
	}

	public static implicit operator EquatableArray<T>(ImmutableArray<T> array) => FromImmutableArray(array);

	public static implicit operator ImmutableArray<T>(EquatableArray<T> array) => array.AsImmutableArray();

	public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

	public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
}
