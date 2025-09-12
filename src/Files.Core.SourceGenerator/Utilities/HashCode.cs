using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

#pragma warning disable CS0809
#pragma warning disable CA1066

namespace System
{
	// xxHash32 is used for the hash code.
	// https://github.com/Cyan4973/xxHash
	// Polyfill System.HashCode
	public struct HashCode
	{
		private static readonly uint s_seed = GenerateGlobalSeed();

		private const uint Prime1 = 2654435761U;
		private const uint Prime2 = 2246822519U;
		private const uint Prime3 = 3266489917U;
		private const uint Prime4 = 668265263U;
		private const uint Prime5 = 374761393U;

		private uint _v1, _v2, _v3, _v4;
		private uint _queue1, _queue2, _queue3;
		private uint _length;

		private static unsafe uint GenerateGlobalSeed()
		{
			byte[] bytes = new byte[4];

			RandomNumberGenerator.Create().GetBytes(bytes);

			return BitConverter.ToUInt32(bytes, 0);
		}

		public static int Combine<T1>(T1 value1)
		{
			// Provide a way of diffusing bits from something with a limited
			// input hash space. For example, many enums only have a few
			// possible hashes, only using the bottom few bits of the code. Some
			// collections are built on the assumption that hashes are spread
			// over a larger space, so diffusing the bits may help the
			// collection work more efficiently.

			uint hc1 = (uint)(value1?.GetHashCode() ?? 0);

			uint hash = MixEmptyState();
			hash += 4;

			hash = QueueRound(hash, hc1);

			hash = MixFinal(hash);
			return (int)hash;
		}

		public static int Combine<T1, T2>(T1 value1, T2 value2)
		{
			uint hc1 = (uint)(value1?.GetHashCode() ?? 0);
			uint hc2 = (uint)(value2?.GetHashCode() ?? 0);

			uint hash = MixEmptyState();
			hash += 8;

			hash = QueueRound(hash, hc1);
			hash = QueueRound(hash, hc2);

			hash = MixFinal(hash);
			return (int)hash;
		}

		public static int Combine<T1, T2, T3>(T1 value1, T2 value2, T3 value3)
		{
			uint hc1 = (uint)(value1?.GetHashCode() ?? 0);
			uint hc2 = (uint)(value2?.GetHashCode() ?? 0);
			uint hc3 = (uint)(value3?.GetHashCode() ?? 0);

			uint hash = MixEmptyState();
			hash += 12;

			hash = QueueRound(hash, hc1);
			hash = QueueRound(hash, hc2);
			hash = QueueRound(hash, hc3);

			hash = MixFinal(hash);
			return (int)hash;
		}

		public static int Combine<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4)
		{
			uint hc1 = (uint)(value1?.GetHashCode() ?? 0);
			uint hc2 = (uint)(value2?.GetHashCode() ?? 0);
			uint hc3 = (uint)(value3?.GetHashCode() ?? 0);
			uint hc4 = (uint)(value4?.GetHashCode() ?? 0);

			Initialize(out uint v1, out uint v2, out uint v3, out uint v4);

			v1 = Round(v1, hc1);
			v2 = Round(v2, hc2);
			v3 = Round(v3, hc3);
			v4 = Round(v4, hc4);

			uint hash = MixState(v1, v2, v3, v4);
			hash += 16;

			hash = MixFinal(hash);
			return (int)hash;
		}

		public static int Combine<T1, T2, T3, T4, T5>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)
		{
			uint hc1 = (uint)(value1?.GetHashCode() ?? 0);
			uint hc2 = (uint)(value2?.GetHashCode() ?? 0);
			uint hc3 = (uint)(value3?.GetHashCode() ?? 0);
			uint hc4 = (uint)(value4?.GetHashCode() ?? 0);
			uint hc5 = (uint)(value5?.GetHashCode() ?? 0);

			Initialize(out uint v1, out uint v2, out uint v3, out uint v4);

			v1 = Round(v1, hc1);
			v2 = Round(v2, hc2);
			v3 = Round(v3, hc3);
			v4 = Round(v4, hc4);

			uint hash = MixState(v1, v2, v3, v4);
			hash += 20;

			hash = QueueRound(hash, hc5);

			hash = MixFinal(hash);
			return (int)hash;
		}

		public static int Combine<T1, T2, T3, T4, T5, T6>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)
		{
			uint hc1 = (uint)(value1?.GetHashCode() ?? 0);
			uint hc2 = (uint)(value2?.GetHashCode() ?? 0);
			uint hc3 = (uint)(value3?.GetHashCode() ?? 0);
			uint hc4 = (uint)(value4?.GetHashCode() ?? 0);
			uint hc5 = (uint)(value5?.GetHashCode() ?? 0);
			uint hc6 = (uint)(value6?.GetHashCode() ?? 0);

			Initialize(out uint v1, out uint v2, out uint v3, out uint v4);

			v1 = Round(v1, hc1);
			v2 = Round(v2, hc2);
			v3 = Round(v3, hc3);
			v4 = Round(v4, hc4);

			uint hash = MixState(v1, v2, v3, v4);
			hash += 24;

			hash = QueueRound(hash, hc5);
			hash = QueueRound(hash, hc6);

			hash = MixFinal(hash);
			return (int)hash;
		}

		public static int Combine<T1, T2, T3, T4, T5, T6, T7>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7)
		{
			uint hc1 = (uint)(value1?.GetHashCode() ?? 0);
			uint hc2 = (uint)(value2?.GetHashCode() ?? 0);
			uint hc3 = (uint)(value3?.GetHashCode() ?? 0);
			uint hc4 = (uint)(value4?.GetHashCode() ?? 0);
			uint hc5 = (uint)(value5?.GetHashCode() ?? 0);
			uint hc6 = (uint)(value6?.GetHashCode() ?? 0);
			uint hc7 = (uint)(value7?.GetHashCode() ?? 0);

			Initialize(out uint v1, out uint v2, out uint v3, out uint v4);

			v1 = Round(v1, hc1);
			v2 = Round(v2, hc2);
			v3 = Round(v3, hc3);
			v4 = Round(v4, hc4);

			uint hash = MixState(v1, v2, v3, v4);
			hash += 28;

			hash = QueueRound(hash, hc5);
			hash = QueueRound(hash, hc6);
			hash = QueueRound(hash, hc7);

			hash = MixFinal(hash);
			return (int)hash;
		}

		public static int Combine<T1, T2, T3, T4, T5, T6, T7, T8>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8)
		{
			uint hc1 = (uint)(value1?.GetHashCode() ?? 0);
			uint hc2 = (uint)(value2?.GetHashCode() ?? 0);
			uint hc3 = (uint)(value3?.GetHashCode() ?? 0);
			uint hc4 = (uint)(value4?.GetHashCode() ?? 0);
			uint hc5 = (uint)(value5?.GetHashCode() ?? 0);
			uint hc6 = (uint)(value6?.GetHashCode() ?? 0);
			uint hc7 = (uint)(value7?.GetHashCode() ?? 0);
			uint hc8 = (uint)(value8?.GetHashCode() ?? 0);

			Initialize(out uint v1, out uint v2, out uint v3, out uint v4);

			v1 = Round(v1, hc1);
			v2 = Round(v2, hc2);
			v3 = Round(v3, hc3);
			v4 = Round(v4, hc4);

			v1 = Round(v1, hc5);
			v2 = Round(v2, hc6);
			v3 = Round(v3, hc7);
			v4 = Round(v4, hc8);

			uint hash = MixState(v1, v2, v3, v4);
			hash += 32;

			hash = MixFinal(hash);
			return (int)hash;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Initialize(out uint v1, out uint v2, out uint v3, out uint v4)
		{
			v1 = s_seed + Prime1 + Prime2;
			v2 = s_seed + Prime2;
			v3 = s_seed;
			v4 = s_seed - Prime1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static uint Round(uint hash, uint input)
		{
			return RotateLeft(hash + input * Prime2, 13) * Prime1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static uint QueueRound(uint hash, uint queuedValue)
		{
			return RotateLeft(hash + queuedValue * Prime3, 17) * Prime4;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static uint MixState(uint v1, uint v2, uint v3, uint v4)
		{
			return RotateLeft(v1, 1) + RotateLeft(v2, 7) + RotateLeft(v3, 12) + RotateLeft(v4, 18);
		}

		private static uint MixEmptyState()
		{
			return s_seed + Prime5;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static uint MixFinal(uint hash)
		{
			hash ^= hash >> 15;
			hash *= Prime2;
			hash ^= hash >> 13;
			hash *= Prime3;
			hash ^= hash >> 16;
			return hash;
		}

		public void Add<T>(T value)
		{
			Add(value?.GetHashCode() ?? 0);
		}

		public void Add<T>(T value, IEqualityComparer<T>? comparer)
		{
			Add(value is null ? 0 : (comparer?.GetHashCode(value) ?? value.GetHashCode()));
		}

		public void AddBytes(ReadOnlySpan<byte> value)
		{
			ref byte pos = ref MemoryMarshal.GetReference(value);
			ref byte end = ref Unsafe.Add(ref pos, value.Length);

			while ((nint)Unsafe.ByteOffset(ref pos, ref end) >= sizeof(int))
			{
				Add(Unsafe.ReadUnaligned<int>(ref pos));
				pos = ref Unsafe.Add(ref pos, sizeof(int));
			}

			while (Unsafe.IsAddressLessThan(ref pos, ref end))
			{
				Add((int)pos);
				pos = ref Unsafe.Add(ref pos, 1);
			}
		}

		private void Add(int value)
		{
			uint val = (uint)value;
			uint previousLength = _length++;
			uint position = previousLength % 4;

			if (position == 0)
				_queue1 = val;
			else if (position == 1)
				_queue2 = val;
			else if (position == 2)
				_queue3 = val;
			else
			{
				if (previousLength == 3)
					Initialize(out _v1, out _v2, out _v3, out _v4);

				_v1 = Round(_v1, _queue1);
				_v2 = Round(_v2, _queue2);
				_v3 = Round(_v3, _queue3);
				_v4 = Round(_v4, val);
			}
		}

		public int ToHashCode()
		{
			uint length = _length;
			uint position = length % 4;
			uint hash = length < 4 ? MixEmptyState() : MixState(_v1, _v2, _v3, _v4);

			hash += length * 4;

			if (position > 0)
			{
				hash = QueueRound(hash, _queue1);

				if (position > 1)
				{
					hash = QueueRound(hash, _queue2);
					if (position > 2)
						hash = QueueRound(hash, _queue3);
				}
			}

			hash = MixFinal(hash);
			return (int)hash;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static uint RotateLeft(uint value, int offset)
		{
			return (value << offset) | (value >> (32 - offset));
		}

		[Obsolete("HashCode is a mutable struct and should not be compared with other HashCodes. Use ToHashCode to retrieve the computed hash code.", error: true)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override int GetHashCode() => throw new NotSupportedException();

		[Obsolete("HashCode is a mutable struct and should not be compared with other HashCodes.", error: true)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override bool Equals(object? obj) => throw new NotSupportedException();
	}
}
