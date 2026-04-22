-- Sets TC_GameOdds.Summary to empty string where it is NULL or whitespace-only.
-- Rows with any non-whitespace summary text are left unchanged. Safe to run multiple times.
-- Run against the same database as TecmoTourney (MainDB).

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TC_GameOdds')
BEGIN
    UPDATE dbo.TC_GameOdds
    SET Summary = N''
    WHERE Summary IS NULL
       OR LTRIM(RTRIM(Summary)) = N'';
END
GO
