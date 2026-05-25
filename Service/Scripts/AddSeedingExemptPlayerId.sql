-- Add column for prelim games that don't count toward one player's seeding (e.g. 3rd game when odd number of players).
-- Run against the same database used by TC_GameResults.

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TC_GameResults') AND name = 'SeedingExemptPlayerId'
)
BEGIN
    ALTER TABLE dbo.TC_GameResults
    ADD SeedingExemptPlayerId INT NULL;
END
GO
