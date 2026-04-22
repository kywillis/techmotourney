-- Game result save audit (e.g. score-grabber). Run against the same DB as TC_GameResults.

IF NOT EXISTS (
    SELECT 1 FROM sys.tables WHERE name = 'TC_GameResultSaveAudit' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE dbo.TC_GameResultSaveAudit (
        AuditId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        GameResultId INT NOT NULL,
        SaveSource NVARCHAR(128) NULL,
        ClientCorrelationId NVARCHAR(64) NULL,
        IsTieGame BIT NOT NULL CONSTRAINT DF_TC_GameResultSaveAudit_IsTieGame DEFAULT (0),
        AccumulatedStats BIT NOT NULL CONSTRAINT DF_TC_GameResultSaveAudit_AccumulatedStats DEFAULT (0),
        RequestJson NVARCHAR(MAX) NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_TC_GameResultSaveAudit_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_TC_GameResultSaveAudit_TC_GameResults FOREIGN KEY (GameResultId)
            REFERENCES dbo.TC_GameResults (GameResultId)
    );
    CREATE INDEX IX_TC_GameResultSaveAudit_GameResultId ON dbo.TC_GameResultSaveAudit (GameResultId);
END
GO
