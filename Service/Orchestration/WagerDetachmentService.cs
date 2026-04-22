using System.Linq;
using TecmoTourney;
using TecmoTourney.DataAccess.Interfaces;
using TecmoTourney.DataAccess.Models;
using TecmoTourney.Orchestration.Interfaces;

namespace TecmoTourney.Orchestration
{
    public class WagerDetachmentService : IWagerDetachmentService
    {
        private readonly IWagerDAO _wagerDAO;
        private readonly IPlayerDAO _playerDAO;
        private readonly IWagerAuditDAO _wagerAuditDAO;

        public WagerDetachmentService(IWagerDAO wagerDAO, IPlayerDAO playerDAO, IWagerAuditDAO wagerAuditDAO)
        {
            _wagerDAO = wagerDAO;
            _playerDAO = playerDAO;
            _wagerAuditDAO = wagerAuditDAO;
        }

        public async Task DetachWagersForGameResultsAsync(IEnumerable<int> gameResultIds, int? actorPlayerId = null)
        {
            var ids = gameResultIds?.Where(id => id > 0).Distinct().ToArray() ?? [];
            foreach (var id in ids)
                await DetachWagersForGameResultAsync(id, actorPlayerId);
        }

        public async Task DetachWagersForGameResultAsync(int gameResultId, int? actorPlayerId = null)
        {
            if (gameResultId < 1)
                return;

            var wagers = (await _wagerDAO.GetByGameResultIdAsync(gameResultId)).ToList();
            foreach (var w in wagers)
            {
                if (w.Status == WagerStatus.Pending)
                    await DetachPendingAsync(w, gameResultId, actorPlayerId);
                else
                    await DetachNonPendingAsync(w, gameResultId, actorPlayerId);
            }
        }

        private async Task DetachPendingAsync(WagerDAOModel w, int gameResultId, int? actorPlayerId)
        {
            var player = await _playerDAO.GetPlayerAsync(w.PlayerId);
            if (player == null)
                return;

            var balanceBefore = player.Balance;
            var balanceAfter = balanceBefore + w.StakeAmount;
            var now = DateTime.UtcNow;

            var updated = await _wagerDAO.CancelPendingAndClearGameResultAsync(w.WagerId, now);
            if (!updated)
                return;

            await _playerDAO.UpdatePlayerBalanceAsync(w.PlayerId, balanceAfter);
            await _wagerAuditDAO.InsertAsync(new WagerAuditDAOModel
            {
                TournamentId = w.TournamentId,
                TargetPlayerId = w.PlayerId,
                ActorPlayerId = actorPlayerId,
                Action = WagerAuditAction.GameResultRemoved,
                WagerId = w.WagerId,
                GameResultId = gameResultId,
                Amount = w.StakeAmount,
                BalanceBefore = balanceBefore,
                BalanceAfter = balanceAfter,
                CreatedAt = now
            });
        }

        private async Task DetachNonPendingAsync(WagerDAOModel w, int gameResultId, int? actorPlayerId)
        {
            var now = DateTime.UtcNow;
            var updated = await _wagerDAO.ClearGameResultIdForNonPendingAsync(w.WagerId);
            if (!updated)
                return;

            await _wagerAuditDAO.InsertAsync(new WagerAuditDAOModel
            {
                TournamentId = w.TournamentId,
                TargetPlayerId = w.PlayerId,
                ActorPlayerId = actorPlayerId,
                Action = WagerAuditAction.GameResultRemoved,
                WagerId = w.WagerId,
                GameResultId = gameResultId,
                Amount = null,
                BalanceBefore = null,
                BalanceAfter = null,
                CreatedAt = now
            });
        }
    }
}
