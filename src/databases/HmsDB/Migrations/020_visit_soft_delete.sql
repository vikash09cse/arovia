-- Soft-delete support for visits
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.visits')
      AND name = N'isdeleted'
)
BEGIN
    ALTER TABLE dbo.visits
        ADD isdeleted BIT NOT NULL
            CONSTRAINT DF_visits_isdeleted DEFAULT (0);
END
GO

-- Refresh filtered index to include isdeleted (safe for existing DBs).
IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.visits')
      AND name = N'IX_visits_tenant_patient_feestatus'
)
    DROP INDEX IX_visits_tenant_patient_feestatus ON dbo.visits;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.visits')
      AND name = N'IX_visits_tenant_patient_feestatus'
)
    AND COL_LENGTH(N'dbo.visits', N'isdeleted') IS NOT NULL
BEGIN
    CREATE INDEX IX_visits_tenant_patient_feestatus
        ON dbo.visits (tenantid, patientid, feestatus, visitdatetime DESC)
        WHERE visitstatus = 1 AND isdeleted = 0;
END
GO
