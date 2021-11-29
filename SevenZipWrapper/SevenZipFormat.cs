namespace SevenZipExtractor
{
    /// <summary>
    /// 
    /// </summary>
    public enum SevenZipFormat
    {
        // Default invalid format value
        Undefined = 0,

        /// <summary>
        /// Open 7-zip archive format.
        /// </summary>  
        /// <remarks><a href="http://en.wikipedia.org/wiki/7-zip">Wikipedia information</a></remarks> 
        SevenZip,

        /// <summary>
        /// Proprietary Arj archive format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/ARJ">Wikipedia information</a></remarks>
        Arj,

        /// <summary>
        /// Open Bzip2 archive format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Bzip2">Wikipedia information</a></remarks>
        BZip2,

        /// <summary>
        /// Microsoft cabinet archive format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Cabinet_(file_format)">Wikipedia information</a></remarks>
        Cab,

        /// <summary>
        /// Microsoft Compiled HTML Help file format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Microsoft_Compiled_HTML_Help">Wikipedia information</a></remarks>
        Chm,

        /// <summary>
        /// Microsoft Compound file format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Compound_File_Binary_Format">Wikipedia information</a></remarks>
        Compound,

        /// <summary>
        /// Open Cpio archive format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Cpio">Wikipedia information</a></remarks>
        Cpio,

        /// <summary>
        /// Open Debian software package format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Deb_(file_format)">Wikipedia information</a></remarks>
        Deb,

        /// <summary>
        /// Open Gzip archive format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Gzip">Wikipedia information</a></remarks>
        GZip,

        /// <summary>
        /// Open ISO disk image format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/ISO_image">Wikipedia information</a></remarks>
        Iso,

        /// <summary>
        /// Open Lzh archive format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Lzh">Wikipedia information</a></remarks>
        Lzh,

        /// <summary>
        /// Open core 7-zip Lzma raw archive format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Lzma">Wikipedia information</a></remarks>
        Lzma,

        /// <summary>
        /// Nullsoft installation package format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/NSIS">Wikipedia information</a></remarks>
        Nsis,

        /// <summary>
        /// RarLab Rar archive format.
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/RAR_(file_format)">Wikipedia information</a></remarks>
        Rar,

        /// <summary>
        /// RarLab Rar archive format, version 5.
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/RAR_(file_format)">Wikipedia information</a></remarks>
        Rar5,

        /// <summary>
        /// Open Rpm software package format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/RPM_Package_Manager">Wikipedia information</a></remarks>
        Rpm,

        /// <summary>
        /// Open split file format.
        /// </summary>
        /// <remarks><a href="?">Wikipedia information</a></remarks>
        Split,

        /// <summary>
        /// Open Tar archive format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Tar_(file_format)">Wikipedia information</a></remarks>
        Tar,

        /// <summary>
        /// Microsoft Windows Imaging disk image format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Windows_Imaging_Format">Wikipedia information</a></remarks>
        Wim,

        /// <summary>
        /// Open LZW archive format; implemented in "compress" program; also known as "Z" archive format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Compress">Wikipedia information</a></remarks>
        Lzw,

        /// <summary>
        /// Open Zip archive format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/ZIP_(file_format)">Wikipedia information</a></remarks>
        Zip,

        /// <summary>
        /// Open Udf disk image format.
        /// </summary>
        Udf,

        /// <summary>
        /// Xar open source archive format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Xar_(archiver)">Wikipedia information</a></remarks>
        Xar,

        /// <summary>
        /// Mub
        /// </summary>
        Mub,

        /// <summary>
        /// Macintosh Disk Image on CD.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/HFS_Plus">Wikipedia information</a></remarks>
        Hfs,

        /// <summary>
        /// Apple Mac OS X Disk Copy Disk Image format.
        /// </summary>
        Dmg,

        /// <summary>
        /// Open Xz archive format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Xz">Wikipedia information</a></remarks>        
        XZ,

        /// <summary>
        /// MSLZ archive format.
        /// </summary>
        Mslz,

        /// <summary>
        /// Flash video format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Flv">Wikipedia information</a></remarks>
        Flv,

        /// <summary>
        /// Shockwave Flash format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Swf">Wikipedia information</a></remarks>         
        Swf,

        /// <summary>
        /// Windows PE executable format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Portable_Executable">Wikipedia information</a></remarks>
        PE,

        /// <summary>
        /// Linux executable Elf format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Executable_and_Linkable_Format">Wikipedia information</a></remarks>
        Elf,

        /// <summary>
        /// Windows Installer Database.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Windows_Installer">Wikipedia information</a></remarks>
        Msi,

        /// <summary>
        /// Microsoft virtual hard disk file format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/VHD_%28file_format%29">Wikipedia information</a></remarks>
        Vhd,

        /// <summary>
        /// SquashFS file system format.
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/SquashFS">Wikipedia information</a></remarks>
        SquashFS,

        /// <summary>
        /// Lzma86 file format.
        /// </summary>
        Lzma86,

        /// <summary>
        /// Prediction by Partial Matching by Dmitry algorithm.
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/Prediction_by_partial_matching">Wikipedia information</a></remarks>
        Ppmd,

        /// <summary>
        /// TE format.
        /// </summary>
        TE,

        /// <summary>
        /// UEFIc format.
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/Unified_Extensible_Firmware_Interface">Wikipedia information</a></remarks>
        UEFIc,

        /// <summary>
        /// UEFIs format.
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/Unified_Extensible_Firmware_Interface">Wikipedia information</a></remarks>
        UEFIs,

        /// <summary>
        /// Compressed ROM file system format.
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/Cramfs">Wikipedia information</a></remarks>
        CramFS,

        /// <summary>
        /// APM format.
        /// </summary>
        APM,

        /// <summary>
        /// Swfc format.
        /// </summary>
        Swfc,

        /// <summary>
        /// NTFS file system format.
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/NTFS">Wikipedia information</a></remarks>
        Ntfs,

        /// <summary>
        /// FAT file system format.
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/File_Allocation_Table">Wikipedia information</a></remarks>
        Fat,

        /// <summary>
        /// MBR format.
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/Master_boot_record">Wikipedia information</a></remarks>
        Mbr,

        /// <summary>
        /// Mach-O file format.
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/Mach-O">Wikipedia information</a></remarks>
        MachO
    }
}