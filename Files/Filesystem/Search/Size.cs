using ByteSizeLib;
using Files.Extensions;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Files.Filesystem.Search
{
    public struct Size : IEquatable<Size>, IComparable<Size>, IFormattable
    {
        public enum Units : ushort { Byte, Kibi, Mebi, Gibi, Tebi, Pebi }

        private readonly ByteSize size;

        public static readonly Size MinValue = new(0);
        public static readonly Size MaxValue = new(ByteSize.MaxValue);

        public long Bytes => size.Bits * ByteSize.BitsInByte;

        public double Value => size.LargestWholeNumberBinaryValue;
        public Units Unit => size.LargestWholeNumberBinarySymbol switch
        {
            ByteSize.KibiByteSymbol => Units.Kibi,
            ByteSize.MebiByteSymbol => Units.Mebi,
            ByteSize.GibiByteSymbol => Units.Gibi,
            ByteSize.TebiByteSymbol => Units.Tebi,
            ByteSize.PebiByteSymbol => Units.Pebi,
            _ => Units.Byte,
        };

        public Size(long bytes) => size = bytes switch
        {
            < 0 => ByteSize.FromBytes(0),
            _ => ByteSize.FromBytes(bytes),
        };
        public Size(double value, Units unit)
        {
            if (value < 0)
            {
                throw new ArgumentException("Size is always positive.");
            }
            size = unit switch
            {
                Units.Byte => ByteSize.FromBytes(value),
                Units.Kibi => ByteSize.FromKibiBytes(value),
                Units.Mebi => ByteSize.FromMebiBytes(value),
                Units.Gibi => ByteSize.FromGibiBytes(value),
                Units.Tebi => ByteSize.FromTebiBytes(value),
                Units.Pebi => ByteSize.FromPebiBytes(value),
                _ => throw new ArgumentException(),
            };
        }
        private Size(ByteSize size)
        {
            if (size.Bytes < 0)
            {
                throw new ArgumentException("Size is always positive.");
            }
            this.size = size;
        }

        public static implicit operator Size(ByteSize size) => new(size);
        public static explicit operator ByteSize(Size size) => size.size;

        public static Size operator +(Size a, Size b) => new(a.size + b.size);
        public static Size operator -(Size a, Size b) => new(a.size - b.size);
        public static bool operator ==(Size a, Size b) => a.size == b.size;
        public static bool operator !=(Size a, Size b) => a.size != b.size;
        public static bool operator <(Size a, Size b) => a.size < b.size;
        public static bool operator >(Size a, Size b) => a.size > b.size;
        public static bool operator <=(Size a, Size b) => a.size <= b.size;
        public static bool operator >=(Size a, Size b) => a.size >= b.size;

        public override int GetHashCode() => size.GetHashCode();
        public override bool Equals(object other) => other is Size size && Equals(size);
        public bool Equals(Size other) => other.size.Equals(size);
        public int CompareTo(Size other) => other.size.CompareTo(size);

        public override string ToString() => ToString("G");
        public string ToString(string format) => ToString(format, CultureInfo.CurrentCulture);
        public string ToString(string format, IFormatProvider formatProvider) => (format ?? "G").ToLower() switch
        {
            "g" => size.ToBinaryString().ConvertSizeAbbreviation(),
            "G" => size.ToBinaryString().ConvertSizeAbbreviation(),
            "n" => GetName() ?? ToString("G", formatProvider),
            "N" => GetName() ?? ToString("G", formatProvider),
            "b" => Bytes.ToString(),
            "B" => Bytes.ToString(),
            "u" => size.LargestWholeNumberBinarySymbol.ConvertSizeAbbreviation(),
            "U" => size.LargestWholeNumberBinarySymbol.ConvertSizeAbbreviation(),
            _ => string.Empty,
        };

        private string GetName() => size == ByteSize.MaxValue ? "No limit" : null;
    }

    public struct SizeRange : IEquatable<SizeRange>, IFormattable
    {
        public static readonly SizeRange None = new(true, Size.MaxValue, Size.MaxValue);
        public static readonly SizeRange All = new(true, Size.MinValue, Size.MaxValue);
        public static readonly SizeRange Empty = new(true, Size.MinValue, Size.MinValue);
        public static readonly SizeRange Tiny = new(true, new Size(1), new Size(16, Size.Units.Kibi));
        public static readonly SizeRange Small = new(true, new Size(16, Size.Units.Kibi), new Size(1, Size.Units.Mebi));
        public static readonly SizeRange Medium = new(true, new Size(1, Size.Units.Mebi), new Size(128, Size.Units.Mebi));
        public static readonly SizeRange Large = new(true, new Size(128, Size.Units.Mebi), new Size(1, Size.Units.Gibi));
        public static readonly SizeRange VeryLarge = new(true, new Size(1, Size.Units.Gibi), new Size(5, Size.Units.Gibi));
        public static readonly SizeRange Huge = new(true, new Size(5, Size.Units.Gibi), Size.MaxValue);

        public bool IsNamed { get; }

        public Size MinSize { get; }
        public Size MaxSize { get; }

        public SizeRange(Size minSize, Size maxSize)
        {
            if (minSize > maxSize)
            {
                (minSize, maxSize) = (maxSize, minSize);
            }

            var named = new List<SizeRange> { Empty, Tiny, Small, Medium, Large, VeryLarge, Huge };
            bool isNamed = named.Any(n => n.MinSize == minSize) && named.Any(n => n.MaxSize == maxSize);

            (IsNamed, MinSize, MaxSize) = (isNamed, minSize, maxSize);
        }
        public SizeRange(Size minSize, SizeRange maxRange)
            : this(Min(minSize, maxRange.MinSize), Max(minSize, maxRange.MaxSize)) {}
        public SizeRange(SizeRange minRange, Size maxSize)
            : this(Min(minRange.MinSize, maxSize), Max(minRange.MaxSize, maxSize)) {}
        public SizeRange(SizeRange minRange, SizeRange maxRange)
            : this(Min(minRange.MinSize, maxRange.MinSize), Max(minRange.MaxSize, maxRange.MaxSize)) {}
        private SizeRange(bool isNamed, Size minSize, Size maxSize)
            => (IsNamed, MinSize, MaxSize) = (isNamed, minSize, maxSize);

        public void Deconstruct(out Size minSize, out Size maxSize)
            => (minSize, maxSize) = (MinSize, MaxSize);
        public void Deconstruct(out bool isNamed, out Size minSize, out Size maxSize)
            => (isNamed, minSize, maxSize) = (IsNamed, MinSize, MaxSize);

        public override int GetHashCode()
            => (MinSize, MaxSize).GetHashCode();
        public override bool Equals(object other)
            => other is SizeRange range && Equals(range);
        public bool Equals(SizeRange other)
            => other is SizeRange range && range.MinSize == MinSize && range.MaxSize == MaxSize;

        public override string ToString() => ToString("G");
        public string ToString(string format) => ToString(format, CultureInfo.CurrentCulture);
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (Equals(None) || Equals(All))
            {
                return string.Empty;
            }

            if (format == "g")
            {
                return ToString("n", formatProvider);
            }
            if (format is null || format == "G")
            {
                return ToString("N", formatProvider);
            }

            var (isNamed, minSize, maxSize) = this;
            bool useName = isNamed && format.ToLower() == "n";

            bool hasMin = minSize > Size.MinValue;
            bool hasMax = maxSize < Size.MaxValue;

            string minLabel = GetMinLabel();
            string maxLabel = GetMaxLabel();

            return format switch
            {
                "n" => string.Format(GetShortFormat(), minLabel , maxLabel),
                "N" => string.Format(GetFullFormat(), minLabel, maxLabel),
                "r" => string.Format(GetShortFormat(), minLabel, maxLabel),
                "R" => string.Format(GetFullFormat(), minLabel, maxLabel),
                "q" => string.Format(GetQueryFormat(), minSize, maxSize),
                "Q" => string.Format(GetQueryFormat(), minSize, maxSize),
                _ => string.Empty,
            };

            string GetMinLabel() => useName switch
            {
                true when Empty.MinSize.Equals(minSize) => "Empty",
                true when Tiny.MinSize.Equals(minSize) => "ItemSizeText_Tiny".GetLocalized(),
                true when Small.MinSize.Equals(minSize) => "ItemSizeText_Small".GetLocalized(),
                true when Medium.MinSize.Equals(minSize) => "ItemSizeText_Medium".GetLocalized(),
                true when Large.MinSize.Equals(minSize) => "ItemSizeText_Large".GetLocalized(),
                true when VeryLarge.MinSize.Equals(minSize) => "ItemSizeText_VeryLarge".GetLocalized(),
                true when Huge.MinSize.Equals(minSize) => "ItemSizeText_Huge".GetLocalized(),
                true => string.Empty,
                false => $"{minSize}",
            };
            string GetMaxLabel() => useName switch
            {
                true when Empty.MaxSize.Equals(maxSize) => "Empty",
                true when Tiny.MaxSize.Equals(maxSize) => "ItemSizeText_Tiny".GetLocalized(),
                true when Small.MaxSize.Equals(maxSize) => "ItemSizeText_Small".GetLocalized(),
                true when Medium.MaxSize.Equals(maxSize) => "ItemSizeText_Medium".GetLocalized(),
                true when Large.MaxSize.Equals(maxSize) => "ItemSizeText_Large".GetLocalized(),
                true when VeryLarge.MaxSize.Equals(maxSize) => "ItemSizeText_VeryLarge".GetLocalized(),
                true when Huge.MaxSize.Equals(maxSize) => "ItemSizeText_Huge".GetLocalized(),
                true => string.Empty,
                false => $"{maxSize}",
            };

            string GetShortFormat() => (hasMin, hasMax) switch
            {
                _ when minLabel == maxLabel => "{0}",
                (false, _) => "< {1}",
                (_, false) => "> {0}",
                _ => "{0} - {1}",
            };
            string GetFullFormat() => (hasMin, hasMax) switch
            {
                _ when minLabel == maxLabel => "{0}",
                (false, _) => "Less than {1}",
                (_, false) => "Greater than {0}",
                _ => "Between {0} and {1}",
            };
            string GetQueryFormat() => (hasMin, hasMax) switch
            {
                _ when minSize == maxSize => "{0:B}",
                (false, _) => "<{1:B}",
                (_, false) => ">{0:B}",
                _ => "{0:B}..{1:B}",
            };
        }

        public static SizeRange operator +(SizeRange a, SizeRange b) => new(a, b);
        public static SizeRange operator -(SizeRange a, SizeRange b) => Substract(a, b);
        public static bool operator ==(SizeRange a, SizeRange b) => a.Equals(b);
        public static bool operator !=(SizeRange a, SizeRange b) => !a.Equals(b);
        public static bool operator <(SizeRange a, SizeRange b) => a.MaxSize < b.MinSize;
        public static bool operator >(SizeRange a, SizeRange b) => a.MaxSize > b.MinSize;
        public static bool operator <=(SizeRange a, SizeRange b) => a.MaxSize <= b.MinSize;
        public static bool operator >=(SizeRange a, SizeRange b) => a.MaxSize >= b.MinSize;

        public bool Contains(Size size) => size >= MinSize && size <= MaxSize;
        public bool Contains(SizeRange range) => range.MinSize >= MinSize && range.MaxSize <= MaxSize;

        private static Size Min(Size a, Size b) => a <= b ? a : b;
        private static Size Max(Size a, Size b) => a >= b ? a : b;

        private static SizeRange Substract(SizeRange a, SizeRange b)
        {
            if (b.MinSize == a.MinSize && b.MaxSize < a.MaxSize)
            {
                return new(b.MaxSize, a.MaxSize);
            }
            if (b.MaxSize == a.MaxSize && b.MinSize > a.MinSize)
            {
                return new(a.MinSize, b.MinSize);
            }
            return None;
        }
    }
}
