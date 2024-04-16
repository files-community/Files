using ByteSizeLib;
using Files.Shared.Extensions;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.IO;

namespace Files.Uwp.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Returns true if <paramref name="path"/> starts with the path <paramref name="baseDirPath"/>.
        /// The comparison is case-insensitive, handles / and \ slashes as folder separators and
        /// only matches if the base dir folder name is matched exactly ("c:\foobar\file.txt" is not a sub path of "c:\foo").
        /// </summary>
        public static bool IsSubPathOf(this string path, string baseDirPath)
        {
            string normalizedPath = Path.GetFullPath(path.Replace('/', '\\')
                .WithEnding("\\"));

            string normalizedBaseDirPath = Path.GetFullPath(baseDirPath.Replace('/', '\\')
                .WithEnding("\\"));

            return normalizedPath.StartsWith(normalizedBaseDirPath, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns <paramref name="str"/> with the minimal concatenation of <paramref name="ending"/> (starting from end) that
        /// results in satisfying .EndsWith(ending).
        /// </summary>
        /// <example>"hel".WithEnding("llo") returns "hello", which is the result of "hel" + "lo".</example>
        public static string WithEnding(this string str, string ending)
        {
            if (str == null)
            {
                return ending;
            }

            string result = str;

            // Right() is 1-indexed, so include these cases
            // * Append no characters
            // * Append up to N characters, where N is ending length
            for (int i = 0; i <= ending.Length; i++)
            {
                string tmp = result + ending.Right(i);
                if (tmp.EndsWith(ending, StringComparison.Ordinal))
                {
                    return tmp;
                }
            }

            return result;
        }

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
