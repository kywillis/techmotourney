-- TC_Wagers.Status: change to INT (0=Pending … 4=Cancelled). Safe to re-run: skips if already integer.
-- Use when values are already numeric; run against your tournament/wagering database.

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @df sysname,
        @sql nvarchar(max);

IF OBJECT_ID(N'dbo.TC_Wagers', N'U') IS NULL
BEGIN
    PRINT N'TC_Wagers not found; skipping.';
    RETURN;
END;

IF EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.TC_Wagers')
      AND c.name = N'Status'
      AND t.name IN (N'int', N'tinyint', N'smallint')
)
BEGIN
    PRINT N'Status is already an integer; no change.';
    RETURN;
END;

BEGIN TRAN;

SELECT @df = dc.name
FROM sys.default_constraints AS dc
INNER JOIN sys.columns AS c
    ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE c.object_id = OBJECT_ID(N'dbo.TC_Wagers') AND c.name = N'Status';

IF @df IS NOT NULL
BEGIN
    SET @sql = N'ALTER TABLE dbo.TC_Wagers DROP CONSTRAINT ' + QUOTENAME(@df) + N';';
    EXEC sys.sp_executesql @sql;
END;

-- Indexes on Status must be dropped before changing the column type.
IF EXISTS (SELECT 1 FROM sys.indexes i WHERE i.object_id = OBJECT_ID(N'dbo.TC_Wagers') AND i.name = N'IX_TC_Wagers_GameResultId_MarketType_Status')
    DROP INDEX IX_TC_Wagers_GameResultId_MarketType_Status ON dbo.TC_Wagers;
IF EXISTS (SELECT 1 FROM sys.indexes i WHERE i.object_id = OBJECT_ID(N'dbo.TC_Wagers') AND i.name = N'IX_TC_Wagers_PlayerId_Status')
    DROP INDEX IX_TC_Wagers_PlayerId_Status ON dbo.TC_Wagers;

ALTER TABLE dbo.TC_Wagers ALTER COLUMN Status INT NOT NULL;

IF NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints AS dc
    INNER JOIN sys.columns AS c
        ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE c.object_id = OBJECT_ID(N'dbo.TC_Wagers')
      AND c.name = N'Status'
      AND dc.name = N'DF_TC_Wagers_Status'
)
    ALTER TABLE dbo.TC_Wagers
    ADD CONSTRAINT DF_TC_Wagers_Status DEFAULT 0 FOR Status;

-- Recreate indexes (same as WageringStep1_Migrations.sql)
IF NOT EXISTS (SELECT 1 FROM sys.indexes i WHERE i.object_id = OBJECT_ID(N'dbo.TC_Wagers') AND i.name = N'IX_TC_Wagers_GameResultId_MarketType_Status')
    CREATE NONCLUSTERED INDEX IX_TC_Wagers_GameResultId_MarketType_Status
    ON dbo.TC_Wagers(GameResultId, MarketType, Status);

IF NOT EXISTS (SELECT 1 FROM sys.indexes i WHERE i.object_id = OBJECT_ID(N'dbo.TC_Wagers') AND i.name = N'IX_TC_Wagers_PlayerId_Status')
    CREATE NONCLUSTERED INDEX IX_TC_Wagers_PlayerId_Status
    ON dbo.TC_Wagers(PlayerId, Status);

COMMIT TRAN;
PRINT N'TC_Wagers.Status is now INT NOT NULL (default 0).';
