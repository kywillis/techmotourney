namespace TecmoTourney.Bracket
{
    /// <summary>
    /// Server-side mirror of <c>double-elim-bracket.builder.ts</c> (leaf ranks / WB round 1 only).
    /// </summary>
    public static class DoubleEliminationBracket
    {
        public static int BracketSizeForEntrantCount(int n)
        {
            if (n < 4)
                return 4;
            var b = 4;
            while (b < n && b < 32)
                b *= 2;
            if (b < n)
                b = 32;
            return b;
        }

        public static int[] BracketLeafRanks(int size)
        {
            if (size <= 1)
                return new[] { 0 };
            var half = size / 2;
            var prev = BracketLeafRanks(half);
            var result = new int[size];
            var idx = 0;
            for (var i = 0; i < half; i++)
            {
                result[idx++] = prev[i];
                result[idx++] = size - 1 - prev[i];
            }
            return result;
        }

        /// <summary>0-based entrant ranks for WB round 1 pairings (both ranks &lt; entrantCount).</summary>
        public static List<(int RankA, int RankB)> GetFirstRoundWinnersBracketMatchupRanks(int entrantCount)
        {
            var n = entrantCount;
            var b = BracketSizeForEntrantCount(n);
            var leafRanks = BracketLeafRanks(b);
            var pairs = new List<(int, int)>();
            var matchesRound0 = b / 2;
            for (var i = 0; i < matchesRound0; i++)
            {
                var topRank = leafRanks[2 * i];
                var bottomRank = leafRanks[2 * i + 1];
                if (topRank >= n || bottomRank >= n)
                    continue;
                pairs.Add((topRank, bottomRank));
            }
            return pairs;
        }
    }
}
