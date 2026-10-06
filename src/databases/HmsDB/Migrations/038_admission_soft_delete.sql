-- Soft-delete support for admissions
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.admissions')
      AND name = N'isdeleted'
)
BEGIN
    ALTER TABLE dbo.admissions
        ADD isdeleted BIT NOT NULL
            CONSTRAINT DF_admissions_isdeleted DEFAULT (0);
END
GO

-- Refresh filtered unique index so deleted active stays free the patient for re-admit.
-- Dynamic SQL avoids compile-time binding when isdeleted is not yet present.
IF OBJECT_ID(N'dbo.admissions', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.admissions', N'isdeleted') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.admissions')
          AND name = N'UQ_admissions_tenant_patient_active'
    )
        DROP INDEX UQ_admissions_tenant_patient_active ON dbo.admissions;

    EXEC(N'
        CREATE UNIQUE INDEX UQ_admissions_tenant_patient_active
            ON dbo.admissions (tenantid, patientid)
            WHERE admissionstatus = 1 AND isdeleted = 0;
    ');
END
GO
