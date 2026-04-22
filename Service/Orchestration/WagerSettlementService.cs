using System;
using System.Linq;
using System.Threading.Tasks;
using TecmoTourney.DataAccess.Interfaces;
using TecmoTourney.DataAccess.Models;
using TecmoTourney.Orchestration.Interfaces;

namespace TecmoTourney.Orchestration
{
    public class WagerSettlementService : IWagerSettlementService
    {
        private static readonly WagerAuditAction[] SettlementActions =
        {
            WagerAuditAction.SettleWagerWin,
            WagerAuditAction.SettleWagerLose,
            WagerAuditAction.VoidWager
        };

        private readonly IWagerDAO _wagerDAO;
        private readonly IWagerAuditDAO _wagerAuditDAO;
        private readonly IPlayerDAO _playerDAO;
        private readonly IGameOddsDAO _gameOddsDAO;

        public WagerSettlementService(
            IWagerDAO wagerDAO,
            IWagerAuditDAO wagerAuditDAO,
            IPlayerDAO playerDAO,
            IGameOddsDAO gameOddsDAO)
        {
            _wagerDAO = wagerDAO;
            _wagerAuditDAO = wagerAuditDAO;
            _playerDAO = playerDAO;
            _gameOddsDAO = gameOddsDAO;
        }

        /// <inheritdoc />
        public async Task SettleWagersAfterGameSaveAsync(GameResultDAOModel game)
        {
            if (game == null || game.IsDeleted)
                return;
            if (!ScoreAllowsSettlement(game))
                return;

            var odds = await _gameOddsDAO.GetByGameResultIdAsync(game.GameResultId);
            if (odds == null || odds.IsDeleted)
                return;

            var wagers = (await _wagerDAO.GetByGameResultIdAsync(game.GameResultId))
                .Where(w => w.Status != WagerStatus.Cancelled)
                .ToList();
            if (wagers.Count == 0)
                return;

            foreach (var wager in wagers)
            {
                if (wager.Status != WagerStatus.Pending)
                    await ReverseLastSettlementAsync(wager, odds, game.GameResultId);

                var grade = GradeWager(wager, odds, game);
                await ApplySettlementAsync(wager, odds, game, grade);
            }
        }

        /// <summary>
        /// Completed game, not a tie, winner score strictly greater than zero.
        /// </summary>
        internal static bool ScoreAllowsSettlement(GameResultDAOModel game)
        {
            if (game.StatusId != (int)GameStatus.Completed)
                return false;
            var s1 = game.Player1Score;
            var s2 = game.Player2Score;
            if (s1 == s2)
                return false;
            var winner = Math.Max(s1, s2);
            return winner > 0;
        }

        private static void SpreadLines(
            GameOddsDAOModel odds,
            out decimal lineP1,
            out decimal lineP2)
        {
            var mag = Math.Abs(odds.Spread);
            lineP1 = mag;
            lineP2 = mag;
            if (odds.FavoredPlayerId.HasValue)
            {
                if (odds.FavoredPlayerId.Value == odds.Player1Id)
                {
                    lineP1 = -mag;
                    lineP2 = mag;
                }
                else if (odds.FavoredPlayerId.Value == odds.Player2Id)
                {
                    lineP1 = mag;
                    lineP2 = -mag;
                }
            }
        }

        private static SettleGrade GradeWager(WagerDAOModel wager, GameOddsDAOModel odds, GameResultDAOModel game)
        {
            var s1 = game.Player1Score;
            var s2 = game.Player2Score;

            switch (wager.MarketType)
            {
                case WagerMarketType.Spread:
                    SpreadLines(odds, out var lp1, out var lp2);
                    if (wager.Side == WagerSide.Player1Spread)
                    {
                        var v = (s1 - s2) + lp1;
                        if (v > 0) return SettleGrade.Win;
                        if (v < 0) return SettleGrade.Loss;
                        return SettleGrade.Void;
                    }

                    if (wager.Side == WagerSide.Player2Spread)
                    {
                        var v = (s2 - s1) + lp2;
                        if (v > 0) return SettleGrade.Win;
                        if (v < 0) return SettleGrade.Loss;
                        return SettleGrade.Void;
                    }

                    return SettleGrade.Void;

                case WagerMarketType.OverUnder:
                    if (!odds.OverUnder.HasValue)
                        return SettleGrade.Void;
                    var total = s1 + s2;
                    var line = odds.OverUnder.Value;
                    if (wager.Side == WagerSide.Over)
                    {
                        if (total > line) return SettleGrade.Win;
                        if (total < line) return SettleGrade.Loss;
                        return SettleGrade.Void;
                    }

                    if (wager.Side == WagerSide.Under)
                    {
                        if (total < line) return SettleGrade.Win;
                        if (total > line) return SettleGrade.Loss;
                        return SettleGrade.Void;
                    }

                    return SettleGrade.Void;

                case WagerMarketType.MoneyLine:
                    if (s1 > s2)
                    {
                        if (wager.Side == WagerSide.Player1ML) return SettleGrade.Win;
                        if (wager.Side == WagerSide.Player2ML) return SettleGrade.Loss;
                    }
                    else
                    {
                        if (wager.Side == WagerSide.Player2ML) return SettleGrade.Win;
                        if (wager.Side == WagerSide.Player1ML) return SettleGrade.Loss;
                    }

                    return SettleGrade.Void;

                default:
                    return SettleGrade.Void;
            }
        }

        private async Task ReverseLastSettlementAsync(WagerDAOModel wager, GameOddsDAOModel odds, int gameResultId)
        {
            var audits = (await _wagerAuditDAO.GetByWagerIdAsync(wager.WagerId)).ToList();
            var last = audits.FirstOrDefault(a => SettlementActions.Contains(a.Action));
            decimal delta;
            if (last != null && last.BalanceAfter.HasValue && last.BalanceBefore.HasValue)
                delta = last.BalanceAfter.Value - last.BalanceBefore.Value;
            else
                delta = FallbackSettlementBalanceDelta(wager, odds);

            if (delta == 0)
                return;

            var player = await _playerDAO.GetPlayerAsync(wager.PlayerId);
            if (player == null)
                return;

            var now = DateTime.UtcNow;
            var newBalance = player.Balance - delta;
            await _playerDAO.UpdatePlayerBalanceAsync(wager.PlayerId, newBalance);
            await _wagerAuditDAO.InsertAsync(new WagerAuditDAOModel
            {
                TournamentId = wager.TournamentId,
                TargetPlayerId = wager.PlayerId,
                ActorPlayerId = null,
                Action = WagerAuditAction.ReverseSettlement,
                WagerId = wager.WagerId,
                GameResultId = gameResultId,
                Amount = -delta,
                BalanceBefore = player.Balance,
                BalanceAfter = newBalance,
                CreatedAt = now
            });
        }

        /// <summary>
        /// When no settlement audit exists (legacy), infer the balance delta that was applied from status + stake + odds.
        /// </summary>
        private static decimal FallbackSettlementBalanceDelta(WagerDAOModel wager, GameOddsDAOModel odds)
        {
            return wager.Status switch
            {
                WagerStatus.Won => ComputePotentialTotalReturnOnWin(wager, odds),
                WagerStatus.Void => wager.StakeAmount,
                _ => 0m
            };
        }

        private async Task ApplySettlementAsync(
            WagerDAOModel wager,
            GameOddsDAOModel odds,
            GameResultDAOModel game,
            SettleGrade grade)
        {
            var now = DateTime.UtcNow;
            var player = await _playerDAO.GetPlayerAsync(wager.PlayerId);
            if (player == null)
                return;

            WagerStatus newStatus;
            decimal balanceAfter;
            WagerAuditAction auditAction;
            decimal? auditAmount;

            switch (grade)
            {
                case SettleGrade.Win:
                    newStatus = WagerStatus.Won;
                    var payout = ComputePotentialTotalReturnOnWin(wager, odds);
                    balanceAfter = player.Balance + payout;
                    auditAction = WagerAuditAction.SettleWagerWin;
                    auditAmount = payout;
                    await _playerDAO.UpdatePlayerBalanceAsync(wager.PlayerId, balanceAfter);
                    break;

                case SettleGrade.Loss:
                    newStatus = WagerStatus.Lost;
                    balanceAfter = player.Balance;
                    auditAction = WagerAuditAction.SettleWagerLose;
                    auditAmount = 0m;
                    break;

                case SettleGrade.Void:
                default:
                    newStatus = WagerStatus.Void;
                    balanceAfter = player.Balance + wager.StakeAmount;
                    auditAction = WagerAuditAction.VoidWager;
                    auditAmount = wager.StakeAmount;
                    await _playerDAO.UpdatePlayerBalanceAsync(wager.PlayerId, balanceAfter);
                    break;
            }

            await _wagerDAO.UpdateStatusAsync(wager.WagerId, newStatus, cancelledAt: null, settledAt: now);
            await _wagerAuditDAO.InsertAsync(new WagerAuditDAOModel
            {
                TournamentId = wager.TournamentId,
                TargetPlayerId = wager.PlayerId,
                ActorPlayerId = null,
                Action = auditAction,
                WagerId = wager.WagerId,
                GameResultId = game.GameResultId,
                Amount = auditAmount,
                BalanceBefore = player.Balance,
                BalanceAfter = balanceAfter,
                CreatedAt = now
            });
        }

        /// <summary>Stake plus profit if the bet wins (same rules as wager placement / my-wagers payout display).</summary>
        private static decimal ComputePotentialTotalReturnOnWin(WagerDAOModel w, GameOddsDAOModel odds)
        {
            var s = w.StakeAmount;
            if (s <= 0)
                return 0;

            switch (w.MarketType)
            {
                case WagerMarketType.Spread:
                case WagerMarketType.OverUnder:
                    return Math.Round(s * 2m, 2, MidpointRounding.AwayFromZero);

                case WagerMarketType.MoneyLine:
                    var american = w.Side == WagerSide.Player1ML
                        ? odds.MoneyLinePlayer1
                        : w.Side == WagerSide.Player2ML
                            ? odds.MoneyLinePlayer2
                            : null;
                    if (!american.HasValue || american.Value == 0)
                        return Math.Round(s * 2m, 2, MidpointRounding.AwayFromZero);
                    return Math.Round(s + ProfitFromAmericanOddsStake(s, american.Value), 2, MidpointRounding.AwayFromZero);

                default:
                    return s;
            }
        }

        private static decimal ProfitFromAmericanOddsStake(decimal stake, decimal american)
        {
            if (american > 0)
                return stake * american / 100m;
            return stake * 100m / (-american);
        }

        private enum SettleGrade
        {
            Win,
            Loss,
            Void
        }
    }
}
