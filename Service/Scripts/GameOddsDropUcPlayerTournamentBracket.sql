-- TC_GameOdds previously enforced one row per (Player1Id, Player2Id, TournamentId, BracketTypeId).
-- Double elimination can schedule the same pairing twice (e.g. winners then losers) with the same
-- BracketTypeId when only Preliminary vs Tournament is distinguished in code — inserts then violate UC_PlayerTournamentBracket.
-- Odds are keyed by game: one row per GameResultId.

IF EXISTS (
    SELECT 1
    FROM sys.key_constraints
    WHERE name = N'UC_PlayerTournamentBracket'
      AND parent_object_id = OBJECT_ID(N'dbo.TC_GameOdds')
)
BEGIN
    ALTER TABLE dbo.TC_GameOdds DROP CONSTRAINT UC_PlayerTournamentBracket;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UQ_TC_GameOdds_GameResultId'
      AND object_id = OBJECT_ID(N'dbo.TC_GameOdds')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_TC_GameOdds_GameResultId
    ON dbo.TC_GameOdds (GameResultId)
    WHERE GameResultId IS NOT NULL;
END
GO
