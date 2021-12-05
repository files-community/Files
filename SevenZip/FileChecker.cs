namespace SevenZip
{
    using System;
    using System.IO;

#if UNMANAGED
    /// <summary>
    /// The signature checker class. Original code by Siddharth Uppal, adapted by Markhor.
    /// </summary>
    /// <remarks>Based on the code at http://blog.somecreativity.com/2008/04/08/how-to-check-if-a-file-is-compressed-in-c/#</remarks>
    internal static class FileChecker
    {
        private const int SIGNATURE_SIZE = 21;
        private const int SFX_SCAN_LENGTH = 256 * 1024;

        private static bool SpecialDetect(Stream stream, int offset, InArchiveFormat expectedFormat)
        {
            if (stream.Length > offset + SIGNATURE_SIZE)
            {
                var signature = new byte[SIGNATURE_SIZE];
                var bytesRequired = SIGNATURE_SIZE;
                var index = 0;
                stream.Seek(offset, SeekOrigin.Begin);

                while (bytesRequired > 0)
                {
                    var bytesRead = stream.Read(signature, index, bytesRequired);
                    bytesRequired -= bytesRead;
                    index += bytesRead;
                }

                var actualSignature = BitConverter.ToString(signature);

                foreach (var expectedSignature in Formats.InSignatureFormats.Keys)
                {
                    if (Formats.InSignatureFormats[expectedSignature] != expectedFormat)
                    {
                        continue;
                    }

                    if (actualSignature.StartsWith(expectedSignature, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the InArchiveFormat for a specific extension.
        /// </summary>
        /// <param name="stream">The stream to identify.</param>
        /// <param name="offset">The archive beginning offset.</param>
        /// <param name="isExecutable">True if the original format of the stream is PE; otherwise, false.</param>
        /// <returns>Corresponding InArchiveFormat.</returns>
        public static InArchiveFormat CheckSignature(Stream stream, out int offset, out bool isExecutable)
        {
            offset = 0;

            if (!stream.CanRead)
            {
                throw new ArgumentException("The stream must be readable.");
            }

            if (stream.Length < SIGNATURE_SIZE)
            {
                throw new ArgumentException("The stream is invalid.");
            }

            #region Get file signature

            var signature = new byte[SIGNATURE_SIZE];
            var bytesRequired = SIGNATURE_SIZE;
            var index = 0;
            stream.Seek(0, SeekOrigin.Begin);

            while (bytesRequired > 0)
            {
                var bytesRead = stream.Read(signature, index, bytesRequired);
                bytesRequired -= bytesRead;
                index += bytesRead;
            }

            var actualSignature = BitConverter.ToString(signature);

            #endregion

            var suspectedFormat = InArchiveFormat.XZ; // any except PE and Cab
            isExecutable = false;

            foreach (var expectedSignature in Formats.InSignatureFormats.Keys)
            {
                if (actualSignature.StartsWith(expectedSignature, StringComparison.OrdinalIgnoreCase) ||
                    actualSignature.Substring(6).StartsWith(expectedSignature, StringComparison.OrdinalIgnoreCase) &&
                    Formats.InSignatureFormats[expectedSignature] == InArchiveFormat.Lzh)
                {
                    if (Formats.InSignatureFormats[expectedSignature] == InArchiveFormat.PE)
                    {
                        suspectedFormat = InArchiveFormat.PE;
                        isExecutable = true;
                    }
                    else
                    {
                        return Formats.InSignatureFormats[expectedSignature];
                    }
                }
            }

            // Many Microsoft formats
            if (actualSignature.StartsWith("D0-CF-11-E0-A1-B1-1A-E1", StringComparison.OrdinalIgnoreCase))
            {
                suspectedFormat = InArchiveFormat.Cab; // != InArchiveFormat.XZ
            }

            #region SpecialDetect

            try
            {
                SpecialDetect(stream, 257, InArchiveFormat.Tar);
            }
            catch (ArgumentException) {}

            if (SpecialDetect(stream, 0x8001, InArchiveFormat.Iso))
            {
                return InArchiveFormat.Iso;
            }

            if (SpecialDetect(stream, 0x8801, InArchiveFormat.Iso))
            {
                return InArchiveFormat.Iso;
            }

            if (SpecialDetect(stream, 0x9001, InArchiveFormat.Iso))
            {
                return InArchiveFormat.Iso;
            }

            if (SpecialDetect(stream, 0x9001, InArchiveFormat.Iso))
            {
                return InArchiveFormat.Iso;
            }

            if (SpecialDetect(stream, 0x400, InArchiveFormat.Hfs))
            {
                return InArchiveFormat.Hfs;
            }

            #region Last resort for tar - can mistake

            if (stream.Length >= 1024)
            {
                stream.Seek(-1024, SeekOrigin.End);
                var buf = new byte[1024];
                stream.Read(buf, 0, 1024);
                var isTar = true;

                for (var i = 0; i < 1024; i++)
                {
                    isTar = isTar && buf[i] == 0;
                }

                if (isTar)
                {
                    return InArchiveFormat.Tar;
                }
            }

            #endregion

            #endregion

            #region Check if it is an SFX archive or a file with an embedded archive.

            if (suspectedFormat != InArchiveFormat.XZ)
            {
                #region Get first Min(stream.Length, SFX_SCAN_LENGTH) bytes

                var scanLength = Math.Min(stream.Length, SFX_SCAN_LENGTH);
                signature = new byte[scanLength];
                bytesRequired = (int)scanLength;
                index = 0;
                stream.Seek(0, SeekOrigin.Begin);

                while (bytesRequired > 0)
                {
                    var bytesRead = stream.Read(signature, index, bytesRequired);
                    bytesRequired -= bytesRead;
                    index += bytesRead;
                }

                actualSignature = BitConverter.ToString(signature);

                #endregion

                foreach (var format in new[] 
                {
                    InArchiveFormat.Zip, 
                    InArchiveFormat.SevenZip,
                    InArchiveFormat.Rar4,
                    InArchiveFormat.Rar,
                    InArchiveFormat.Cab,
                    InArchiveFormat.Arj
                })
                {
                    var pos = actualSignature.IndexOf(Formats.InSignatureFormatsReversed[format]);

                    if (pos > -1)
                    {
                        offset = pos / 3;
                        return format;
                    }
                }

                // Nothing
                if (suspectedFormat == InArchiveFormat.PE)
                {
                    return InArchiveFormat.PE;
                }
            }

            #endregion

            throw new ArgumentException("The stream is invalid or no corresponding signature was found.");
        }

        /// <summary>
        /// Gets the InArchiveFormat for a specific file name.
        /// </summary>
        /// <param name="fileName">The archive file name.</param>
        /// <param name="offset">The archive beginning offset.</param>
        /// <param name="isExecutable">True if the original format of the file is PE; otherwise, false.</param>
        /// <returns>Corresponding InArchiveFormat.</returns>
        /// <exception cref="System.ArgumentException"/>
        public static InArchiveFormat CheckSignature(string fileName, out int offset, out bool isExecutable)
        {
            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                try
                {
                    return CheckSignature(fs, out offset, out isExecutable);
                }
                catch (ArgumentException)
                {
                    offset = 0;
                    isExecutable = false;
                    return Formats.FormatByFileName(fileName, true);
                }
            }
        }
    }
#endif
}