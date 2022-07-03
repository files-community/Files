using ByteSizeLib;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;

namespace Files.Uwp.Extensions
{
    public static class StringExtensions
    {
        private static readonly Dictionary<string, string> abbreviations = new Dictionary<string, string>()
        {
            { "KiB", "KiloByteSymbol".GetLocalized() },
            { "MiB", "MegaByteSymbol".GetLocalized() },
            { "GiB", "GigaByteSymbol".GetLocalized() },
            { "TiB", "TeraByteSymbol".GetLocalized() },
            { "PiB", "PetaByteSymbol".GetLocalized() },
            { "B", "ByteSymbol".GetLocalized() },
            { "b", "ByteSymbol".GetLocalized() }
        };

        public static string ConvertSizeAbbreviation(this string value)
        {
            foreach (var item in abbreviations)
            {
                value = value.Replace(item.Key, item.Value, StringComparison.Ordinal);
            }
            return value;
        }

        public static string ToSizeString(this long size) => ByteSize.FromBytes(size).ToSizeString();
        public static string ToSizeString(this ulong size) => ByteSize.FromBytes(size).ToSizeString();
        public static string ToSizeString(this ByteSize size) => size.ToBinaryString().ConvertSizeAbbreviation();

        public static string ToLongSizeString(this long size) => ByteSize.FromBytes(size).ToLongSizeString();
        public static string ToLongSizeString(this ulong size) => ByteSize.FromBytes(size).ToLongSizeString();
        public static string ToLongSizeString(this ByteSize size) => $"{size.ToSizeString()} ({size.Bytes:#,##0} {"ItemSizeBytes".GetLocalized()})";
    }
}
