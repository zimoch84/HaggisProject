using Haggis.Domain.Enums;
using System;
using System.Collections.Generic;
using static Haggis.Domain.Enums.TrickType;

namespace Haggis.Domain.Extentions
{
    public static class TrickTypeExtention
    {
        private static readonly List<TrickType> sameCardTypes = new List<TrickType>() { SINGLE, PAIR, TRIPLE, QUAD, FIVED, SIXED };
        private static readonly List<TrickType> sequenceTypes = new List<TrickType>() { SEQ3, SEQ4, SEQ5, SEQ6, SEQ6, SEQ7 };
        private static readonly List<TrickType> sequenceOfPairs = new List<TrickType>() { PAIRSEQ2, PAIRSEQ3, PAIRSEQ4, PAIRSEQ5, PAIRSEQ6, PAIRSEQ7 };

        public static TrickClass Class(this TrickType type)
        {
            if (sameCardTypes.Contains(type))
            {
                return TrickClass.SAME_KIND;
            }

            if (sequenceTypes.Contains(type))
            {
                return TrickClass.SEQUENCE;
            }

            if (sequenceOfPairs.Contains(type))
            {
                return TrickClass.SEQUENCE_OF_PAIRS;
            }

            return TrickClass.ELSE;

        }

        public static int Quantity(this TrickType type) {
            
            if (sameCardTypes.Contains(type))
            {
                return (int)type % 10;
            }

            if (sequenceTypes.Contains(type))
            {
                return ((int)type -2) % 10;
            }

            if (sequenceOfPairs.Contains(type))
            {
                return ((int)type - 4) % 10;
            }

            return 0;
        }
        public static TrickType SeqByPair(this TrickType type)
        {
            switch (type)
            {
                case TrickType.PAIRSEQ2: return TrickType.SEQ2;
                case TrickType.PAIRSEQ3: return TrickType.SEQ3;
                case TrickType.PAIRSEQ4: return TrickType.SEQ4;
                case TrickType.PAIRSEQ5: return TrickType.SEQ5;
                case TrickType.PAIRSEQ6: return TrickType.SEQ6;
                case TrickType.SEQ2: return TrickType.PAIRSEQ2;
                case TrickType.SEQ3: return TrickType.PAIRSEQ3;
                case TrickType.SEQ4: return TrickType.PAIRSEQ4;
                case TrickType.SEQ5: return TrickType.PAIRSEQ5;
                case TrickType.SEQ6: return TrickType.PAIRSEQ6;
                default: return type;
            };
        }

        public static TrickType LesserTrick(this TrickType type)
        {
            int difference = (type >= TrickType.PAIRSEQ2 && type <= TrickType.PAIRSEQ7) ? 20 : 10;
            int lesserValue = (int)type - difference;
            if (Enum.IsDefined(typeof(TrickType), lesserValue))
            {
                return (TrickType)lesserValue;
            }
            return type;
        }
    }
}
