-- Soft-delete for game odds (invalidated bracket lines stay in DB for wager history).
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TC_GameOdds') AND name = 'IsDeleted'
)
BEGIN
    ALTER TABLE dbo.TC_GameOdds ADD IsDeleted BIT NOT NULL CONSTRAINT DF_TC_GameOdds_IsDeleted DEFAULT (0);
END
GO
