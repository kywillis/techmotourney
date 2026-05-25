/*
  Hard-delete one player and dependent rows (wagers, games they appear in, odds, audit, etc.).

  WARNING
  - Set @PlayerId before running. Prefer a copy of production or a transaction you can ROLLBACK first.
  - Every TC_GameResults row where this player is Player1, Player2, or SeedingExempt is DELETED.
    That removes the entire matchup row (the opponent is no longer in that game in the DB).
  - TC_Tournaments.BracketData (JSON) may still reference this PlayerId; fix or regenerate brackets separately.
  - If extra FKs exist in your DB (not in repo scripts), this may fail — read the error and delete those rows first.

  Order respects typical FKs: WagerAudit -> Wagers -> GameOdds / save audit / bracket updates -> GameResults -> PlayerTournaments -> Players.
  PendingActivations.ActivatedByPlayerId is nulled first (FK to TC_Players).
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @PlayerId INT = 0; /* <-- set to the PlayerId to remove */

IF @PlayerId <= 0
BEGIN
    RAISERROR(N'Set @PlayerId to a positive TC_Players.PlayerId.', 16, 1);
    RETURN;
END

BEGIN TRANSACTION;

DECLARE @GameIds TABLE (GameResultId INT PRIMARY KEY);

INSERT INTO @GameIds (GameResultId)
SELECT gr.GameResultId
FROM dbo.TC_GameResults AS gr
WHERE gr.Player1Id = @PlayerId
   OR gr.Player2Id = @PlayerId
   OR gr.SeedingExemptPlayerId = @PlayerId;

/* 1) FK: TC_PendingActivations.ActivatedByPlayerId -> TC_Players */
UPDATE dbo.TC_PendingActivations
SET ActivatedByPlayerId = NULL
WHERE ActivatedByPlayerId = @PlayerId;

/* 2) TC_WagerAudit: references Wagers, Players, GameResults */
DELETE wa
FROM dbo.TC_WagerAudit AS wa
WHERE wa.TargetPlayerId = @PlayerId
   OR wa.ActorPlayerId = @PlayerId
   OR wa.GameResultId IN (SELECT GameResultId FROM @GameIds)
   OR wa.WagerId IN (
        SELECT w.WagerId
        FROM dbo.TC_Wagers AS w
        WHERE w.PlayerId = @PlayerId
           OR w.GameResultId IN (SELECT GameResultId FROM @GameIds)
      );

/* 3) TC_Wagers */
DELETE w
FROM dbo.TC_Wagers AS w
WHERE w.PlayerId = @PlayerId
   OR w.GameResultId IN (SELECT GameResultId FROM @GameIds);

/* 4) TC_GameOdds (per-game link + player columns) */
DELETE o
FROM dbo.TC_GameOdds AS o
WHERE o.GameResultId IN (SELECT GameResultId FROM @GameIds)
   OR o.Player1Id = @PlayerId
   OR o.Player2Id = @PlayerId
   OR o.FavoredPlayerId = @PlayerId;

/* 5) TC_GameResultSaveAudit */
DELETE a
FROM dbo.TC_GameResultSaveAudit AS a
WHERE a.GameResultId IN (SELECT GameResultId FROM @GameIds);

/* 6) TC_TournamentBracketUpdates (when present / FK’d to game) */
DELETE u
FROM dbo.TC_TournamentBracketUpdates AS u
WHERE u.GameResultId IN (SELECT GameResultId FROM @GameIds);

/* 7) TC_GameResults */
DELETE gr
FROM dbo.TC_GameResults AS gr
WHERE gr.GameResultId IN (SELECT GameResultId FROM @GameIds);

/* 8) TC_PlayerTournaments */
DELETE pt
FROM dbo.TC_PlayerTournaments AS pt
WHERE pt.PlayerId = @PlayerId;

/* 9) TC_Players */
DELETE FROM dbo.TC_Players WHERE PlayerId = @PlayerId;

COMMIT TRANSACTION;

PRINT N'Delete completed for PlayerId = ' + CAST(@PlayerId AS NVARCHAR(11)) + N'. Remember to fix tournament bracket JSON if needed.';
