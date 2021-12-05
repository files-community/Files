namespace SevenZip.Sdk.Compression.LZ
{
    using System;
    using System.IO;

    /// <summary>
    /// Input window class
    /// </summary>
    internal class InWindow
    {
        /// <summary>
        /// Size of Allocated memory block
        /// </summary>
        public UInt32 _blockSize;

        /// <summary>
        /// The pointer to buffer with data
        /// </summary>
        public Byte[] _bufferBase;

        /// <summary>
        /// Buffer offset value
        /// </summary>
        public UInt32 _bufferOffset;

        /// <summary>
        /// How many BYTEs must be kept buffer after _pos
        /// </summary>
        private UInt32 _keepSizeAfter;

        /// <summary>
        /// How many BYTEs must be kept in buffer before _pos
        /// </summary>
        private UInt32 _keepSizeBefore;

        private UInt32 _pointerToLastSafePosition;

        /// <summary>
        /// Offset (from _buffer) of curent byte
        /// </summary>
        public UInt32 _pos;

        private UInt32 _posLimit; // offset (from _buffer) of first byte when new block reading must be done
        private Stream _stream;
        private bool _streamEndWasReached; // if (true) then _streamPos shows real end of stream

        /// <summary>
        /// Offset (from _buffer) of first not read byte from Stream
        /// </summary>
        public UInt32 _streamPos;

        public void MoveBlock()
        {
            UInt32 offset = (_bufferOffset) + _pos - _keepSizeBefore;
            // we need one additional byte, since MovePos moves on 1 byte.
            if (offset > 0)
                offset--;

            UInt32 numBytes = (_bufferOffset) + _streamPos - offset;

            // check negative offset ????
            for (UInt32 i = 0; i < numBytes; i++)
                _bufferBase[i] = _bufferBase[offset + i];
            _bufferOffset -= offset;
        }

        public virtual void ReadBlock()
        {
            if (_streamEndWasReached)
                return;
            while (true)
            {
                var size = (int) ((0 - _bufferOffset) + _blockSize - _streamPos);
                if (size == 0)
                    return;
                int numReadBytes = _stream.Read(_bufferBase, (int) (_bufferOffset + _streamPos), size);
                if (numReadBytes == 0)
                {
                    _posLimit = _streamPos;
                    UInt32 pointerToPostion = _bufferOffset + _posLimit;
                    if (pointerToPostion > _pointerToLastSafePosition)
                        _posLimit = (_pointerToLastSafePosition - _bufferOffset);

                    _streamEndWasReached = true;
                    return;
                }
                _streamPos += (UInt32) numReadBytes;
                if (_streamPos >= _pos + _keepSizeAfter)
                    _posLimit = _streamPos - _keepSizeAfter;
            }
        }

        private void Free()
        {
            _bufferBase = null;
        }

        public void Create(UInt32 keepSizeBefore, UInt32 keepSizeAfter, UInt32 keepSizeReserv)
        {
            _keepSizeBefore = keepSizeBefore;
            _keepSizeAfter = keepSizeAfter;
            UInt32 blockSize = keepSizeBefore + keepSizeAfter + keepSizeReserv;
            if (_bufferBase == null || _blockSize != blockSize)
            {
                Free();
                _blockSize = blockSize;
                _bufferBase = new Byte[_blockSize];
            }
            _pointerToLastSafePosition = _blockSize - keepSizeAfter;
        }

        public void SetStream(Stream stream)
        {
            _stream = stream;
        }

        public void ReleaseStream()
        {
            _stream = null;
        }

        public void Init()
        {
            _bufferOffset = 0;
            _pos = 0;
            _streamPos = 0;
            _streamEndWasReached = false;
            ReadBlock();
        }

        public void MovePos()
        {
            _pos++;
            if (_pos > _posLimit)
            {
                UInt32 pointerToPostion = _bufferOffset + _pos;
                if (pointerToPostion > _pointerToLastSafePosition)
                    MoveBlock();
                ReadBlock();
            }
        }

        public Byte GetIndexByte(Int32 index)
        {
            return _bufferBase[_bufferOffset + _pos + index];
        }

        /// <summary>
        /// index + limit have not to exceed _keepSizeAfter
        /// </summary>
        /// <param name="index"></param>
        /// <param name="distance"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        public UInt32 GetMatchLen(Int32 index, UInt32 distance, UInt32 limit)
        {
            if (_streamEndWasReached)
                if ((_pos + index) + limit > _streamPos)
                    limit = _streamPos - (UInt32) (_pos + index);
            distance++;
            // Byte *pby = _buffer + (size_t)_pos + index;
            UInt32 pby = _bufferOffset + _pos + (UInt32) index;

            UInt32 i;
            for (i = 0; i < limit && _bufferBase[pby + i] == _bufferBase[pby + i - distance]; i++) ;
            return i;
        }

        public UInt32 GetNumAvailableBytes()
        {
            return _streamPos - _pos;
        }

        public void ReduceOffsets(Int32 subValue)
        {
            _bufferOffset += (UInt32) subValue;
            _posLimit -= (UInt32) subValue;
            _pos -= (UInt32) subValue;
            _streamPos -= (UInt32) subValue;
        }
    }
}