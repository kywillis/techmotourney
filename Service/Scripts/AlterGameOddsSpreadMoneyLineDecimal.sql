-- Spread: DECIMAL(5,1) for half-point lines (e.g. -3.5). Money lines: DECIMAL(10,1) with one decimal.
-- Run against MainDB after backup. Safe if columns are already DECIMAL (checks sys.types).

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TC_GameOdds')
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns c
               JOIN sys.types t ON c.user_type_id = t.user_type_id
               WHERE c.object_id = OBJECT_ID(N'dbo.TC_GameOdds') AND c.name = N'Spread' AND t.name = 'int')
    BEGIN
        ALTER TABLE dbo.TC_GameOdds ALTER COLUMN Spread DECIMAL(5, 1) NOT NULL;
    END

    IF EXISTS (SELECT 1 FROM sys.columns c
               JOIN sys.types t ON c.user_type_id = t.user_type_id
               WHERE c.object_id = OBJECT_ID(N'dbo.TC_GameOdds') AND c.name = N'MoneyLinePlayer1' AND t.name = 'int')
    BEGIN
        ALTER TABLE dbo.TC_GameOdds ALTER COLUMN MoneyLinePlayer1 DECIMAL(10, 1) NULL;
    END

    IF EXISTS (SELECT 1 FROM sys.columns c
               JOIN sys.types t ON c.user_type_id = t.user_type_id
               WHERE c.object_id = OBJECT_ID(N'dbo.TC_GameOdds') AND c.name = N'MoneyLinePlayer2' AND t.name = 'int')
    BEGIN
        ALTER TABLE dbo.TC_GameOdds ALTER COLUMN MoneyLinePlayer2 DECIMAL(10, 1) NULL;
    END
END
GO
