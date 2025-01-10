using TecmoTourney.Models;

namespace TecmoTourney
{
    public class TournamentStandingComparer : IComparer<TournamentStandingModel>
    {
        private readonly List<PrelimTieBreaker> tiebreakers;
        private readonly Random rand;

        public TournamentStandingComparer(List<PrelimTieBreaker> tiebreakers)
        {
            this.tiebreakers = tiebreakers;
            this.rand = new Random();
        }

        public int Compare(TournamentStandingModel x, TournamentStandingModel y)
        {
            // First compare PreliminariesScore (descending)
            int result = y.PreliminariesScore.CompareTo(x.PreliminariesScore);
            if (result != 0)
                return result;

            // Apply tie-breakers
            foreach (var tieBreaker in tiebreakers)
            {
                switch (tieBreaker)
                {
                    case PrelimTieBreaker.PointsScored:
                        result = y.TotalPointsFor.CompareTo(x.TotalPointsFor);
                        if (result != 0)
                            return result;
                        break;
                    case PrelimTieBreaker.PointsAllowed:
                        result = x.TotalPointsAgainst.CompareTo(y.TotalPointsAgainst); // Fewer is better
                        if (result != 0)
                            return result;
                        break;
                    case PrelimTieBreaker.PassingYards:
                        result = y.TotalPassingYards.CompareTo(x.TotalPassingYards);
                        if (result != 0)
                            return result;
                        break;
                    case PrelimTieBreaker.RushingYards:
                        result = y.TotalRushingYards.CompareTo(x.TotalRushingYards);
                        if (result != 0)
                            return result;
                        break;
                    case PrelimTieBreaker.PassingYardsAllowed:
                        result = x.TotalPassingYardsAllowed.CompareTo(y.TotalPassingYardsAllowed); // Fewer is better
                        if (result != 0)
                            return result;
                        break;
                    case PrelimTieBreaker.RushingYardsAllowed:
                        result = x.TotalRushingYardsAllowed.CompareTo(y.TotalRushingYardsAllowed); // Fewer is better
                        if (result != 0)
                            return result;
                        break;
                    case PrelimTieBreaker.CoinFlip:
                        //just compare the ids of the players
                        return x.PlayerId.CompareTo(y.PlayerId);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(tieBreaker), "Unknown tie-breaker");
                }
            }

            // If all tie-breakers exhausted, players remain tied
            return 0;
        }
    }

}
