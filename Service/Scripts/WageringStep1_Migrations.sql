-- Wagering feature: Step 1 - Database migrations.
-- Run against the same database used by TC_GameResults, TC_GameOdds, TC_Players.
-- Idempotent: safe to run multiple times.

-- =============================================================================
-- 1.1 TC_GameResults: GameStartedAt (betting closes when set; if completed without it, void all wagers)
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TC_GameResults') AND name = 'GameStartedAt'
)
BEGIN
    ALTER TABLE dbo.TC_GameResults
    ADD GameStartedAt DATETIME2 NULL;
END
GO

-- =============================================================================
-- 1.2 TC_GameOdds: GameResultId (link odds to a specific game for wagering)
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TC_GameOdds') AND name = 'GameResultId'
)
BEGIN
    ALTER TABLE dbo.TC_GameOdds
    ADD GameResultId INT NULL;

    ALTER TABLE dbo.TC_GameOdds
    ADD CONSTRAINT FK_TC_GameOdds_TC_GameResults
    FOREIGN KEY (GameResultId) REFERENCES dbo.TC_GameResults(GameResultId);
END
GO

-- =============================================================================
-- 1.3 TC_Players: wagering columns (Google auth, admin, balance, active)
-- =============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TC_Players') AND name = 'GoogleSubjectId'
)
BEGIN
    ALTER TABLE dbo.TC_Players
    ADD GoogleSubjectId NVARCHAR(256) NULL;

    CREATE UNIQUE NONCLUSTERED INDEX UQ_TC_Players_GoogleSubjectId
    ON dbo.TC_Players(GoogleSubjectId)
    WHERE GoogleSubjectId IS NOT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TC_Players') AND name = 'IsAdmin'
)
BEGIN
    ALTER TABLE dbo.TC_Players
    ADD IsAdmin BIT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TC_Players') AND name = 'Balance'
)
BEGIN
    ALTER TABLE dbo.TC_Players
    ADD Balance DECIMAL(18,2) NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TC_Players') AND name = 'IsActive'
)
BEGIN
    ALTER TABLE dbo.TC_Players
    ADD IsActive BIT NOT NULL DEFAULT 1;
END
GO

-- =============================================================================
-- 1.4 TC_PendingActivations (users waiting to be activated; created on first Google sign-in)
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TC_PendingActivations')
BEGIN
    CREATE TABLE dbo.TC_PendingActivations (
        PendingActivationId INT IDENTITY(1,1) NOT NULL,
        GoogleSubjectId     NVARCHAR(256) NOT NULL,
        Email               NVARCHAR(256) NOT NULL,
        FullName            NVARCHAR(256) NOT NULL,
        RequestedProfilePic INT NOT NULL DEFAULT 0,
        Status              NVARCHAR(32) NOT NULL DEFAULT 'Pending',
        RequestedAt         DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ActivatedAt         DATETIME2 NULL,
        ActivatedByPlayerId INT NULL,
        CONSTRAINT PK_TC_PendingActivations PRIMARY KEY (PendingActivationId),
        CONSTRAINT FK_TC_PendingActivations_ActivatedBy FOREIGN KEY (ActivatedByPlayerId) REFERENCES dbo.TC_Players(PlayerId)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UQ_TC_PendingActivations_GoogleSubjectId
    ON dbo.TC_PendingActivations(GoogleSubjectId);
END
GO

-- =============================================================================
-- 1.5 TC_Wagers
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TC_Wagers')
BEGIN
    CREATE TABLE dbo.TC_Wagers (
        WagerId     INT IDENTITY(1,1) NOT NULL,
        PlayerId    INT NOT NULL,
        GameResultId INT NOT NULL,
        TournamentId INT NOT NULL,
        MarketType  NVARCHAR(32) NOT NULL,
        Side        NVARCHAR(32) NOT NULL,
        StakeAmount DECIMAL(18,2) NOT NULL,
        Status      INT NOT NULL DEFAULT 0,
        CreatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CancelledAt DATETIME2 NULL,
        SettledAt   DATETIME2 NULL,
        CONSTRAINT PK_TC_Wagers PRIMARY KEY (WagerId),
        CONSTRAINT FK_TC_Wagers_Player    FOREIGN KEY (PlayerId)    REFERENCES dbo.TC_Players(PlayerId),
        CONSTRAINT FK_TC_Wagers_GameResult FOREIGN KEY (GameResultId) REFERENCES dbo.TC_GameResults(GameResultId),
        CONSTRAINT FK_TC_Wagers_Tournament FOREIGN KEY (TournamentId) REFERENCES dbo.TC_Tournaments(TournamentId)
    );

    CREATE NONCLUSTERED INDEX IX_TC_Wagers_GameResultId_MarketType_Status
    ON dbo.TC_Wagers(GameResultId, MarketType, Status);

    CREATE NONCLUSTERED INDEX IX_TC_Wagers_PlayerId_Status
    ON dbo.TC_Wagers(PlayerId, Status);
END
GO

-- =============================================================================
-- 1.6 TC_WagerAudit (audit trail: place wager, settle, balance changes)
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TC_WagerAudit')
BEGIN
    CREATE TABLE dbo.TC_WagerAudit (
        AuditId        INT IDENTITY(1,1) NOT NULL,
        TournamentId   INT NULL,
        TargetPlayerId INT NOT NULL,
        ActorPlayerId  INT NULL,
        Action         INT NOT NULL,
        WagerId        INT NULL,
        GameResultId   INT NULL,
        Amount         DECIMAL(18,2) NULL,
        BalanceBefore  DECIMAL(18,2) NULL,
        BalanceAfter   DECIMAL(18,2) NULL,
        CreatedAt      DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_TC_WagerAudit PRIMARY KEY (AuditId),
        CONSTRAINT FK_TC_WagerAudit_TargetPlayer FOREIGN KEY (TargetPlayerId) REFERENCES dbo.TC_Players(PlayerId),
        CONSTRAINT FK_TC_WagerAudit_Actor        FOREIGN KEY (ActorPlayerId)   REFERENCES dbo.TC_Players(PlayerId),
        CONSTRAINT FK_TC_WagerAudit_Wager       FOREIGN KEY (WagerId)         REFERENCES dbo.TC_Wagers(WagerId),
        CONSTRAINT FK_TC_WagerAudit_GameResult   FOREIGN KEY (GameResultId)    REFERENCES dbo.TC_GameResults(GameResultId),
        CONSTRAINT FK_TC_WagerAudit_Tournament  FOREIGN KEY (TournamentId)    REFERENCES dbo.TC_Tournaments(TournamentId)
    );

    CREATE NONCLUSTERED INDEX IX_TC_WagerAudit_TargetPlayerId_TournamentId_CreatedAt
    ON dbo.TC_WagerAudit(TargetPlayerId, TournamentId, CreatedAt DESC);
END
GO

-- =============================================================================
-- 1.7 TC_WagerSettings (single row: ShowActionOnGames, MaxMarketImbalance)
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TC_WagerSettings')
BEGIN
    CREATE TABLE dbo.TC_WagerSettings (
        WagerSettingsId   INT IDENTITY(1,1) NOT NULL,
        ShowActionOnGames BIT NOT NULL DEFAULT 1,
        MaxMarketImbalance DECIMAL(18,2) NOT NULL DEFAULT 50,
        CONSTRAINT PK_TC_WagerSettings PRIMARY KEY (WagerSettingsId)
    );

    INSERT INTO dbo.TC_WagerSettings (ShowActionOnGames, MaxMarketImbalance)
    VALUES (1, 50);
END
GO
