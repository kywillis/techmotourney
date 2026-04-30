using System.Globalization;

namespace TecmoTourney
{
    /// <summary>US book-style return after vig (profit only) and USD display: $40.00 / ($30.00).</summary>
    public static class BookMoney
    {
        /// <summary>Standard US price for spread and totals (e.g. −110: risk $1.10 to win $1 profit before house vig).</summary>
        public const decimal StandardSpreadAmericanOdds = -110m;

        /// <summary>Stake + profit at <see cref="StandardSpreadAmericanOdds"/>; cents, away from zero.</summary>
        public static decimal GrossReturnOnWinSpreadOrOverUnder(decimal stake)
        {
            if (stake <= 0m)
                return 0m;
            return Math.Round(
                stake + ProfitFromAmericanOdds(stake, StandardSpreadAmericanOdds),
                2,
                MidpointRounding.AwayFromZero);
        }

        private static decimal ProfitFromAmericanOdds(decimal stake, decimal american)
        {
            if (american > 0m)
                return stake * american / 100m;
            return stake * 100m / (-american);
        }

        /// <summary>
        /// Gross return including stake, after taking <paramref name="vigPercent"/> of the profit
        /// (5 = 5%). Rounded to cents, away from zero.
        /// </summary>
        public static decimal NetReturnAfterVigOnProfit(decimal grossReturnIncludingStake, decimal stake, int vigPercent)
        {
            if (grossReturnIncludingStake < 0m || stake < 0m)
                return 0m;
            if (vigPercent <= 0)
                return Math.Round(grossReturnIncludingStake, 2, MidpointRounding.AwayFromZero);

            var profit = grossReturnIncludingStake - stake;
            if (profit <= 0m)
                return Math.Round(grossReturnIncludingStake, 2, MidpointRounding.AwayFromZero);

            var vig = profit * vigPercent / 100m;
            var net = grossReturnIncludingStake - vig;
            return Math.Round(net, 2, MidpointRounding.AwayFromZero);
        }

        public static string FormatUsd(decimal d)
        {
            if (d == 0m)
                return "$0.00";
            var abs = Math.Abs(d);
            var s = abs.ToString("0.00", CultureInfo.InvariantCulture);
            if (d < 0m)
                return "($" + s + ")";
            return "$" + s;
        }
    }
}
