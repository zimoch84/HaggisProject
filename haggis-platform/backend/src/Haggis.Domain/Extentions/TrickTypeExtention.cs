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
        private static readonly List<TrickType> sequenceOfTriples = new List<TrickType>() { TRIPLESTAIR2, TRIPLESTAIR3, TRIPLESTAIR4, TRIPLESTAIR5 };
        private static readonly List<TrickType> sequenceOfQuadriples = new List<TrickType>() { QUADSTAIR2, QUADSTAIR3, QUADSTAIR4 };
        private static readonly List<TrickType> sequenceOfFiveds = new List<TrickType>() { FIVEDSTAIR2, FIVEDSTAIR3 };

        private static readonly Dictionary<TrickType, int> stairGroupSizes = new Dictionary<TrickType, int>
        {
            { PAIRSEQ2, 2 }, { PAIRSEQ3, 2 }, { PAIRSEQ4, 2 }, { PAIRSEQ5, 2 }, { PAIRSEQ6, 2 }, { PAIRSEQ7, 2 },
            { TRIPLESTAIR2, 3 }, { TRIPLESTAIR3, 3 }, { TRIPLESTAIR4, 3 }, { TRIPLESTAIR5, 3 },
            { QUADSTAIR2, 4 }, { QUADSTAIR3, 4 }, { QUADSTAIR4, 4 },
            { FIVEDSTAIR2, 5 }, { FIVEDSTAIR3, 5 }
        };

        private static readonly Dictionary<TrickType, int> stairLengths = new Dictionary<TrickType, int>
        {
            { PAIRSEQ2, 2 }, { PAIRSEQ3, 3 }, { PAIRSEQ4, 4 }, { PAIRSEQ5, 5 }, { PAIRSEQ6, 6 }, { PAIRSEQ7, 7 },
            { TRIPLESTAIR2, 2 }, { TRIPLESTAIR3, 3 }, { TRIPLESTAIR4, 4 }, { TRIPLESTAIR5, 5 },
            { QUADSTAIR2, 2 }, { QUADSTAIR3, 3 }, { QUADSTAIR4, 4 },
            { FIVEDSTAIR2, 2 }, { FIVEDSTAIR3, 3 }
        };

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

            if (sequenceOfTriples.Contains(type))
            {
                return TrickClass.SEQUENCE_OF_TRIPLES;
            }

            if (sequenceOfQuadriples.Contains(type))
            {
                return TrickClass.SEQUENCE_OF_QUADRUPLES;
            }

            if (sequenceOfFiveds.Contains(type))
            {
                return TrickClass.SEQUENCE_OF_FIVED;
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

            if (stairLengths.TryGetValue(type, out var quantity))
            {
                return quantity;
            }

            return 0;
        }

        public static bool IsStair(this TrickType type)
        {
            return stairGroupSizes.ContainsKey(type);
        }

        public static int StairGroupSize(this TrickType type)
        {
            return stairGroupSizes.TryGetValue(type, out var groupSize) ? groupSize : 0;
        }

        public static int StairLength(this TrickType type)
        {
            return stairLengths.TryGetValue(type, out var length) ? length : 0;
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
            int difference = type.IsStair() ? 20 : 10;
            int lesserValue = (int)type - difference;
            if (Enum.IsDefined(typeof(TrickType), lesserValue))
            {
                return (TrickType)lesserValue;
            }
            return type;
        }
    }
}
