namespace SevenZip.Sdk.Compression.Lzma
{
    internal abstract class Base
    {
        public const uint kAlignMask = (kAlignTableSize - 1);
        public const uint kAlignTableSize = 1 << kNumAlignBits;
        public const int kDicLogSizeMin = 0;
        public const uint kEndPosModelIndex = 14;
        public const uint kMatchMaxLen = kMatchMinLen + kNumLenSymbols - 1;
        // public const int kDicLogSizeMax = 30;
        // public const uint kDistTableSizeMax = kDicLogSizeMax * 2;

        public const uint kMatchMinLen = 2;

        public const int kNumAlignBits = 4;

        public const uint kNumFullDistances = 1 << ((int) kEndPosModelIndex/2);
        public const int kNumHighLenBits = 8;

        public const uint kNumLenSymbols = kNumLowLenSymbols + kNumMidLenSymbols +
                                           (1 << kNumHighLenBits);

        public const uint kNumLenToPosStates = 1 << kNumLenToPosStatesBits;
        public const int kNumLenToPosStatesBits = 2; // it's for speed optimization

        public const uint kNumLitContextBitsMax = 8;
        public const uint kNumLitPosStatesBitsEncodingMax = 4;

        public const int kNumLowLenBits = 3;
        public const uint kNumLowLenSymbols = 1 << kNumLowLenBits;
        public const int kNumMidLenBits = 3;
        public const uint kNumMidLenSymbols = 1 << kNumMidLenBits;
        public const uint kNumPosModels = kEndPosModelIndex - kStartPosModelIndex;
        public const int kNumPosSlotBits = 6;
        public const int kNumPosStatesBitsEncodingMax = 4;
        public const int kNumPosStatesBitsMax = 4;
        public const uint kNumPosStatesEncodingMax = (1 << kNumPosStatesBitsEncodingMax);
        public const uint kNumPosStatesMax = (1 << kNumPosStatesBitsMax);
        public const uint kNumRepDistances = 4;
        public const uint kNumStates = 12;
        public const uint kStartPosModelIndex = 4;

        public static uint GetLenToPosState(uint len)
        {
            len -= kMatchMinLen;
            if (len < kNumLenToPosStates)
                return len;
            return (kNumLenToPosStates - 1);
        }

        #region Nested type: State

        public struct State
        {
            public uint Index;

            public void Init()
            {
                Index = 0;
            }

            public void UpdateChar()
            {
                if (Index < 4) Index = 0;
                else if (Index < 10) Index -= 3;
                else Index -= 6;
            }

            public void UpdateMatch()
            {
                Index = (uint) (Index < 7 ? 7 : 10);
            }

            public void UpdateRep()
            {
                Index = (uint) (Index < 7 ? 8 : 11);
            }

            public void UpdateShortRep()
            {
                Index = (uint) (Index < 7 ? 9 : 11);
            }

            public bool IsCharState()
            {
                return Index < 7;
            }
        }

        #endregion
    }
}