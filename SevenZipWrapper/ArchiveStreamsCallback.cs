using System.Collections.Generic;
using System.IO;

namespace SevenZipExtractor
{
    internal class ArchiveStreamsCallback : IArchiveExtractCallback
    {
        private readonly IList<Stream> streams;

        public ArchiveStreamsCallback(IList<Stream> streams) 
        {
            this.streams = streams;
        }

        public void SetTotal(ulong total)
        {
        }

        public void SetCompleted(ref ulong completeValue)
        {
        }

        public int GetStream(uint index, out ISequentialOutStream outStream, AskMode askExtractMode)
        {
            if (askExtractMode != AskMode.kExtract)
            {
                outStream = null;
                return 0;
            }

            if (this.streams == null)
            {
                outStream = null;
                return 0;
            }

            Stream stream = this.streams[(int) index];

            if (stream == null)
            {
                outStream = null;
                return 0;
            }

            outStream = new OutStreamWrapper(stream);

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