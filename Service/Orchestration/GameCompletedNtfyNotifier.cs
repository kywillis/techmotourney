using System.Text;
using TecmoTourney;
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
                nameById[id] = FormatNotificationPersonName(raw!, emptyFallback: "unknown");
            }

            var p1 = FormatNotificationPersonName(game.Player1?.PlayerName, emptyFallback: "player");
            var p2 = FormatNotificationPersonName(game.Player2?.PlayerName, emptyFallback: "player");
            var s1 = game.Player1?.Score ?? 0;
            var s2 = game.Player2?.Score ?? 0;

            var sb = new StringBuilder();
            sb.AppendLine("final score");
            sb.AppendLine(FormattableString.Invariant($"{p1} {s1} - {p2} {s2}"));
            sb.AppendLine();
            sb.AppendLine("wagers");
            sb.AppendLine(FormatWagerSection(wagers, winPayouts, nameById));
            sb.AppendLine();
            sb.AppendLine(FormatNetSummaryLine("this game", netThisGame));
            sb.AppendLine(FormatNetSummaryLine("all games", netTournament));

            await _ntfy.SendAsync(sb.ToString().TrimEnd(), "Game Completed", cancellationToken);
        }

        /// <summary>House net: positive = to house, negative = to players; zero shows "even".</summary>
        private static string FormatNetSummaryLine(string scope, decimal net)
        {
            if (net == 0m)
                return FormattableString.Invariant($"{scope}: even");
            if (net > 0m)
                return FormattableString.Invariant($"{scope}: paid to house {BookMoney.FormatUsd(net)}");
            return FormattableString.Invariant($"{scope}: paid to players {BookMoney.FormatUsd(net)}");
        }

        private static string FormatWagerSection(
            IList<WagerDAOModel> wagers,
            IReadOnlyDictionary<int, decimal> winPayouts,
            IReadOnlyDictionary<int, string> bettorDisplayName)
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
                var who = bettorDisplayName[x.Wager.PlayerId];
                lines.Add($"{who} won - {BookMoney.FormatUsd(x.Payout)}");
            }

            var orderedLoss = lost.OrderByDescending(w => w.StakeAmount);
            foreach (var w in orderedLoss)
            {
                var who = bettorDisplayName[w.PlayerId];
                lines.Add($"{who} lost {BookMoney.FormatUsd(-w.StakeAmount)}");
            }

            foreach (var w in voi)
            {
                var who = bettorDisplayName[w.PlayerId];
                lines.Add($"{who} void - {BookMoney.FormatUsd(w.StakeAmount)}");
            }

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Multi-word names use first word plus initial of last word: "John Smith" → "John S".
        /// Single token is capitalized normally (e.g. "Madonna").
        /// </summary>
        private static string FormatNotificationPersonName(string? raw, string emptyFallback)
        {
            var t = (raw ?? string.Empty).Trim();
            if (t.Length == 0)
                return emptyFallback;

            var parts = t.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return emptyFallback;

            if (parts.Length == 1)
                return CapitalizeWord(parts[0]);

            var last = parts[^1];
            var initial = last.Length > 0 ? char.ToUpperInvariant(last[0]).ToString() : string.Empty;
            return $"{CapitalizeWord(parts[0])} {initial}";
        }

        private static string CapitalizeWord(string word)
        {
            if (word.Length == 0)
                return word;
            if (word.Length == 1)
                return word.ToUpperInvariant();
            return char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant();
        }

    }
}
