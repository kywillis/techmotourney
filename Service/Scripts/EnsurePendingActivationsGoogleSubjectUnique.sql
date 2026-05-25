-- Ensures one TC_PendingActivations row per Google account (prevents duplicate pending signups).
-- Safe to run multiple times. Existing DBs that already ran WageringStep1_Migrations.sql will skip.

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UQ_TC_PendingActivations_GoogleSubjectId'
      AND object_id = OBJECT_ID(N'dbo.TC_PendingActivations')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_TC_PendingActivations_GoogleSubjectId
    ON dbo.TC_PendingActivations (GoogleSubjectId);
END
GO
