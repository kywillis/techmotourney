-- Step 1 (Section 0): Rename Point Spread to Game Odds.
-- Storage standards: primary key (GameOddsId), DateAdded and DateModified NOT NULL with default GETDATE().
-- Run against the same database that contains TC_PointSpreads.

-- Rename table
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TC_PointSpreads')
BEGIN
    EXEC sp_rename 'dbo.TC_PointSpreads', 'TC_GameOdds';
END
GO

-- Rename primary key column
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TC_GameOdds') AND name = 'PointSpreadId'
)
BEGIN
    EXEC sp_rename 'dbo.TC_GameOdds.PointSpreadId', 'GameOddsId', 'COLUMN';
END
GO

-- Ensure DateAdded (storage standard)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TC_GameOdds') AND name = 'DateAdded'
)
BEGIN
    ALTER TABLE dbo.TC_GameOdds
    ADD DateAdded DATETIME NOT NULL DEFAULT GETDATE();
END
GO

-- Ensure DateModified (storage standard)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TC_GameOdds') AND name = 'DateModified'
)
BEGIN
    ALTER TABLE dbo.TC_GameOdds
    ADD DateModified DATETIME NOT NULL DEFAULT GETDATE();
END
GO

-- Money line (American odds): nullable, e.g. -150 and +130
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TC_GameOdds') AND name = 'MoneyLinePlayer1'
)
BEGIN
    ALTER TABLE dbo.TC_GameOdds
    ADD MoneyLinePlayer1 INT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TC_GameOdds') AND name = 'MoneyLinePlayer2'
)
BEGIN
    ALTER TABLE dbo.TC_GameOdds
    ADD MoneyLinePlayer2 INT NULL;
END
GO

-- Over/under: total points line, nullable
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TC_GameOdds') AND name = 'OverUnder'
)
BEGIN
    ALTER TABLE dbo.TC_GameOdds
    ADD OverUnder DECIMAL(9,2) NULL;
END
GO
