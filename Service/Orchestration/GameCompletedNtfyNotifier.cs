using System.Globalization;
using System.Text;
using TecmoTourney.DataAccess.Interfaces;
using TecmoTourney.DataAccess.Models;
using TecmoTourney.Models;
using TecmoTourney.Notifications;
using TecmoTourney.Orchestration.Interfaces;

namespace TecmoTourney.Orchestration
{
    public class GameCompletedNtfyNotifier : IGameCompletedNtfyNotifier
    {
        private readonly INtfyClient _ntfy;
        private readonly IWagerDAO _wagerDao;
        private readonly IPlayerDAO _playerDao;

        public GameCompletedNtfyNotifier(
            INtfyClient ntfy,
            IWagerDAO wagerDao,
            IPlayerDAO playerDao)
        {
            _ntfy = ntfy;
            _wagerDao = wagerDao;
            _playerDao = playerDao;
        }

        public async Task TryNotifyFirstCompletedAsync(
            GameResultModel game,
            int gameResultId,
            int tournamentId,
            CancellationToken cancellationToken = default)
        {
            var wagers = (await _wagerDao.GetByGameResultIdAsync(gameResultId))
                .Where(w => w.Status != WagerStatus.Cancelled)
                .ToList();

            var netThisGame = await _wagerDao.GetSettledWagerNetForGameResultAsync(gameResultId);
            var netTournament = await _wagerDao.GetSettledWagerNetForTournamentAsync(tournamentId);
            var winPayouts = await _wagerDao.GetWinPayoutsByWagerIdForGameResultAsync(gameResultId);

            var nameById = new Dictionary<int, string>();
            foreach (var id in wagers.Select(w => w.PlayerId).Distinct())
            {
                var p = await _playerDao.GetPlayerAsync(id);
                var raw = p?.FullName;
                if (string.IsNullOrWhiteSpace(raw))
                    raw = $"Player {id}";
                nameById[id] = FirstTokenLower(raw!);
            }

            var p1 = NameForScoreLine(game.Player1?.PlayerName);
            var p2 = NameForScoreLine(game.Player2?.PlayerName);
            var s1 = game.Player1?.Score ?? 0;
            var s2 = game.Player2?.Score ?? 0;

            var sb = new StringBuilder();
            sb.AppendLine("final score");
            sb.AppendLine(FormattableString.Invariant($"{p1} {s1} - {p2} {s2}"));
            sb.AppendLine();
            sb.AppendLine("wagers");
            sb.AppendLine(FormatWagerSection(wagers, winPayouts, nameById));
            sb.AppendLine();
            sb.AppendLine(FormattableString.Invariant($"total payout for this game: {FmtDollar(netThisGame)}"));
            sb.AppendLine(FormattableString.Invariant($"total payout for all games: {FmtDollar(netTournament)}"));

            await _ntfy.SendAsync(sb.ToString().TrimEnd(), cancellationToken);
        }

        private static string FormatWagerSection(
            IList<WagerDAOModel> wagers,
            IReadOnlyDictionary<int, decimal> winPayouts,
            IReadOnlyDictionary<int, string> bettorFirstNameLower)
        {
            if (wagers.Count == 0)
                return "no wagers";

            var won = wagers.Where(w => w.Status == WagerStatus.Won).ToList();
            var lost = wagers.Where(w => w.Status == WagerStatus.Lost).ToList();
            var voi = wagers.Where(w => w.Status == WagerStatus.Void).ToList();
            var pending = wagers.Where(w => w.Status == WagerStatus.Pending).ToList();

            if (won.Count == 0 && lost.Count == 0 && voi.Count == 0)
            {
                if (pending.Count > 0)
                    return "not settled (wagers still pending for this result)";
                return "no wagers";
            }

            var lines = new List<string>();
            var orderedWins = won
                .Select(w => (Wager: w, Payout: winPayouts.TryGetValue(w.WagerId, out var p) ? p : 0m))
                .OrderByDescending(x => x.Payout);
            foreach (var x in orderedWins)
            {
                var who = bettorFirstNameLower[x.Wager.PlayerId];
                lines.Add(FormattableString.Invariant($"{who} won - {FmtDollar(x.Payout)}"));
            }

            var orderedLoss = lost.OrderByDescending(w => w.StakeAmount);
            foreach (var w in orderedLoss)
            {
                var who = bettorFirstNameLower[w.PlayerId];
                lines.Add(FormattableString.Invariant($"{who} lost - {FmtDollar(w.StakeAmount)}"));
            }

            foreach (var w in voi)
            {
                var who = bettorFirstNameLower[w.PlayerId];
                lines.Add(FormattableString.Invariant($"{who} void - {FmtDollar(w.StakeAmount)}"));
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static string FirstTokenLower(string fullName)
        {
            var t = fullName.Trim();
            if (t.Length == 0)
                return "unknown";
            var space = t.IndexOf(' ');
            if (space < 0)
                return t.ToLowerInvariant();
            return t.Substring(0, space).ToLowerInvariant();
        }

        private static string NameForScoreLine(string? playerName)
        {
            var t = (playerName ?? string.Empty).Trim();
            if (t.Length == 0)
                return "player";
            var parts = t.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[^1].ToLowerInvariant() : t.ToLowerInvariant();
        }

        private static string FmtDollar(decimal d)
        {
            if (d == 0m)
                return "$0.00";
            var a = Math.Abs(d);
            var s = a.ToString("0.00", CultureInfo.InvariantCulture);
            return d < 0m ? "-$" + s : "$" + s;
        }
    }
}
