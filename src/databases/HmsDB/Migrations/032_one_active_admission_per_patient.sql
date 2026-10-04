-- Enforce at most one Admitted stay per patient (re-admit allowed after discharge/cancel).
IF OBJECT_ID(N'dbo.admissions', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'UQ_admissions_tenant_patient_active'
          AND object_id = OBJECT_ID(N'dbo.admissions'))
BEGIN
    CREATE UNIQUE INDEX UQ_admissions_tenant_patient_active
        ON dbo.admissions (tenantid, patientid)
        WHERE admissionstatus = 1;
END
GO
