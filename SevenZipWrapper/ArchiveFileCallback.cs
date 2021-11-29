using System;
using System.IO;

namespace SevenZipExtractor
{
    internal class ArchiveFileCallback : IArchiveExtractCallback
    {
        private readonly string fileName;
        private readonly uint fileNumber;
        private OutStreamWrapper fileStream; // to be removed

        public ArchiveFileCallback(uint fileNumber, string fileName)
        {
            this.fileNumber = fileNumber;
            this.fileName = fileName;
        }

        public void SetTotal(ulong total)
        {
        }

        public void SetCompleted(ref ulong completeValue)
        {
        }

        public int GetStream(uint index, out ISequentialOutStream outStream, AskMode askExtractMode)
        {
            if ((index != this.fileNumber) || (askExtractMode != AskMode.kExtract))
            {
                outStream = null;
                return 0;
            }

            string fileDir = Path.GetDirectoryName(this.fileName);

            if (!string.IsNullOrEmpty(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }

            this.fileStream = new OutStreamWrapper(File.Create(this.fileName));

            outStream = this.fileStream;

            return 0;
        }

        public void PrepareOperation(AskMode askExtractMode)
        {
        }

        public void SetOperationResult(OperationResult resultEOperationResult)
        {
            this.fileStream.Dispose();
        }
    }
}