-- TC_Wagers.GameResultId: allow NULL when game/odds are removed; wagers rows are retained for history.
-- Run against the main tournament/wagering database.

IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_TC_Wagers_GameResult' AND parent_object_id = OBJECT_ID(N'dbo.TC_Wagers')
)
BEGIN
    ALTER TABLE dbo.TC_Wagers DROP CONSTRAINT FK_TC_Wagers_GameResult;
END
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TC_Wagers') AND name = N'GameResultId' AND is_nullable = 0
)
BEGIN
    ALTER TABLE dbo.TC_Wagers ALTER COLUMN GameResultId INT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_TC_Wagers_GameResult' AND parent_object_id = OBJECT_ID(N'dbo.TC_Wagers')
)
BEGIN
    ALTER TABLE dbo.TC_Wagers ADD CONSTRAINT FK_TC_Wagers_GameResult
        FOREIGN KEY (GameResultId) REFERENCES dbo.TC_GameResults(GameResultId);
END
GO
