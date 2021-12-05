namespace SevenZip.Sdk.Buffer
{
    using System.IO;

    internal class OutBuffer
    {
        private readonly byte[] m_Buffer;
        private readonly uint m_BufferSize;
        private uint m_Pos;
        private ulong m_ProcessedSize;
        private Stream m_Stream;

        /// <summary>
        /// Initializes a new instance of the OutBuffer class
        /// </summary>
        /// <param name="bufferSize"></param>
        public OutBuffer(uint bufferSize)
        {
            m_Buffer = new byte[bufferSize];
            m_BufferSize = bufferSize;
        }

        public void SetStream(Stream stream)
        {
            m_Stream = stream;
        }

        public void FlushStream()
        {
            m_Stream.Flush();
        }

        public void CloseStream()
        {
            m_Stream.Close();
        }

        public void ReleaseStream()
        {
            m_Stream = null;
        }

        public void Init()
        {
            m_ProcessedSize = 0;
            m_Pos = 0;
        }

        public void WriteByte(byte b)
        {
            m_Buffer[m_Pos++] = b;
            if (m_Pos >= m_BufferSize)
                FlushData();
        }

        public void FlushData()
        {
            if (m_Pos == 0)
                return;
            m_Stream.Write(m_Buffer, 0, (int) m_Pos);
            m_Pos = 0;
        }

        public ulong GetProcessedSize()
        {
            return m_ProcessedSize + m_Pos;
        }
    }
}