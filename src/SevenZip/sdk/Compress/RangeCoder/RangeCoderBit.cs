namespace SevenZip.Sdk.Compression.RangeCoder
{
    using System;

    internal struct BitEncoder
    {
        public const uint kBitModelTotal = (1 << kNumBitModelTotalBits);
        public const int kNumBitModelTotalBits = 11;
        public const int kNumBitPriceShiftBits = 6;
        private const int kNumMoveBits = 5;
        private const int kNumMoveReducingBits = 2;
        private static readonly UInt32[] ProbPrices = new UInt32[kBitModelTotal >> kNumMoveReducingBits];

        private uint Prob;

        static BitEncoder()
        {
            const int kNumBits = (kNumBitModelTotalBits - kNumMoveReducingBits);
            for (int i = kNumBits - 1; i >= 0; i--)
            {
                UInt32 start = (UInt32) 1 << (kNumBits - i - 1);
                UInt32 end = (UInt32) 1 << (kNumBits - i);
                for (UInt32 j = start; j < end; j++)
                    ProbPrices[j] = ((UInt32) i << kNumBitPriceShiftBits) +
                                    (((end - j) << kNumBitPriceShiftBits) >> (kNumBits - i - 1));
            }
        }

        public void Init()
        {
            Prob = kBitModelTotal >> 1;
        }

        /*public void UpdateModel(uint symbol)
		{
			if (symbol == 0)
				Prob += (kBitModelTotal - Prob) >> kNumMoveBits;
			else
				Prob -= (Prob) >> kNumMoveBits;
		}*/

        public void Encode(Encoder encoder, uint symbol)
        {
            // encoder.EncodeBit(Prob, kNumBitModelTotalBits, symbol);
            // UpdateModel(symbol);
            uint newBound = (encoder.Range >> kNumBitModelTotalBits)*Prob;
            if (symbol == 0)
            {
                encoder.Range = newBound;
                Prob += (kBitModelTotal - Prob) >> kNumMoveBits;
            }
            else
            {
                encoder.Low += newBound;
                encoder.Range -= newBound;
                Prob -= (Prob) >> kNumMoveBits;
            }
            if (encoder.Range < Encoder.kTopValue)
            {
                encoder.Range <<= 8;
                encoder.ShiftLow();
            }
        }

        public uint GetPrice(uint symbol)
        {
            return ProbPrices[(((Prob - symbol) ^ ((-(int) symbol))) & (kBitModelTotal - 1)) >> kNumMoveReducingBits];
        }

        public uint GetPrice0()
        {
            return ProbPrices[Prob >> kNumMoveReducingBits];
        }

        public uint GetPrice1()
        {
            return ProbPrices[(kBitModelTotal - Prob) >> kNumMoveReducingBits];
        }
    }

    internal struct BitDecoder
    {
        public const uint kBitModelTotal = (1 << kNumBitModelTotalBits);
        public const int kNumBitModelTotalBits = 11;
        private const int kNumMoveBits = 5;

        private uint Prob;

        /*public void UpdateModel(int numMoveBits, uint symbol)
		{
			if (symbol == 0)
				Prob += (kBitModelTotal - Prob) >> numMoveBits;
			else
				Prob -= (Prob) >> numMoveBits;
		}*/

        public void Init()
        {
            Prob = kBitModelTotal >> 1;
        }

        public uint Decode(Decoder rangeDecoder)
        {
            uint newBound = (rangeDecoder.Range >> kNumBitModelTotalBits)*Prob;
            if (rangeDecoder.Code < newBound)
            {
                rangeDecoder.Range = newBound;
                Prob += (kBitModelTotal - Prob) >> kNumMoveBits;
                if (rangeDecoder.Range < Decoder.kTopValue)
                {
                    rangeDecoder.Code = (rangeDecoder.Code << 8) | (byte) rangeDecoder.Stream.ReadByte();
                    rangeDecoder.Range <<= 8;
                }
                return 0;
            }
            else
            {
                rangeDecoder.Range -= newBound;
                rangeDecoder.Code -= newBound;
                Prob -= (Prob) >> kNumMoveBits;
                if (rangeDecoder.Range < Decoder.kTopValue)
                {
                    rangeDecoder.Code = (rangeDecoder.Code << 8) | (byte) rangeDecoder.Stream.ReadByte();
                    rangeDecoder.Range <<= 8;
                }
                return 1;
            }
        }
    }
}