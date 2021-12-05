namespace SevenZip.Sdk.Buffer
{
    using System.IO;

    /// <summary>
    /// Implements the input buffer work
    /// </summary>
    internal class InBuffer
    {
        private readonly byte[] m_Buffer;
        private readonly uint m_BufferSize;
        private uint m_Limit;
        private uint m_Pos;
        private ulong m_ProcessedSize;
        private Stream m_Stream;
        private bool m_StreamWasExhausted;

        /// <summary>
        /// Initializes the input buffer
        /// </summary>
        /// <param name="bufferSize"></param>
        private InBuffer(uint bufferSize)
        {
            m_Buffer = new byte[bufferSize];
            m_BufferSize = bufferSize;
        }

        /// <summary>
        /// Initializes the class
        /// </summary>
        /// <param name="stream"></param>
        private void Init(Stream stream)
        {
            m_Stream = stream;
            m_ProcessedSize = 0;
            m_Limit = 0;
            m_Pos = 0;
            m_StreamWasExhausted = false;
        }

        /// <summary>
        /// Reads the whole block
        /// </summary>
        /// <returns></returns>
        private bool ReadBlock()
        {
            if (m_StreamWasExhausted)
                return false;
            m_ProcessedSize += m_Pos;
            int aNumProcessedBytes = m_Stream.Read(m_Buffer, 0, (int) m_BufferSize);
            m_Pos = 0;
            m_Limit = (uint) aNumProcessedBytes;
            m_StreamWasExhausted = (aNumProcessedBytes == 0);
            return (!m_StreamWasExhausted);
        }

        /// <summary>
        /// Releases the stream
        /// </summary>
        private void ReleaseStream()
        {
            // m_Stream.Close(); 
            m_Stream = null;
        }

        /// <summary>
        /// Reads the byte to check it
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        private bool ReadByte(out byte b)
        {
            b = 0;
            if (m_Pos >= m_Limit)
                if (!ReadBlock())
                    return false;
            b = m_Buffer[m_Pos++];
            return true;
        }

        /// <summary>
        /// Reads the next byte
        /// </summary>
        /// <returns></returns>
        private byte ReadByte()
        {
            // return (byte)m_Stream.ReadByte();
            if (m_Pos >= m_Limit)
                if (!ReadBlock())
                    return 0xFF;
            return m_Buffer[m_Pos++];
        }

        /// <summary>
        /// Gets processed size
        /// </summary>
        /// <returns></returns>
        private ulong GetProcessedSize()
        {
            return m_ProcessedSize + m_Pos;
        }
    }
}