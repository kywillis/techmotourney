namespace TecmoTourney.Bracket;

/// <summary>Server-side mirror of <c>double-elim-bracket.builder.ts</c>.</summary>
public static class DoubleElimBracketBuilder
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

    private static int LbRoundMatchCount(int b, int j)
    {
        if (j == 0)
            return b / 4;
        if (j % 2 == 1)
            return (int)(b / Math.Pow(2, (j + 1) / 2 + 1));
        return (int)(b / Math.Pow(2, j / 2 + 2));
    }

    private static FeedRef WbLeafFeed(int n, int[] leafRanks, int leafIndex)
    {
        var rank = leafRanks[leafIndex];
        if (rank >= n)
            return new FeedRef { Kind = FeedKind.Bye };
        return new FeedRef { Kind = FeedKind.Seed, SeedSlot = rank };
    }

    public static (int B, List<BracketMatch> Matches) BuildDoubleEliminationMatches(int entrantCount)
    {
        var n = entrantCount;
        var b = BracketSizeForEntrantCount(n);
        var k = (int)Math.Log2(b);
        var leafRanks = BracketLeafRanks(b);
        var matches = new List<BracketMatch>();

        for (var r = 0; r < k; r++)
        {
            var count = (int)(b / Math.Pow(2, r + 1));
            for (var i = 0; i < count; i++)
            {
                var top = r == 0
                    ? WbLeafFeed(n, leafRanks, 2 * i)
                    : new FeedRef { Kind = FeedKind.Winner, MatchId = $"wb-{r - 1}-{2 * i}" };
                var bottom = r == 0
                    ? WbLeafFeed(n, leafRanks, 2 * i + 1)
                    : new FeedRef { Kind = FeedKind.Winner, MatchId = $"wb-{r - 1}-{2 * i + 1}" };
                matches.Add(new BracketMatch
                {
                    Id = $"wb-{r}-{i}",
                    Segment = "WB",
                    Round = r,
                    IndexInRound = i,
                    Top = top,
                    Bottom = bottom
                });
            }
        }

        var lbRounds = 2 * k - 2;
        for (var j = 0; j < lbRounds; j++)
        {
            var count = LbRoundMatchCount(b, j);
            for (var i = 0; i < count; i++)
            {
                FeedRef top;
                FeedRef bottom;
                if (j == 0)
                {
                    top = new FeedRef { Kind = FeedKind.Loser, MatchId = $"wb-0-{2 * i}" };
                    bottom = new FeedRef { Kind = FeedKind.Loser, MatchId = $"wb-0-{2 * i + 1}" };
                }
                else if (j % 2 == 1)
                {
                    var wbRound = (j + 1) / 2;
                    // Winner of previous LB round match i (not 2*i — e.g. j=1,i=1 must be lb-0-1, not lb-0-2).
                    top = new FeedRef { Kind = FeedKind.Winner, MatchId = $"lb-{j - 1}-{i}" };
                    bottom = new FeedRef { Kind = FeedKind.Loser, MatchId = $"wb-{wbRound}-{i}" };
                }
                else
                {
                    top = new FeedRef { Kind = FeedKind.Winner, MatchId = $"lb-{j - 1}-{2 * i}" };
                    bottom = new FeedRef { Kind = FeedKind.Winner, MatchId = $"lb-{j - 1}-{2 * i + 1}" };
                }

                matches.Add(new BracketMatch
                {
                    Id = $"lb-{j}-{i}",
                    Segment = "LB",
                    Round = j,
                    IndexInRound = i,
                    Top = top,
                    Bottom = bottom
                });
            }
        }

        matches.Add(new BracketMatch
        {
            Id = "gf-0-0",
            Segment = "GF",
            Round = 0,
            IndexInRound = 0,
            Top = new FeedRef { Kind = FeedKind.Winner, MatchId = $"wb-{k - 1}-0" },
            Bottom = new FeedRef { Kind = FeedKind.Winner, MatchId = $"lb-{lbRounds - 1}-0" }
        });

        matches.Add(new BracketMatch
        {
            Id = "gf-1-0",
            Segment = "GF",
            Round = 1,
            IndexInRound = 0,
            Top = new FeedRef { Kind = FeedKind.Empty },
            Bottom = new FeedRef { Kind = FeedKind.Empty }
        });

        return (b, matches);
    }
}
