using Haggis.Enums;
using Haggis.Model;

namespace Haggis.Extentions
{
    public static class RankExtention
    {
        public static Card ToCard(this Rank rank)
        {
            return new Card(rank);
        }

        public static string ToLetter(this Rank rank)
        {
            if (rank.Equals(Rank.JACK))
                return "J";
            if (rank.Equals(Rank.QUEEN))
                return "Q";
            if (rank.Equals(Rank.KING))
                return "K";
            return "XXX";
        }
    }
}
