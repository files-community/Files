using System;
using System.IO;

namespace SevenZipExtractor
{
    public class ZipEntry
    {
        private readonly IInArchive archive;
        private readonly uint index;

        internal ZipEntry(IInArchive archive, uint index)
        {
            this.archive = archive;
            this.index = index;
        }

        /// <summary>
        /// Name of the file with its relative path within the archive
        /// </summary>
        public string FileName { get; internal set; }
        /// <summary>
        /// True if entry is a folder, false if it is a file
        /// </summary>
        public bool IsFolder { get; internal set; }
        /// <summary>
        /// Original entry size
        /// </summary>
        public ulong Size { get; internal set; }
        /// <summary>
        /// Entry size in a archived state
        /// </summary>
        public ulong PackedSize { get; internal set; }

        /// <summary>
        /// Date and time of the file (entry) creation
        /// </summary>
        public DateTime CreationTime { get; internal set; }

        /// <summary>
        /// Date and time of the last change of the file (entry)
        /// </summary>
        public DateTime LastWriteTime { get; internal set; }

        /// <summary>
        /// Date and time of the last access of the file (entry)
        /// </summary>
        public DateTime LastAccessTime { get; internal set; }
        
        /// <summary>
        /// CRC hash of the entry
        /// </summary>
        public UInt32 CRC { get; internal set; }

        /// <summary>
        /// Attributes of the entry
        /// </summary>
        public UInt32 Attributes { get; internal set; }

        /// <summary>
        /// True if entry is encrypted, otherwise false
        /// </summary>
        public bool IsEncrypted { get; internal set; }

        /// <summary>
        /// Comment of the entry
        /// </summary>
        public string Comment { get; internal set; }

        /// <summary>
        /// Compression method of the entry
        /// </summary>
        public string Method { get; internal set; }

        /// <summary>
        /// Host operating system of the entry
        /// </summary>
        public string HostOS { get; internal set; }

        /// <summary>
        /// True if there are parts of this file in previous split archive parts
        /// </summary>
        public bool IsSplitBefore { get; set; }

        /// <summary>
        /// True if there are parts of this file in next split archive parts
        /// </summary>
        public bool IsSplitAfter { get; set; }

        public void Extract(string fileName, bool preserveTimestamp = true)
        {
            if (this.IsFolder)
            {
                Directory.CreateDirectory(fileName);
                return;
            }

            string directoryName = Path.GetDirectoryName(fileName);

            if (!string.IsNullOrWhiteSpace(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            using (FileStream fileStream = File.Create(fileName))
            {
                this.Extract(fileStream);
            }

            if (preserveTimestamp)
            {
                File.SetLastWriteTime(fileName, this.LastWriteTime);
            }
        }
        public void Extract(Stream stream)
        {
            this.archive.Extract(new[] { this.index }, 1, 0, new ArchiveStreamCallback(this.index, stream));
        }
    }
}
