using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lib = ByteSizeLib;

namespace Files.Backend.Item
{
    public struct ByteSize : IEquatable<ByteSize>, IComparable<ByteSize>
    {
        private static readonly IReadOnlyDictionary<string, string> units = new ReadOnlyDictionary<string, string>
        (
            new Dictionary<string, string>
            {
                [Lib.ByteSize.BitSymbol] = "ByteSymbol".ToLocalized(),
                [Lib.ByteSize.ByteSymbol] = "ByteSymbol".ToLocalized(),
                [Lib.ByteSize.KibiByteSymbol] = "KiloByteSymbol".ToLocalized(),
                [Lib.ByteSize.MebiByteSymbol] = "MegaByteSymbol".ToLocalized(),
                [Lib.ByteSize.GibiByteSymbol] = "GigaByteSymbol".ToLocalized(),
                [Lib.ByteSize.TebiByteSymbol] = "TeraByteSymbol".ToLocalized(),
                [Lib.ByteSize.PebiByteSymbol] = "PetaByteSymbol".ToLocalized(),
            }
        );

        private readonly Lib.ByteSize size;

        public static readonly ByteSize Zero = new(0L);
        public static readonly ByteSize MaxValue = new(long.MaxValue);

        public ulong Bytes => (ulong)size.Bytes;

        public string ShortString
            => $"{size.LargestWholeNumberBinaryValue:0.##} {units[size.LargestWholeNumberBinarySymbol]}";
        public string LongString
            => $"{ShortString} ({size.Bytes:#,##0} {units[Lib.ByteSize.ByteSymbol]})";

        public ByteSize(ulong bytes)
        {
            size = bytes switch
            {
                > long.MaxValue => throw new ArgumentException($"The maximum size is {long.MaxValue}."),
                _ => Lib.ByteSize.FromBytes((long)bytes),
            };
        }

        public static ByteSize FromBytes(ulong bytes) => new(bytes);
        public static ByteSize FromKibiBytes(ulong bytes) => new(bytes * Lib.ByteSize.BytesInKibiByte);
        public static ByteSize FromMebiBytes(ulong bytes) => new(bytes * Lib.ByteSize.BytesInMebiByte);
        public static ByteSize FromGibiBytes(ulong bytes) => new(bytes * Lib.ByteSize.BytesInGibiByte);
        public static ByteSize FromTebiBytes(ulong bytes) => new(bytes * Lib.ByteSize.BytesInTebiByte);
        public static ByteSize FromPebiBytes(ulong bytes) => new(bytes * Lib.ByteSize.BytesInPebiByte);

        public static explicit operator long(ByteSize size) => (long)size.Bytes;
        public static explicit operator ulong(ByteSize size) => size.Bytes;
        public static implicit operator ByteSize(long size) => new((ulong)size);
        public static implicit operator ByteSize(ulong size) => new(size);

        public static ByteSize operator +(ByteSize a, ByteSize b) => new(a.Bytes + b.Bytes);
        public static ByteSize operator -(ByteSize a, ByteSize b) => new(a.Bytes - b.Bytes);
        public static bool operator ==(ByteSize a, ByteSize b) => a.size == b.size;
        public static bool operator !=(ByteSize a, ByteSize b) => a.size != b.size;
        public static bool operator <(ByteSize a, ByteSize b) => a.size < b.size;
        public static bool operator >(ByteSize a, ByteSize b) => a.size > b.size;
        public static bool operator <=(ByteSize a, ByteSize b) => a.size <= b.size;
        public static bool operator >=(ByteSize a, ByteSize b) => a.size >= b.size;

        public override string ToString() => ShortString;
        public override int GetHashCode() => size.GetHashCode();
        public override bool Equals(object other) => other is ByteSize size && Equals(size);
        public bool Equals(ByteSize other) => other.size.Equals(size);
        public int CompareTo(ByteSize other) => other.size.CompareTo(size);
    }
}
