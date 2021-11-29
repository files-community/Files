using System.IO;

namespace SevenZipExtractor
{
    internal class ArchiveStreamCallback : IArchiveExtractCallback
    {
        private readonly uint fileNumber;
        private readonly Stream stream;

        public ArchiveStreamCallback(uint fileNumber, Stream stream)
        {
            this.fileNumber = fileNumber;
            this.stream = stream;
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

            outStream = new OutStreamWrapper(this.stream);

            return 0;
        }

        public void PrepareOperation(AskMode askExtractMode)
        {
        }

        public void SetOperationResult(OperationResult resultEOperationResult)
        {
        }
    }
}