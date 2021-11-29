using System;
using System.Collections.Generic;

namespace SevenZipExtractor
{
    public class Formats
    {
        internal static readonly Dictionary<string, SevenZipFormat> ExtensionFormatMapping = new Dictionary<string, SevenZipFormat>
        {
            {"7z", SevenZipFormat.SevenZip},
            {"gz", SevenZipFormat.GZip},
            {"tar", SevenZipFormat.Tar},
            {"rar", SevenZipFormat.Rar},
            {"zip", SevenZipFormat.Zip},
            {"lzma", SevenZipFormat.Lzma},
            {"lzh", SevenZipFormat.Lzh},
            {"arj", SevenZipFormat.Arj},
            {"bz2", SevenZipFormat.BZip2},
            {"cab", SevenZipFormat.Cab},
            {"chm", SevenZipFormat.Chm},
            {"deb", SevenZipFormat.Deb},
            {"iso", SevenZipFormat.Iso},
            {"rpm", SevenZipFormat.Rpm},
            {"wim", SevenZipFormat.Wim},
            {"udf", SevenZipFormat.Udf},
            {"mub", SevenZipFormat.Mub},
            {"xar", SevenZipFormat.Xar},
            {"hfs", SevenZipFormat.Hfs},
            {"dmg", SevenZipFormat.Dmg},
            {"z", SevenZipFormat.Lzw},
            {"xz", SevenZipFormat.XZ},
            {"flv", SevenZipFormat.Flv},
            {"swf", SevenZipFormat.Swf},
            {"exe", SevenZipFormat.PE},
            {"dll", SevenZipFormat.PE},
            {"vhd", SevenZipFormat.Vhd}
        };

        internal static Dictionary<SevenZipFormat, Guid> FormatGuidMapping = new Dictionary<SevenZipFormat, Guid>
        {
            {SevenZipFormat.SevenZip, new Guid("23170f69-40c1-278a-1000-000110070000")},
            {SevenZipFormat.Arj, new Guid("23170f69-40c1-278a-1000-000110040000")},
            {SevenZipFormat.BZip2, new Guid("23170f69-40c1-278a-1000-000110020000")},
            {SevenZipFormat.Cab, new Guid("23170f69-40c1-278a-1000-000110080000")},
            {SevenZipFormat.Chm, new Guid("23170f69-40c1-278a-1000-000110e90000")},
            {SevenZipFormat.Compound, new Guid("23170f69-40c1-278a-1000-000110e50000")},
            {SevenZipFormat.Cpio, new Guid("23170f69-40c1-278a-1000-000110ed0000")},
            {SevenZipFormat.Deb, new Guid("23170f69-40c1-278a-1000-000110ec0000")},
            {SevenZipFormat.GZip, new Guid("23170f69-40c1-278a-1000-000110ef0000")},
            {SevenZipFormat.Iso, new Guid("23170f69-40c1-278a-1000-000110e70000")},
            {SevenZipFormat.Lzh, new Guid("23170f69-40c1-278a-1000-000110060000")},
            {SevenZipFormat.Lzma, new Guid("23170f69-40c1-278a-1000-0001100a0000")},
            {SevenZipFormat.Nsis, new Guid("23170f69-40c1-278a-1000-000110090000")},
            {SevenZipFormat.Rar, new Guid("23170f69-40c1-278a-1000-000110030000")},
            {SevenZipFormat.Rar5, new Guid("23170f69-40c1-278a-1000-000110CC0000")},
            {SevenZipFormat.Rpm, new Guid("23170f69-40c1-278a-1000-000110eb0000")},
            {SevenZipFormat.Split, new Guid("23170f69-40c1-278a-1000-000110ea0000")},
            {SevenZipFormat.Tar, new Guid("23170f69-40c1-278a-1000-000110ee0000")},
            {SevenZipFormat.Wim, new Guid("23170f69-40c1-278a-1000-000110e60000")},
            {SevenZipFormat.Lzw, new Guid("23170f69-40c1-278a-1000-000110050000")},
            {SevenZipFormat.Zip, new Guid("23170f69-40c1-278a-1000-000110010000")},
            {SevenZipFormat.Udf, new Guid("23170f69-40c1-278a-1000-000110E00000")},
            {SevenZipFormat.Xar, new Guid("23170f69-40c1-278a-1000-000110E10000")},
            {SevenZipFormat.Mub, new Guid("23170f69-40c1-278a-1000-000110E20000")},
            {SevenZipFormat.Hfs, new Guid("23170f69-40c1-278a-1000-000110E30000")},
            {SevenZipFormat.Dmg, new Guid("23170f69-40c1-278a-1000-000110E40000")},
            {SevenZipFormat.XZ, new Guid("23170f69-40c1-278a-1000-0001100C0000")},
            {SevenZipFormat.Mslz, new Guid("23170f69-40c1-278a-1000-000110D50000")},
            {SevenZipFormat.PE, new Guid("23170f69-40c1-278a-1000-000110DD0000")},
            {SevenZipFormat.Elf, new Guid("23170f69-40c1-278a-1000-000110DE0000")},
            {SevenZipFormat.Swf, new Guid("23170f69-40c1-278a-1000-000110D70000")},
            {SevenZipFormat.Vhd, new Guid("23170f69-40c1-278a-1000-000110DC0000")},
            {SevenZipFormat.Flv, new Guid("23170f69-40c1-278a-1000-000110D60000")},
            {SevenZipFormat.SquashFS, new Guid("23170f69-40c1-278a-1000-000110D20000")},
            {SevenZipFormat.Lzma86, new Guid("23170f69-40c1-278a-1000-0001100B0000")},
            {SevenZipFormat.Ppmd, new Guid("23170f69-40c1-278a-1000-0001100D0000")},
            {SevenZipFormat.TE, new Guid("23170f69-40c1-278a-1000-000110CF0000")},
            {SevenZipFormat.UEFIc, new Guid("23170f69-40c1-278a-1000-000110D00000")},
            {SevenZipFormat.UEFIs, new Guid("23170f69-40c1-278a-1000-000110D10000")},
            {SevenZipFormat.CramFS, new Guid("23170f69-40c1-278a-1000-000110D30000")},
            {SevenZipFormat.APM, new Guid("23170f69-40c1-278a-1000-000110D40000")},
            {SevenZipFormat.Swfc, new Guid("23170f69-40c1-278a-1000-000110D80000")},
            {SevenZipFormat.Ntfs, new Guid("23170f69-40c1-278a-1000-000110D90000")},
            {SevenZipFormat.Fat, new Guid("23170f69-40c1-278a-1000-000110DA0000")},
            {SevenZipFormat.Mbr, new Guid("23170f69-40c1-278a-1000-000110DB0000")},
            {SevenZipFormat.MachO, new Guid("23170f69-40c1-278a-1000-000110DF0000")}
        };

        internal static Dictionary<SevenZipFormat, byte[]> FileSignatures = new Dictionary<SevenZipFormat, byte[]>
        {
            {SevenZipFormat.Rar5, new byte[] {0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x01, 0x00}},
            {SevenZipFormat.Rar, new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00 }},
            {SevenZipFormat.Vhd, new byte[] { 0x63, 0x6F, 0x6E, 0x65, 0x63, 0x74, 0x69, 0x78 }},
            {SevenZipFormat.Deb, new byte[] { 0x21, 0x3C, 0x61, 0x72, 0x63, 0x68, 0x3E }},
            {SevenZipFormat.Dmg, new byte[] { 0x78, 0x01, 0x73, 0x0D, 0x62, 0x62, 0x60 }},
            {SevenZipFormat.SevenZip, new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C }},
            {SevenZipFormat.Tar, new byte[] { 0x75, 0x73, 0x74, 0x61, 0x72 }},
            {SevenZipFormat.Iso, new byte[] { 0x43, 0x44, 0x30, 0x30, 0x31 }},
            {SevenZipFormat.Cab, new byte[] { 0x4D, 0x53, 0x43, 0x46 }},
            {SevenZipFormat.Rpm, new byte[] { 0xed, 0xab, 0xee, 0xdb }},
            {SevenZipFormat.Xar, new byte[] { 0x78, 0x61, 0x72, 0x21 }},
            {SevenZipFormat.Chm, new byte[] { 0x49, 0x54, 0x53, 0x46 }},
            {SevenZipFormat.BZip2, new byte[] { 0x42, 0x5A, 0x68 }},
            {SevenZipFormat.Flv, new byte[] { 0x46, 0x4C, 0x56 }},
            {SevenZipFormat.Swf, new byte[] { 0x46, 0x57, 0x53 }},
            {SevenZipFormat.GZip, new byte[] { 0x1f, 0x0b }},
            {SevenZipFormat.Zip, new byte[] { 0x50, 0x4b }},
            {SevenZipFormat.Arj, new byte[] { 0x60, 0xEA }},
            {SevenZipFormat.Lzh, new byte[] { 0x2D, 0x6C, 0x68 }}
        };
    }
}